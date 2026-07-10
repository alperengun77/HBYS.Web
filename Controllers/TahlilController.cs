using HBYS.Web.Data;
using HBYS.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HBYS.Web.Helpers;

namespace HBYS.Web.Controllers
{
    public class TahlilController : Controller
    {
        private readonly AppDbContext _context;

        public TahlilController(AppDbContext context)
        {
            _context = context;
        }

        private bool GirisYapildiMi()
        {
            string? kullaniciAdi = HttpContext.Session.GetString("KullaniciAdi");
            return !string.IsNullOrEmpty(kullaniciAdi);
        }

        private bool TahlilIslemiYetkisiVarMi()
        {
            string? rolAdi = HttpContext.Session.GetString("RolAdi");

            return rolAdi == "Admin" ||
                   rolAdi == "Doktor" ||
                   rolAdi == "Laborant";
        }

        private void MuayeneleriHazirla()
        {
            var muayeneListesi = _context.Muayeneler
                .Include(m => m.Hasta)
                .Include(m => m.Doktor)
                .Where(m => m.AktifMi)
                .OrderByDescending(m => m.MuayeneTarihi)
                .ToList()
                .Select(m => new
                {
                    m.MuayeneId,
                    MuayeneBilgisi =
                        m.MuayeneTarihi.ToString("dd.MM.yyyy HH:mm") +
                        " - " +
                        m.Hasta!.TcKimlikNo +
                        " - " +
                        m.Hasta.Ad + " " + m.Hasta.Soyad +
                        " / Dr. " +
                        m.Doktor!.Ad + " " + m.Doktor.Soyad
                })
                .ToList();

            ViewBag.Muayeneler = new SelectList(
                muayeneListesi,
                "MuayeneId",
                "MuayeneBilgisi"
            );

            ViewBag.Durumlar = new SelectList(new List<string>
            {
                "Istenildi",
                "Calisiliyor",
                "Sonuclandi",
                "Iptal"
            });
        }

        public IActionResult Index(
     string? arama,
     DateTime? baslangicTarihi,
     DateTime? bitisTarihi,
     string? durum)
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction("Login", "Account");
            }

