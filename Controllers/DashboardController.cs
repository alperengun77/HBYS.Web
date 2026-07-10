using HBYS.Web.Data;
using HBYS.Web.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HBYS.Web.Controllers
{
    public class DashboardController : Controller
    {
        private readonly AppDbContext _context;

        public DashboardController(AppDbContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            string? kullaniciAdi = HttpContext.Session.GetString("KullaniciAdi");

            if (string.IsNullOrEmpty(kullaniciAdi))
            {
                return RedirectToAction("Login", "Account");
            }

            string? adSoyad = HttpContext.Session.GetString("AdSoyad");
            string? rolAdi = HttpContext.Session.GetString("RolAdi");
            int? doktorId = HttpContext.Session.GetInt32("DoktorId");

            bool adminMi = rolAdi == "Admin";
            bool doktorMu = rolAdi == "Doktor";
            bool sekreterMi = rolAdi == "Sekreter";
            bool laborantMi = rolAdi == "Laborant";
            bool eczaneMi = rolAdi == "Eczane";
            bool yoneticiMi = rolAdi == "Yonetici";

            DateTime bugunBaslangic = DateTime.Today;
            DateTime yarinBaslangic = bugunBaslangic.AddDays(1);

            int hastaSayisi = 0;
            int doktorSayisi = 0;
            int poliklinikSayisi = 0;
            int randevuSayisi = 0;
            int bugunRandevuSayisi = 0;
            int bekleyenRandevuSayisi = 0;
            int tamamlananRandevuSayisi = 0;
            int muayeneSayisi = 0;
            int bugunMuayeneSayisi = 0;
            int receteSayisi = 0;
            int tahlilSayisi = 0;
            int bekleyenTahlilSayisi = 0;

            List<Randevu> sonRandevular = new List<Randevu>();
            List<Muayene> sonMuayeneler = new List<Muayene>();
            List<Recete> sonReceteler = new List<Recete>();
            List<Tahlil> sonTahliller = new List<Tahlil>();

            string dashboardBaslik = "Genel Yönetim Paneli";
            string dashboardAciklama = "Yetkinize göre sistem özetleri görüntülenmektedir.";
            string? dashboardUyarisi = null;

            if (adminMi || yoneticiMi)
            {
                hastaSayisi = _context.Hastalar.Count(h => h.AktifMi);
                doktorSayisi = _context.Doktorlar.Count(d => d.AktifMi);
                poliklinikSayisi = _context.Poliklinikler.Count(p => p.AktifMi);

                randevuSayisi = _context.Randevular.Count(r => r.AktifMi);
                bugunRandevuSayisi = _context.Randevular.Count(r =>
                    r.AktifMi &&
                    r.RandevuTarihiSaati >= bugunBaslangic &&
                    r.RandevuTarihiSaati < yarinBaslangic);

                bekleyenRandevuSayisi = _context.Randevular.Count(r =>
                    r.AktifMi &&
                    r.Durum == "Bekliyor");

                tamamlananRandevuSayisi = _context.Randevular.Count(r =>
                    r.AktifMi &&
                    r.Durum == "Tamamlandi");

                muayeneSayisi = _context.Muayeneler.Count(m => m.AktifMi);

                bugunMuayeneSayisi = _context.Muayeneler.Count(m =>
                    m.AktifMi &&
                    m.MuayeneTarihi >= bugunBaslangic &&
                    m.MuayeneTarihi < yarinBaslangic);

                receteSayisi = _context.Receteler.Count(r => r.AktifMi);
                tahlilSayisi = _context.Tahliller.Count(t => t.AktifMi);

                bekleyenTahlilSayisi = _context.Tahliller.Count(t =>
                    t.AktifMi &&
                    t.Durum != "Sonuclandi" &&
                    t.Durum != "Iptal");

                sonRandevular = _context.Randevular
                    .Include(r => r.Hasta)
                    .Include(r => r.Doktor)
                    .Include(r => r.Poliklinik)
                    .Where(r => r.AktifMi)
                    .OrderByDescending(r => r.RandevuTarihiSaati)
                    .Take(5)
                    .ToList();

                sonMuayeneler = _context.Muayeneler
                    .Include(m => m.Hasta)
                    .Include(m => m.Doktor)
                    .Where(m => m.AktifMi)
                    .OrderByDescending(m => m.MuayeneTarihi)
                    .Take(5)
                    .ToList();

                sonReceteler = _context.Receteler
                    .Include(r => r.Muayene)
                        .ThenInclude(m => m!.Hasta)
                    .Include(r => r.Muayene)
                        .ThenInclude(m => m!.Doktor)
                    .Where(r => r.AktifMi)
                    .OrderByDescending(r => r.KayitTarihi)
                    .Take(5)
                    .ToList();

                sonTahliller = _context.Tahliller
                    .Include(t => t.Muayene)
                        .ThenInclude(m => m!.Hasta)
                    .Include(t => t.Muayene)
                        .ThenInclude(m => m!.Doktor)
                    .Where(t => t.AktifMi)
                    .OrderByDescending(t => t.IstenmeTarihi)
                    .Take(5)
                    .ToList();

                dashboardBaslik = adminMi ? "Admin Yönetim Paneli" : "Yönetici Paneli";
                dashboardAciklama = "Sistemdeki tüm aktif kayıtların genel özeti görüntülenmektedir.";
            }
            else if (sekreterMi)
            {
                hastaSayisi = _context.Hastalar.Count(h => h.AktifMi);
                doktorSayisi = _context.Doktorlar.Count(d => d.AktifMi);
                poliklinikSayisi = _context.Poliklinikler.Count(p => p.AktifMi);

                randevuSayisi = _context.Randevular.Count(r => r.AktifMi);

                bugunRandevuSayisi = _context.Randevular.Count(r =>
                    r.AktifMi &&
                    r.RandevuTarihiSaati >= bugunBaslangic &&
                    r.RandevuTarihiSaati < yarinBaslangic);

                bekleyenRandevuSayisi = _context.Randevular.Count(r =>
                    r.AktifMi &&
                    r.Durum == "Bekliyor");

                tamamlananRandevuSayisi = _context.Randevular.Count(r =>
                    r.AktifMi &&
                    r.Durum == "Tamamlandi");

                sonRandevular = _context.Randevular
                    .Include(r => r.Hasta)
                    .Include(r => r.Doktor)
                    .Include(r => r.Poliklinik)
                    .Where(r => r.AktifMi)
                    .OrderByDescending(r => r.RandevuTarihiSaati)
                    .Take(5)
                    .ToList();

                dashboardBaslik = "Sekreter Paneli";
                dashboardAciklama = "Hasta, doktor ve randevu işlemlerine ait özet bilgiler görüntülenmektedir.";
            }
            else if (doktorMu)
            {
                if (!doktorId.HasValue)
                {
                    dashboardUyarisi = "Bu doktor kullanıcısı herhangi bir doktor kaydıyla eşleştirilmemiş. Kullanıcı İşlemleri ekranından doktora bağlanmalıdır.";
                }
                else
                {
                    int aktifDoktorId = doktorId.Value;

                    hastaSayisi = _context.Randevular
                        .Where(r => r.AktifMi && r.DoktorId == aktifDoktorId)
                        .Select(r => r.HastaId)
                        .Distinct()
                        .Count();

                    doktorSayisi = _context.Doktorlar.Count(d =>
                        d.AktifMi &&
                        d.DoktorId == aktifDoktorId);

                    poliklinikSayisi = _context.Doktorlar
                        .Where(d => d.AktifMi && d.DoktorId == aktifDoktorId)
                        .Select(d => d.PoliklinikId)
                        .Distinct()
                        .Count();

                    randevuSayisi = _context.Randevular.Count(r =>
                        r.AktifMi &&
                        r.DoktorId == aktifDoktorId);

                    bugunRandevuSayisi = _context.Randevular.Count(r =>
                        r.AktifMi &&
                        r.DoktorId == aktifDoktorId &&
                        r.RandevuTarihiSaati >= bugunBaslangic &&
                        r.RandevuTarihiSaati < yarinBaslangic);

                    bekleyenRandevuSayisi = _context.Randevular.Count(r =>
                        r.AktifMi &&
                        r.DoktorId == aktifDoktorId &&
                        r.Durum == "Bekliyor");

                    tamamlananRandevuSayisi = _context.Randevular.Count(r =>
                        r.AktifMi &&
                        r.DoktorId == aktifDoktorId &&
                        r.Durum == "Tamamlandi");

                    muayeneSayisi = _context.Muayeneler.Count(m =>
                        m.AktifMi &&
                        m.DoktorId == aktifDoktorId);

                    bugunMuayeneSayisi = _context.Muayeneler.Count(m =>
                        m.AktifMi &&
                        m.DoktorId == aktifDoktorId &&
                        m.MuayeneTarihi >= bugunBaslangic &&
                        m.MuayeneTarihi < yarinBaslangic);

                    receteSayisi = _context.Receteler.Count(r =>
                        r.AktifMi &&
                        r.Muayene != null &&
                        r.Muayene.DoktorId == aktifDoktorId);

                    tahlilSayisi = _context.Tahliller.Count(t =>
                        t.AktifMi &&
                        t.Muayene != null &&
                        t.Muayene.DoktorId == aktifDoktorId);

                    bekleyenTahlilSayisi = _context.Tahliller.Count(t =>
                        t.AktifMi &&
                        t.Muayene != null &&
                        t.Muayene.DoktorId == aktifDoktorId &&
                        t.Durum != "Sonuclandi" &&
                        t.Durum != "Iptal");

                    sonRandevular = _context.Randevular
                        .Include(r => r.Hasta)
                        .Include(r => r.Doktor)
                        .Include(r => r.Poliklinik)
                        .Where(r => r.AktifMi && r.DoktorId == aktifDoktorId)
                        .OrderByDescending(r => r.RandevuTarihiSaati)
                        .Take(5)
                        .ToList();

                    sonMuayeneler = _context.Muayeneler
                        .Include(m => m.Hasta)
                        .Include(m => m.Doktor)
                        .Where(m => m.AktifMi && m.DoktorId == aktifDoktorId)
                        .OrderByDescending(m => m.MuayeneTarihi)
                        .Take(5)
                        .ToList();

                    sonReceteler = _context.Receteler
                        .Include(r => r.Muayene)
                            .ThenInclude(m => m!.Hasta)
                        .Include(r => r.Muayene)
                            .ThenInclude(m => m!.Doktor)
                        .Where(r =>
                            r.AktifMi &&
                            r.Muayene != null &&
                            r.Muayene.DoktorId == aktifDoktorId)
                        .OrderByDescending(r => r.KayitTarihi)
                        .Take(5)
                        .ToList();

                    sonTahliller = _context.Tahliller
                        .Include(t => t.Muayene)
                            .ThenInclude(m => m!.Hasta)
                        .Include(t => t.Muayene)
                            .ThenInclude(m => m!.Doktor)
                        .Where(t =>
                            t.AktifMi &&
                            t.Muayene != null &&
                            t.Muayene.DoktorId == aktifDoktorId)
                        .OrderByDescending(t => t.IstenmeTarihi)
                        .Take(5)
                        .ToList();
                }

                dashboardBaslik = "Doktor Paneli";
                dashboardAciklama = "Sadece size bağlı randevu, muayene, reçete ve tahlil kayıtları görüntülenmektedir.";
            }
            else if (laborantMi)
            {
                hastaSayisi = _context.Tahliller
                    .Where(t => t.AktifMi && t.Muayene != null)
                    .Select(t => t.Muayene!.HastaId)
                    .Distinct()
                    .Count();

                tahlilSayisi = _context.Tahliller.Count(t => t.AktifMi);

                bekleyenTahlilSayisi = _context.Tahliller.Count(t =>
                    t.AktifMi &&
                    t.Durum != "Sonuclandi" &&
                    t.Durum != "Iptal");

                sonTahliller = _context.Tahliller
                    .Include(t => t.Muayene)
                        .ThenInclude(m => m!.Hasta)
                    .Include(t => t.Muayene)
                        .ThenInclude(m => m!.Doktor)
                    .Where(t => t.AktifMi)
                    .OrderByDescending(t => t.IstenmeTarihi)
                    .Take(5)
                    .ToList();

                dashboardBaslik = "Laborant Paneli";
                dashboardAciklama = "Tahlil işlemlerine ait özet bilgiler görüntülenmektedir.";
            }
            else if (eczaneMi)
            {
                hastaSayisi = _context.Receteler
                    .Where(r => r.AktifMi && r.Muayene != null)
                    .Select(r => r.Muayene!.HastaId)
                    .Distinct()
                    .Count();

                receteSayisi = _context.Receteler.Count(r => r.AktifMi);

                sonReceteler = _context.Receteler
                    .Include(r => r.Muayene)
                        .ThenInclude(m => m!.Hasta)
                    .Include(r => r.Muayene)
                        .ThenInclude(m => m!.Doktor)
                    .Where(r => r.AktifMi)
                    .OrderByDescending(r => r.KayitTarihi)
                    .Take(5)
                    .ToList();

                dashboardBaslik = "Eczane Paneli";
                dashboardAciklama = "Reçete işlemlerine ait özet bilgiler görüntülenmektedir.";
            }

            ViewBag.AdSoyad = adSoyad;
            ViewBag.YetkiAdi = rolAdi;
            ViewBag.RolAdi = rolAdi;
            ViewBag.DoktorId = doktorId;

            ViewBag.AdminMi = adminMi;
            ViewBag.DoktorMu = doktorMu;
            ViewBag.SekreterMi = sekreterMi;
            ViewBag.LaborantMi = laborantMi;
            ViewBag.EczaneMi = eczaneMi;
            ViewBag.YoneticiMi = yoneticiMi;

            ViewBag.DashboardBaslik = dashboardBaslik;
            ViewBag.DashboardAciklama = dashboardAciklama;
            ViewBag.DashboardUyarisi = dashboardUyarisi;

            ViewBag.HastaSayisi = hastaSayisi;
            ViewBag.DoktorSayisi = doktorSayisi;
            ViewBag.PoliklinikSayisi = poliklinikSayisi;

            ViewBag.RandevuSayisi = randevuSayisi;
            ViewBag.BugunRandevuSayisi = bugunRandevuSayisi;
            ViewBag.BekleyenRandevuSayisi = bekleyenRandevuSayisi;
            ViewBag.TamamlananRandevuSayisi = tamamlananRandevuSayisi;

            ViewBag.MuayeneSayisi = muayeneSayisi;
            ViewBag.BugunMuayeneSayisi = bugunMuayeneSayisi;

            ViewBag.ReceteSayisi = receteSayisi;
            ViewBag.TahlilSayisi = tahlilSayisi;
            ViewBag.BekleyenTahlilSayisi = bekleyenTahlilSayisi;

            ViewBag.SonRandevular = sonRandevular;
            ViewBag.SonMuayeneler = sonMuayeneler;
            ViewBag.SonReceteler = sonReceteler;
            ViewBag.SonTahliller = sonTahliller;

            return View();
        }
    }
}