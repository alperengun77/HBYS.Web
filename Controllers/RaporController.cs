using HBYS.Web.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HBYS.Web.Controllers
{
    public class RaporController : Controller
    {
        private readonly AppDbContext _context;

        public RaporController(AppDbContext context)
        {
            _context = context;
        }

        private bool GirisYapildiMi()
        {
            string? kullaniciAdi = HttpContext.Session.GetString("KullaniciAdi");
            return !string.IsNullOrEmpty(kullaniciAdi);
        }

        private bool RaporGormeYetkisiVarMi()
        {
            string? rolAdi = HttpContext.Session.GetString("RolAdi");

            return rolAdi == "Admin" ||
                   rolAdi == "Yonetici" ||
                   rolAdi == "Sekreter" ||
                   rolAdi == "Doktor";
        }

        public IActionResult Index()
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction("Login", "Account");
            }

            if (!RaporGormeYetkisiVarMi())
            {
                TempData["Hata"] = "Raporları görüntüleme yetkiniz yok.";
                return RedirectToAction("Index", "Dashboard");
            }

            DateTime bugunBaslangic = DateTime.Today;
            DateTime yarinBaslangic = bugunBaslangic.AddDays(1);

            ViewBag.HastaSayisi = _context.Hastalar.Count(h => h.AktifMi);
            ViewBag.DoktorSayisi = _context.Doktorlar.Count(d => d.AktifMi);
            ViewBag.PoliklinikSayisi = _context.Poliklinikler.Count(p => p.AktifMi);
            ViewBag.RandevuSayisi = _context.Randevular.Count(r => r.AktifMi);
            ViewBag.MuayeneSayisi = _context.Muayeneler.Count(m => m.AktifMi);
            ViewBag.ReceteSayisi = _context.Receteler.Count(r => r.AktifMi);
            ViewBag.TahlilSayisi = _context.Tahliller.Count(t => t.AktifMi);

            ViewBag.BugunRandevuSayisi = _context.Randevular.Count(r =>
                r.AktifMi &&
                r.RandevuTarihiSaati >= bugunBaslangic &&
                r.RandevuTarihiSaati < yarinBaslangic);

            ViewBag.BugunMuayeneSayisi = _context.Muayeneler.Count(m =>
                m.AktifMi &&
                m.MuayeneTarihi >= bugunBaslangic &&
                m.MuayeneTarihi < yarinBaslangic);

            ViewBag.BekleyenTahlilSayisi = _context.Tahliller.Count(t =>
                t.AktifMi &&
                t.Durum != "Sonuclandi" &&
                t.Durum != "Iptal");

            return View();
        }

        public IActionResult GunlukRandevu(DateTime? tarih)
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction("Login", "Account");
            }

            if (!RaporGormeYetkisiVarMi())
            {
                TempData["Hata"] = "Raporları görüntüleme yetkiniz yok.";
                return RedirectToAction("Index", "Dashboard");
            }

            DateTime seciliTarih = tarih ?? DateTime.Today;
            DateTime baslangic = seciliTarih.Date;
            DateTime bitis = baslangic.AddDays(1);

            var randevular = _context.Randevular
                .Include(r => r.Hasta)
                .Include(r => r.Doktor)
                .Include(r => r.Poliklinik)
                .Where(r =>
                    r.AktifMi &&
                    r.RandevuTarihiSaati >= baslangic &&
                    r.RandevuTarihiSaati < bitis)
                .OrderBy(r => r.RandevuTarihiSaati)
                .ToList();

            ViewBag.SeciliTarih = seciliTarih.ToString("yyyy-MM-dd");
            ViewBag.Toplam = randevular.Count;
            ViewBag.Bekleyen = randevular.Count(r => r.Durum == "Bekliyor");
            ViewBag.Geldi = randevular.Count(r => r.Durum == "Geldi");
            ViewBag.Tamamlandi = randevular.Count(r => r.Durum == "Tamamlandi");
            ViewBag.Iptal = randevular.Count(r => r.Durum == "Iptal");

            return View(randevular);
        }

        public IActionResult MuayeneRaporu(DateTime? baslangicTarihi, DateTime? bitisTarihi)
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction("Login", "Account");
            }

            if (!RaporGormeYetkisiVarMi())
            {
                TempData["Hata"] = "Raporları görüntüleme yetkiniz yok.";
                return RedirectToAction("Index", "Dashboard");
            }

            DateTime baslangic = baslangicTarihi?.Date ?? DateTime.Today.AddDays(-7);
            DateTime bitis = (bitisTarihi?.Date ?? DateTime.Today).AddDays(1);

            var muayeneler = _context.Muayeneler
                .Include(m => m.Hasta)
                .Include(m => m.Doktor)
                .Where(m =>
                    m.AktifMi &&
                    m.MuayeneTarihi >= baslangic &&
                    m.MuayeneTarihi < bitis)
                .OrderByDescending(m => m.MuayeneTarihi)
                .ToList();

            ViewBag.BaslangicTarihi = baslangic.ToString("yyyy-MM-dd");
            ViewBag.BitisTarihi = bitis.AddDays(-1).ToString("yyyy-MM-dd");
            ViewBag.Toplam = muayeneler.Count;

            return View(muayeneler);
        }

        public IActionResult TahlilRaporu(DateTime? baslangicTarihi, DateTime? bitisTarihi)
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction("Login", "Account");
            }

            if (!RaporGormeYetkisiVarMi())
            {
                TempData["Hata"] = "Raporları görüntüleme yetkiniz yok.";
                return RedirectToAction("Index", "Dashboard");
            }

            DateTime baslangic = baslangicTarihi?.Date ?? DateTime.Today.AddDays(-7);
            DateTime bitis = (bitisTarihi?.Date ?? DateTime.Today).AddDays(1);

            var tahliller = _context.Tahliller
                .Include(t => t.Muayene)
                    .ThenInclude(m => m!.Hasta)
                .Include(t => t.Muayene)
                    .ThenInclude(m => m!.Doktor)
                .Where(t =>
                    t.AktifMi &&
                    t.IstenmeTarihi >= baslangic &&
                    t.IstenmeTarihi < bitis)
                .OrderByDescending(t => t.IstenmeTarihi)
                .ToList();

            ViewBag.BaslangicTarihi = baslangic.ToString("yyyy-MM-dd");
            ViewBag.BitisTarihi = bitis.AddDays(-1).ToString("yyyy-MM-dd");
            ViewBag.Toplam = tahliller.Count;
            ViewBag.Istenildi = tahliller.Count(t => t.Durum == "Istenildi");
            ViewBag.Calisiliyor = tahliller.Count(t => t.Durum == "Calisiliyor");
            ViewBag.Sonuclandi = tahliller.Count(t => t.Durum == "Sonuclandi");
            ViewBag.Iptal = tahliller.Count(t => t.Durum == "Iptal");

            return View(tahliller);
        }
    }
}