            var tahliller = _context.Tahliller
                .Include(t => t.Muayene)
                    .ThenInclude(m => m!.Hasta)
                .Include(t => t.Muayene)
                    .ThenInclude(m => m!.Doktor)
                .Where(t => t.AktifMi)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(arama))
            {
                tahliller = tahliller.Where(t =>
                    t.TahlilAdi.Contains(arama) ||
                    t.Durum.Contains(arama) ||
                    (t.Sonuc != null && t.Sonuc.Contains(arama)) ||
                    (t.Muayene != null &&
                     t.Muayene.Hasta != null &&
                     (
                        t.Muayene.Hasta.TcKimlikNo.Contains(arama) ||
                        t.Muayene.Hasta.Ad.Contains(arama) ||
                        t.Muayene.Hasta.Soyad.Contains(arama)
                     )) ||
                    (t.Muayene != null &&
                     t.Muayene.Doktor != null &&
                     (
                        t.Muayene.Doktor.Ad.Contains(arama) ||
                        t.Muayene.Doktor.Soyad.Contains(arama)
                     )));
            }

            if (baslangicTarihi.HasValue)
            {
                DateTime baslangic = baslangicTarihi.Value.Date;
                tahliller = tahliller.Where(t => t.IstenmeTarihi >= baslangic);
            }

            if (bitisTarihi.HasValue)
            {
                DateTime bitis = bitisTarihi.Value.Date.AddDays(1);
                tahliller = tahliller.Where(t => t.IstenmeTarihi < bitis);
            }

            if (!string.IsNullOrWhiteSpace(durum))
            {
                tahliller = tahliller.Where(t => t.Durum == durum);
            }

            ViewBag.Arama = arama;
            ViewBag.BaslangicTarihi = baslangicTarihi?.ToString("yyyy-MM-dd");
            ViewBag.BitisTarihi = bitisTarihi?.ToString("yyyy-MM-dd");
            ViewBag.Durum = durum;

            return View(tahliller
                .OrderByDescending(t => t.IstenmeTarihi)
                .ToList());
        }

        [HttpGet]
        public IActionResult Create()
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction("Login", "Account");
            }

            if (!TahlilIslemiYetkisiVarMi())
            {
                TempData["Hata"] = "Bu işlem için yetkiniz yok.";
                return RedirectToAction("Index");
            }

            MuayeneleriHazirla();

            Tahlil yeniTahlil = new Tahlil
            {
                IstenmeTarihi = DateTime.Now,
                Durum = "Istenildi"
            };

            return View(yeniTahlil);
        }
        public IActionResult Details(int id)
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction("Login", "Account");
            }

            if (!TahlilIslemiYetkisiVarMi())
            {
                TempData["Hata"] = "Bu sayfayı görüntüleme yetkiniz yok.";
                return RedirectToAction("Index", "Dashboard");
            }

            var tahlil = _context.Tahliller
                .Include(t => t.Muayene)
                    .ThenInclude(m => m!.Hasta)
                .Include(t => t.Muayene)
                    .ThenInclude(m => m!.Doktor)
                .FirstOrDefault(t => t.TahlilId == id && t.AktifMi);

            if (tahlil == null)
            {
                TempData["Hata"] = "Tahlil kaydı bulunamadı.";
                return RedirectToAction("Index");
            }

            return View(tahlil);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Tahlil tahlil)
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction("Login", "Account");
            }

            if (!TahlilIslemiYetkisiVarMi())
            {
                TempData["Hata"] = "Bu işlem için yetkiniz yok.";
                return RedirectToAction("Index");
            }

            if (tahlil.MuayeneId <= 0)
            {
                ModelState.AddModelError("MuayeneId", "Muayene seçiniz.");
            }

            bool muayeneVarMi = _context.Muayeneler.Any(m =>
                m.MuayeneId == tahlil.MuayeneId &&
                m.AktifMi);

            if (!muayeneVarMi)
            {
                ModelState.AddModelError("MuayeneId", "Seçilen muayene kaydı bulunamadı.");
            }

            if (string.IsNullOrWhiteSpace(tahlil.Durum))
            {
                tahlil.Durum = "Istenildi";
            }

            if (tahlil.Durum == "Sonuclandi" && tahlil.SonucTarihi == null)
            {
                tahlil.SonucTarihi = DateTime.Now;
            }

            if (!ModelState.IsValid)
            {
                MuayeneleriHazirla();
                return View(tahlil);
            }

            tahlil.AktifMi = true;

            if (tahlil.IstenmeTarihi == DateTime.MinValue)
            {
                tahlil.IstenmeTarihi = DateTime.Now;
            }
            tahlil.IstenmeTarihi = ValidationHelper.SaniyeVeSaliseyiTemizle(tahlil.IstenmeTarihi);

            if (tahlil.SonucTarihi.HasValue)
            {
                tahlil.SonucTarihi = ValidationHelper.SaniyeVeSaliseyiTemizle(tahlil.SonucTarihi.Value);
            }

            _context.Tahliller.Add(tahlil);
            _context.SaveChanges();

            TempData["Basari"] = "Tahlil kaydı başarıyla oluşturuldu.";

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction("Login", "Account");
            }

            if (!TahlilIslemiYetkisiVarMi())
            {
                TempData["Hata"] = "Bu işlem için yetkiniz yok.";
                return RedirectToAction("Index");
            }

            var tahlil = _context.Tahliller
                .FirstOrDefault(t => t.TahlilId == id && t.AktifMi);

            if (tahlil == null)
            {
                TempData["Hata"] = "Tahlil kaydı bulunamadı.";
                return RedirectToAction("Index");
            }

            MuayeneleriHazirla();

            return View(tahlil);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Tahlil tahlil)
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction("Login", "Account");
            }

            if (!TahlilIslemiYetkisiVarMi())
            {
                TempData["Hata"] = "Bu işlem için yetkiniz yok.";
                return RedirectToAction("Index");
            }

            if (tahlil.MuayeneId <= 0)
            {
                ModelState.AddModelError("MuayeneId", "Muayene seçiniz.");
            }

            bool muayeneVarMi = _context.Muayeneler.Any(m =>
                m.MuayeneId == tahlil.MuayeneId &&
                m.AktifMi);

            if (!muayeneVarMi)
            {
                ModelState.AddModelError("MuayeneId", "Seçilen muayene kaydı bulunamadı.");
            }

            if (tahlil.Durum == "Sonuclandi" && tahlil.SonucTarihi == null)
            {
                tahlil.SonucTarihi = DateTime.Now;
            }

            if (tahlil.Durum != "Sonuclandi")
            {
                tahlil.SonucTarihi = null;
            }

            if (!ModelState.IsValid)
            {
                MuayeneleriHazirla();
                return View(tahlil);
            }

            var guncellenecekTahlil = _context.Tahliller
                .FirstOrDefault(t => t.TahlilId == tahlil.TahlilId && t.AktifMi);

            if (guncellenecekTahlil == null)
            {
                TempData["Hata"] = "Tahlil kaydı bulunamadı.";
                return RedirectToAction("Index");
            }

            guncellenecekTahlil.MuayeneId = tahlil.MuayeneId;
            guncellenecekTahlil.TahlilAdi = tahlil.TahlilAdi;
            guncellenecekTahlil.Sonuc = tahlil.Sonuc;
            guncellenecekTahlil.Durum = tahlil.Durum;
            guncellenecekTahlil.IstenmeTarihi = ValidationHelper.SaniyeVeSaliseyiTemizle(tahlil.IstenmeTarihi);

            if (tahlil.SonucTarihi.HasValue)
            {
                guncellenecekTahlil.SonucTarihi = ValidationHelper.SaniyeVeSaliseyiTemizle(tahlil.SonucTarihi.Value);
            }
            else
            {
                guncellenecekTahlil.SonucTarihi = null;
            }

            _context.SaveChanges();

            TempData["Basari"] = "Tahlil kaydı başarıyla güncellendi.";

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction("Login", "Account");
            }

            if (!TahlilIslemiYetkisiVarMi())
            {
                TempData["Hata"] = "Tahlil silme işlemi için yetkiniz yok.";
                return RedirectToAction("Index");
            }

            var tahlil = _context.Tahliller
                .FirstOrDefault(t => t.TahlilId == id && t.AktifMi);

            if (tahlil == null)
            {
                TempData["Hata"] = "Tahlil kaydı bulunamadı.";
                return RedirectToAction("Index");
            }

            tahlil.AktifMi = false;

            _context.SaveChanges();

            TempData["Basari"] = "Tahlil kaydı başarıyla silindi.";

            return RedirectToAction("Index");
        }
    }
}
