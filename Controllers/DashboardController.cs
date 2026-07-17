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

        public IActionResult Index(DateTime? tarih)
        {
            string? kullaniciAdi =
                HttpContext.Session.GetString("KullaniciAdi");

            if (string.IsNullOrEmpty(kullaniciAdi))
            {
                return RedirectToAction("Login", "Account");
            }

            string? adSoyad =
                HttpContext.Session.GetString("AdSoyad");

            string? rolAdi =
                HttpContext.Session.GetString("RolAdi");

            int? doktorId =
                HttpContext.Session.GetInt32("DoktorId");

            bool adminMi = rolAdi == "Admin";
            bool doktorMu = rolAdi == "Doktor";
            bool sekreterMi = rolAdi == "Sekreter";
            bool laborantMi = rolAdi == "Laborant";
            bool eczaneMi = rolAdi == "Eczane";
            bool yoneticiMi = rolAdi == "Yonetici";

            DateTime seciliTarih =
                (tarih ?? DateTime.Today).Date;

            DateTime gunBaslangic = seciliTarih;
            DateTime gunBitis = seciliTarih.AddDays(1);

            IQueryable<Randevu> randevuSorgusu =
                _context.Randevular
                    .AsNoTracking()
                    .Where(r =>
                        r.AktifMi &&
                        r.RandevuTarihiSaati >= gunBaslangic &&
                        r.RandevuTarihiSaati < gunBitis
                    );

            IQueryable<Muayene> muayeneSorgusu =
                _context.Muayeneler
                    .AsNoTracking()
                    .Where(m =>
                        m.AktifMi &&
                        m.MuayeneTarihi >= gunBaslangic &&
                        m.MuayeneTarihi < gunBitis
                    );

            IQueryable<Recete> receteSorgusu =
                _context.Receteler
                    .AsNoTracking()
                    .Where(r =>
                        r.AktifMi &&
                        r.KayitTarihi >= gunBaslangic &&
                        r.KayitTarihi < gunBitis
                    );

            IQueryable<Tahlil> tahlilSorgusu =
                _context.Tahliller
                    .AsNoTracking()
                    .Where(t =>
                        t.AktifMi &&
                        t.IstenmeTarihi >= gunBaslangic &&
                        t.IstenmeTarihi < gunBitis
                    );

            string dashboardBaslik = "Genel Panel";

            string dashboardAciklama =
                "Seçilen güne ait yetkiniz kapsamındaki kayıtlar gösterilmektedir.";

            string? dashboardUyarisi = null;

            if (doktorMu)
            {
                if (!doktorId.HasValue)
                {
                    dashboardUyarisi =
                        "Bu doktor kullanıcısı bir doktor kaydıyla eşleştirilmemiştir.";

                    randevuSorgusu =
                        randevuSorgusu.Where(r => false);

                    muayeneSorgusu =
                        muayeneSorgusu.Where(m => false);

                    receteSorgusu =
                        receteSorgusu.Where(r => false);

                    tahlilSorgusu =
                        tahlilSorgusu.Where(t => false);
                }
                else
                {
                    int aktifDoktorId = doktorId.Value;

                    randevuSorgusu =
                        randevuSorgusu.Where(r =>
                            r.DoktorId == aktifDoktorId
                        );

                    muayeneSorgusu =
                        muayeneSorgusu.Where(m =>
                            m.DoktorId == aktifDoktorId
                        );

                    receteSorgusu =
                        receteSorgusu.Where(r =>
                            r.Muayene != null &&
                            r.Muayene.DoktorId ==
                            aktifDoktorId
                        );

                    tahlilSorgusu =
                        tahlilSorgusu.Where(t =>
                            t.Muayene != null &&
                            t.Muayene.DoktorId ==
                            aktifDoktorId
                        );
                }

                dashboardBaslik = "Doktor Paneli";

                dashboardAciklama =
                    "Seçilen gün için yalnızca size ait hasta, randevu, muayene, reçete ve tahlil kayıtları gösterilmektedir.";
            }
            else if (sekreterMi)
            {
                muayeneSorgusu =
                    muayeneSorgusu.Where(m => false);

                receteSorgusu =
                    receteSorgusu.Where(r => false);

                tahlilSorgusu =
                    tahlilSorgusu.Where(t => false);

                dashboardBaslik = "Sekreter Paneli";

                dashboardAciklama =
                    "Seçilen günün hasta ve randevu hareketleri gösterilmektedir.";
            }
            else if (laborantMi)
            {
                randevuSorgusu =
                    randevuSorgusu.Where(r => false);

                muayeneSorgusu =
                    muayeneSorgusu.Where(m => false);

                receteSorgusu =
                    receteSorgusu.Where(r => false);

                dashboardBaslik = "Laborant Paneli";

                dashboardAciklama =
                    "Seçilen günün tahlil işlemleri gösterilmektedir.";
            }
            else if (eczaneMi)
            {
                randevuSorgusu =
                    randevuSorgusu.Where(r => false);

                muayeneSorgusu =
                    muayeneSorgusu.Where(m => false);

                tahlilSorgusu =
                    tahlilSorgusu.Where(t => false);

                dashboardBaslik = "Eczane Paneli";

                dashboardAciklama =
                    "Seçilen günün reçete işlemleri gösterilmektedir.";
            }
            else if (adminMi || yoneticiMi)
            {
                dashboardBaslik =
                    adminMi
                        ? "Admin Yönetim Paneli"
                        : "Yönetici Paneli";

                dashboardAciklama =
                    "Seçilen güne ait hastane hareketlerinin genel özeti gösterilmektedir.";
            }
            else
            {
                randevuSorgusu =
                    randevuSorgusu.Where(r => false);

                muayeneSorgusu =
                    muayeneSorgusu.Where(m => false);

                receteSorgusu =
                    receteSorgusu.Where(r => false);

                tahlilSorgusu =
                    tahlilSorgusu.Where(t => false);

                dashboardUyarisi =
                    "Bu yetki için dashboard görünümü tanımlanmamıştır.";
            }

            List<int> randevuHastaIdleri =
                randevuSorgusu
                    .Select(r => r.HastaId)
                    .Distinct()
                    .ToList();

            List<int> muayeneHastaIdleri =
                muayeneSorgusu
                    .Select(m => m.HastaId)
                    .Distinct()
                    .ToList();

            List<int> receteHastaIdleri =
                receteSorgusu
                    .Where(r => r.Muayene != null)
                    .Select(r => r.Muayene!.HastaId)
                    .Distinct()
                    .ToList();

            List<int> tahlilHastaIdleri =
                tahlilSorgusu
                    .Where(t => t.Muayene != null)
                    .Select(t => t.Muayene!.HastaId)
                    .Distinct()
                    .ToList();

            int hastaSayisi;

            if (sekreterMi)
            {
                hastaSayisi =
                    randevuHastaIdleri.Count;
            }
            else if (laborantMi)
            {
                hastaSayisi =
                    tahlilHastaIdleri.Count;
            }
            else if (eczaneMi)
            {
                hastaSayisi =
                    receteHastaIdleri.Count;
            }
            else
            {
                hastaSayisi =
                    randevuHastaIdleri
                        .Union(muayeneHastaIdleri)
                        .Union(receteHastaIdleri)
                        .Union(tahlilHastaIdleri)
                        .Distinct()
                        .Count();
            }

            List<int> randevuDoktorIdleri =
                randevuSorgusu
                    .Select(r => r.DoktorId)
                    .Distinct()
                    .ToList();

            List<int> muayeneDoktorIdleri =
                muayeneSorgusu
                    .Select(m => m.DoktorId)
                    .Distinct()
                    .ToList();

            List<int> receteDoktorIdleri =
                receteSorgusu
                    .Where(r => r.Muayene != null)
                    .Select(r => r.Muayene!.DoktorId)
                    .Distinct()
                    .ToList();

            List<int> tahlilDoktorIdleri =
                tahlilSorgusu
                    .Where(t => t.Muayene != null)
                    .Select(t => t.Muayene!.DoktorId)
                    .Distinct()
                    .ToList();

            int doktorSayisi;

            if (doktorMu)
            {
                doktorSayisi =
                    doktorId.HasValue &&
                    _context.Doktorlar.Any(d =>
                        d.AktifMi &&
                        d.DoktorId == doktorId.Value
                    )
                        ? 1
                        : 0;
            }
            else if (sekreterMi)
            {
                doktorSayisi =
                    randevuDoktorIdleri.Count;
            }
            else if (laborantMi)
            {
                doktorSayisi =
                    tahlilDoktorIdleri.Count;
            }
            else if (eczaneMi)
            {
                doktorSayisi =
                    receteDoktorIdleri.Count;
            }
            else
            {
                doktorSayisi =
                    randevuDoktorIdleri
                        .Union(muayeneDoktorIdleri)
                        .Union(receteDoktorIdleri)
                        .Union(tahlilDoktorIdleri)
                        .Distinct()
                        .Count();
            }

            int poliklinikSayisi =
                randevuSorgusu
                    .Select(r => r.PoliklinikId)
                    .Distinct()
                    .Count();

            int randevuSayisi =
                randevuSorgusu.Count();

            int bekleyenRandevuSayisi =
                randevuSorgusu.Count(r =>
                    r.Durum == "Bekliyor"
                );

            int tamamlananRandevuSayisi =
                randevuSorgusu.Count(r =>
                    r.Durum == "Tamamlandi"
                );

            int iptalRandevuSayisi =
                randevuSorgusu.Count(r =>
                    r.Durum == "Iptal"
                );

            int muayeneSayisi =
                muayeneSorgusu.Count();

            int receteSayisi =
                receteSorgusu.Count();

            int tahlilSayisi =
                tahlilSorgusu.Count();

            int bekleyenTahlilSayisi =
                tahlilSorgusu.Count(t =>
                    t.Durum != "Sonuclandi" &&
                    t.Durum != "Iptal"
                );

            List<Randevu> gununRandevulari =
                randevuSorgusu
                    .Include(r => r.Hasta)
                    .Include(r => r.Doktor)
                    .Include(r => r.Poliklinik)
                    .OrderBy(r =>
                        r.RandevuTarihiSaati
                    )
                    .ToList();

            List<Muayene> gununMuayeneleri =
                muayeneSorgusu
                    .Include(m => m.Hasta)
                    .Include(m => m.Doktor)
                    .OrderBy(m =>
                        m.MuayeneTarihi
                    )
                    .ToList();

            List<Recete> gununReceteleri =
                receteSorgusu
                    .Include(r => r.Muayene)
                        .ThenInclude(m => m!.Hasta)
                    .Include(r => r.Muayene)
                        .ThenInclude(m => m!.Doktor)
                    .OrderBy(r =>
                        r.KayitTarihi
                    )
                    .ToList();

            List<Tahlil> gununTahlilleri =
                tahlilSorgusu
                    .Include(t => t.Muayene)
                        .ThenInclude(m => m!.Hasta)
                    .Include(t => t.Muayene)
                        .ThenInclude(m => m!.Doktor)
                    .OrderBy(t =>
                        t.IstenmeTarihi
                    )
                    .ToList();

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

            ViewBag.DashboardBaslik =
                dashboardBaslik;

            ViewBag.DashboardAciklama =
                dashboardAciklama;

            ViewBag.DashboardUyarisi =
                dashboardUyarisi;

            ViewBag.SeciliTarih =
                seciliTarih.ToString("yyyy-MM-dd");

            ViewBag.SeciliTarihYazi =
                seciliTarih.ToString("dd.MM.yyyy");

            ViewBag.OncekiGun =
                seciliTarih
                    .AddDays(-1)
                    .ToString("yyyy-MM-dd");

            ViewBag.SonrakiGun =
                seciliTarih
                    .AddDays(1)
                    .ToString("yyyy-MM-dd");

            ViewBag.Bugun =
                DateTime.Today
                    .ToString("yyyy-MM-dd");

            ViewBag.HastaSayisi =
                hastaSayisi;

            ViewBag.DoktorSayisi =
                doktorSayisi;

            ViewBag.PoliklinikSayisi =
                poliklinikSayisi;

            ViewBag.RandevuSayisi =
                randevuSayisi;

            ViewBag.BekleyenRandevuSayisi =
                bekleyenRandevuSayisi;

            ViewBag.TamamlananRandevuSayisi =
                tamamlananRandevuSayisi;

            ViewBag.IptalRandevuSayisi =
                iptalRandevuSayisi;

            ViewBag.MuayeneSayisi =
                muayeneSayisi;

            ViewBag.ReceteSayisi =
                receteSayisi;

            ViewBag.TahlilSayisi =
                tahlilSayisi;

            ViewBag.BekleyenTahlilSayisi =
                bekleyenTahlilSayisi;

            ViewBag.GununRandevulari =
                gununRandevulari;

            ViewBag.GununMuayeneleri =
                gununMuayeneleri;

            ViewBag.GununReceteleri =
                gununReceteleri;

            ViewBag.GununTahlilleri =
                gununTahlilleri;

            return View();
        }
    }
}