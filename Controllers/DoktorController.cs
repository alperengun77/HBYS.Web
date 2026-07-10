using HBYS.Web.Data;
using HBYS.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HBYS.Web.Helpers;

namespace HBYS.Web.Controllers
{
    public class DoktorController : Controller
    {
        private readonly AppDbContext _context;

        public DoktorController(AppDbContext context)
        {
            _context = context;
        }

        private bool GirisYapildiMi()
        {
            string? kullaniciAdi = HttpContext.Session.GetString("KullaniciAdi");
            return !string.IsNullOrEmpty(kullaniciAdi);
        }

        private bool DoktorIslemiYetkisiVarMi()
        {
            string? rolAdi = HttpContext.Session.GetString("RolAdi");

            return rolAdi == "Admin" ||
                   rolAdi == "Sekreter" ||
                   rolAdi == "Yonetici";
        }

        private void PoliklinikleriHazirla()
        {
            ViewBag.Poliklinikler = new SelectList(
                _context.Poliklinikler
                    .Where(p => p.AktifMi)
                    .OrderBy(p => p.PoliklinikAdi)
                    .ToList(),
                "PoliklinikId",
                "PoliklinikAdi"
            );
        }

        public IActionResult Index(string? arama)
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction("Login", "Account");
            }

            var doktorlar = _context.Doktorlar
                .Include(d => d.Poliklinik)
                .Where(d => d.AktifMi)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(arama))
            {
                doktorlar = doktorlar.Where(d =>
                    d.SicilNo.Contains(arama) ||
                    d.Ad.Contains(arama) ||
                    d.Soyad.Contains(arama) ||
                    (d.Unvan != null && d.Unvan.Contains(arama)) ||
                    (d.Poliklinik != null && d.Poliklinik.PoliklinikAdi.Contains(arama)));
            }

            ViewBag.Arama = arama;

            return View(doktorlar.OrderByDescending(d => d.KayitTarihi).ToList());
        }

        [HttpGet]
        public IActionResult Create()
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction("Login", "Account");
            }

            if (!DoktorIslemiYetkisiVarMi())
            {
                TempData["Hata"] = "Bu işlem için yetkiniz yok.";
                return RedirectToAction("Index");
            }

            PoliklinikleriHazirla();

            return View();
        }
        public IActionResult Details(int id)
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction("Login", "Account");
            }

            string? rolAdi = HttpContext.Session.GetString("RolAdi");

            if (rolAdi != "Admin" && rolAdi != "Sekreter" && rolAdi != "Yonetici" && rolAdi != "Doktor")
            {
                TempData["Hata"] = "Bu sayfayı görüntüleme yetkiniz yok.";
                return RedirectToAction("Index", "Dashboard");
            }

            var doktor = _context.Doktorlar
                .Include(d => d.Poliklinik)
                .FirstOrDefault(d => d.DoktorId == id && d.AktifMi);

            if (doktor == null)
            {
                TempData["Hata"] = "Doktor kaydı bulunamadı.";
                return RedirectToAction("Index");
            }

            ViewBag.Randevular = _context.Randevular
                .Include(r => r.Hasta)
                .Include(r => r.Poliklinik)
                .Where(r => r.DoktorId == id && r.AktifMi)
                .OrderByDescending(r => r.RandevuTarihiSaati)
                .ToList();

            ViewBag.Muayeneler = _context.Muayeneler
                .Include(m => m.Hasta)
                .Where(m => m.DoktorId == id && m.AktifMi)
                .OrderByDescending(m => m.MuayeneTarihi)
                .ToList();

            ViewBag.Kullanicilar = _context.Kullanicilar
                .Include(k => k.Rol)
                .Where(k => k.DoktorId == id && k.AktifMi)
                .OrderBy(k => k.KullaniciAdi)
                .ToList();

            return View(doktor);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Doktor doktor)
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction("Login", "Account");
            }

            if (!DoktorIslemiYetkisiVarMi())
            {
                TempData["Hata"] = "Bu işlem için yetkiniz yok.";
                return RedirectToAction("Index");
            }

            if (doktor.PoliklinikId <= 0)
            {
                ModelState.AddModelError("PoliklinikId", "Poliklinik seçiniz.");
            }

            bool sicilVarMi = _context.Doktorlar.Any(d =>
                d.SicilNo == doktor.SicilNo &&
                d.AktifMi);

            if (sicilVarMi)
            {
                ModelState.AddModelError("SicilNo", "Bu sicil numarası ile kayıtlı aktif doktor zaten var.");
            }
            if (!ValidationHelper.TelefonGecerliMi(doktor.Telefon))
            {
                ModelState.AddModelError("Telefon", "Telefon numarası sadece rakamlardan oluşmalı ve 10 veya 11 haneli olmalıdır.");
            }
            if (!ModelState.IsValid)
            {
                PoliklinikleriHazirla();
                return View(doktor);
            }

            doktor.AktifMi = true;
            doktor.KayitTarihi = DateTime.Now;

            _context.Doktorlar.Add(doktor);
            _context.SaveChanges();

            TempData["Basari"] = "Doktor kaydı başarıyla eklendi.";

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction("Login", "Account");
            }

            if (!DoktorIslemiYetkisiVarMi())
            {
                TempData["Hata"] = "Bu işlem için yetkiniz yok.";
                return RedirectToAction("Index");
            }

            var doktor = _context.Doktorlar.FirstOrDefault(d => d.DoktorId == id && d.AktifMi);

            if (doktor == null)
            {
                TempData["Hata"] = "Doktor kaydı bulunamadı.";
                return RedirectToAction("Index");
            }

            PoliklinikleriHazirla();

            return View(doktor);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Doktor doktor)
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction("Login", "Account");
            }

            if (!DoktorIslemiYetkisiVarMi())
            {
                TempData["Hata"] = "Bu işlem için yetkiniz yok.";
                return RedirectToAction("Index");
            }

            if (doktor.PoliklinikId <= 0)
            {
                ModelState.AddModelError("PoliklinikId", "Poliklinik seçiniz.");
            }

            bool sicilBaskaDoktordaVarMi = _context.Doktorlar.Any(d =>
                d.SicilNo == doktor.SicilNo &&
                d.DoktorId != doktor.DoktorId &&
                d.AktifMi);

            if (sicilBaskaDoktordaVarMi)
            {
                ModelState.AddModelError("SicilNo", "Bu sicil numarası başka bir doktorda kullanılıyor.");
            }
            if (!ValidationHelper.TelefonGecerliMi(doktor.Telefon))
            {
                ModelState.AddModelError("Telefon", "Telefon numarası sadece rakamlardan oluşmalı ve 10 veya 11 haneli olmalıdır.");
            }
            if (!ModelState.IsValid)
            {
                PoliklinikleriHazirla();
                return View(doktor);
            }

            var guncellenecekDoktor = _context.Doktorlar
                .FirstOrDefault(d => d.DoktorId == doktor.DoktorId && d.AktifMi);

            if (guncellenecekDoktor == null)
            {
                TempData["Hata"] = "Doktor kaydı bulunamadı.";
                return RedirectToAction("Index");
            }

            guncellenecekDoktor.SicilNo = doktor.SicilNo;
            guncellenecekDoktor.Ad = doktor.Ad;
            guncellenecekDoktor.Soyad = doktor.Soyad;
            guncellenecekDoktor.Unvan = doktor.Unvan;
            guncellenecekDoktor.Telefon = doktor.Telefon;
            guncellenecekDoktor.Eposta = doktor.Eposta;
            guncellenecekDoktor.PoliklinikId = doktor.PoliklinikId;

            _context.SaveChanges();

            TempData["Basari"] = "Doktor kaydı başarıyla güncellendi.";

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

            if (!DoktorIslemiYetkisiVarMi())
            {
                TempData["Hata"] = "Doktor silme işlemi için yetkiniz yok.";
                return RedirectToAction("Index");
            }

            var doktor = _context.Doktorlar.FirstOrDefault(d => d.DoktorId == id && d.AktifMi);

            if (doktor == null)
            {
                TempData["Hata"] = "Doktor kaydı bulunamadı.";
                return RedirectToAction("Index");
            }

            doktor.AktifMi = false;

            _context.SaveChanges();

            TempData["Basari"] = "Doktor kaydı başarıyla silindi.";

            return RedirectToAction("Index");
        }
    }
}