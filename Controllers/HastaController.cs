using HBYS.Web.Data;
using HBYS.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HBYS.Web.Helpers;

namespace HBYS.Web.Controllers
{
    public class HastaController : Controller
    {
        private readonly AppDbContext _context;

        public HastaController(AppDbContext context)
        {
            _context = context;
        }

        private bool GirisYapildiMi()
        {
            string? kullaniciAdi = HttpContext.Session.GetString("KullaniciAdi");
            return !string.IsNullOrEmpty(kullaniciAdi);
        }

        private bool HastaIslemiYetkisiVarMi()
        {
            string? rolAdi = HttpContext.Session.GetString("RolAdi");

            return rolAdi == "Admin" ||
                   rolAdi == "Sekreter" ||
                   rolAdi == "Doktor";
        }

        public IActionResult Index(string? arama, string? cinsiyet, string? kanGrubu)
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction("Login", "Account");
            }

            if (!HastaIslemiYetkisiVarMi())
            {
                TempData["Hata"] = "Bu sayfaya erişim yetkiniz yok.";
                return RedirectToAction("Index", "Dashboard");
            }

            var hastalar = _context.Hastalar
                .Where(h => h.AktifMi)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(arama))
            {
                hastalar = hastalar.Where(h =>
                    h.TcKimlikNo.Contains(arama) ||
                    h.Ad.Contains(arama) ||
                    h.Soyad.Contains(arama) ||
                    (h.Telefon != null && h.Telefon.Contains(arama)) ||
                    (h.Eposta != null && h.Eposta.Contains(arama)));
            }

            if (!string.IsNullOrWhiteSpace(cinsiyet))
            {
                hastalar = hastalar.Where(h => h.Cinsiyet == cinsiyet);
            }

            if (!string.IsNullOrWhiteSpace(kanGrubu))
            {
                hastalar = hastalar.Where(h => h.KanGrubu == kanGrubu);
            }

            ViewBag.Arama = arama;
            ViewBag.Cinsiyet = cinsiyet;
            ViewBag.KanGrubu = kanGrubu;

            return View(hastalar
                .OrderByDescending(h => h.KayitTarihi)
                .ToList());
        }

        [HttpGet]
        public IActionResult Create()
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction("Login", "Account");
            }

            if (!HastaIslemiYetkisiVarMi())
            {
                TempData["Hata"] = "Bu işlem için yetkiniz yok.";
                return RedirectToAction("Index");
            }

            return View();
        }
        public IActionResult Details(int id)
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction("Login", "Account");
            }

            if (!HastaIslemiYetkisiVarMi())
            {
                TempData["Hata"] = "Bu sayfayı görüntüleme yetkiniz yok.";
                return RedirectToAction("Index", "Dashboard");
            }

            var hasta = _context.Hastalar
                .FirstOrDefault(h => h.HastaId == id && h.AktifMi);

            if (hasta == null)
            {
                TempData["Hata"] = "Hasta kaydı bulunamadı.";
                return RedirectToAction("Index");
            }

            ViewBag.Randevular = _context.Randevular
                .Include(r => r.Doktor)
                .Include(r => r.Poliklinik)
                .Where(r => r.HastaId == id && r.AktifMi)
                .OrderByDescending(r => r.RandevuTarihiSaati)
                .ToList();

            ViewBag.Muayeneler = _context.Muayeneler
                .Include(m => m.Doktor)
                .Where(m => m.HastaId == id && m.AktifMi)
                .OrderByDescending(m => m.MuayeneTarihi)
                .ToList();

            ViewBag.Receteler = _context.Receteler
                .Include(r => r.Muayene)
                    .ThenInclude(m => m!.Doktor)
                .Where(r =>
                    r.AktifMi &&
                    r.Muayene != null &&
                    r.Muayene.HastaId == id)
                .OrderByDescending(r => r.KayitTarihi)
                .ToList();

            ViewBag.Tahliller = _context.Tahliller
                .Include(t => t.Muayene)
                    .ThenInclude(m => m!.Doktor)
                .Where(t =>
                    t.AktifMi &&
                    t.Muayene != null &&
                    t.Muayene.HastaId == id)
                .OrderByDescending(t => t.IstenmeTarihi)
                .ToList();

            return View(hasta);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Hasta hasta)
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction("Login", "Account");
            }

            if (!HastaIslemiYetkisiVarMi())
            {
                TempData["Hata"] = "Bu işlem için yetkiniz yok.";
                return RedirectToAction("Index");
            }

            if (!ValidationHelper.TcKimlikNoGecerliMi(hasta.TcKimlikNo))
            {
                ModelState.AddModelError("TcKimlikNo", "TC kimlik no 11 haneli olmalı ve sadece rakamlardan oluşmalıdır.");
            }

            if (!ValidationHelper.TelefonGecerliMi(hasta.Telefon))
            {
                ModelState.AddModelError("Telefon", "Telefon numarası sadece rakamlardan oluşmalı ve 10 veya 11 haneli olmalıdır.");
            }

            bool tcVarMi = _context.Hastalar.Any(h =>
                h.TcKimlikNo == hasta.TcKimlikNo &&
                h.AktifMi);

            if (tcVarMi)
            {
                ModelState.AddModelError("TcKimlikNo", "Bu TC kimlik numarası ile kayıtlı aktif hasta zaten var.");
            }

            if (!ModelState.IsValid)
            {
                return View(hasta);
            }

            hasta.AktifMi = true;
            hasta.KayitTarihi = DateTime.Now;

            _context.Hastalar.Add(hasta);
            _context.SaveChanges();

            TempData["Basari"] = "Hasta kaydı başarıyla eklendi.";

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction("Login", "Account");
            }

            if (!HastaIslemiYetkisiVarMi())
            {
                TempData["Hata"] = "Bu işlem için yetkiniz yok.";
                return RedirectToAction("Index");
            }

            var hasta = _context.Hastalar.FirstOrDefault(h => h.HastaId == id && h.AktifMi);

            if (hasta == null)
            {
                TempData["Hata"] = "Hasta kaydı bulunamadı.";
                return RedirectToAction("Index");
            }

            return View(hasta);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Hasta hasta)
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction("Login", "Account");
            }

            if (!HastaIslemiYetkisiVarMi())
            {
                TempData["Hata"] = "Bu işlem için yetkiniz yok.";
                return RedirectToAction("Index");
            }

            if (!ValidationHelper.TcKimlikNoGecerliMi(hasta.TcKimlikNo))
            {
                ModelState.AddModelError("TcKimlikNo", "TC kimlik no 11 haneli olmalı ve sadece rakamlardan oluşmalıdır.");
            }

            if (!ValidationHelper.TelefonGecerliMi(hasta.Telefon))
            {
                ModelState.AddModelError("Telefon", "Telefon numarası sadece rakamlardan oluşmalı ve 10 veya 11 haneli olmalıdır.");
            }

            bool tcBaskaHastadaVarMi = _context.Hastalar.Any(h =>
                h.TcKimlikNo == hasta.TcKimlikNo &&
                h.HastaId != hasta.HastaId &&
                h.AktifMi);

            if (tcBaskaHastadaVarMi)
            {
                ModelState.AddModelError("TcKimlikNo", "Bu TC kimlik numarası başka bir hastada kullanılıyor.");
            }

            if (!ModelState.IsValid)
            {
                return View(hasta);
            }

            var guncellenecekHasta = _context.Hastalar
                .FirstOrDefault(h => h.HastaId == hasta.HastaId && h.AktifMi);

            if (guncellenecekHasta == null)
            {
                TempData["Hata"] = "Hasta kaydı bulunamadı.";
                return RedirectToAction("Index");
            }

            guncellenecekHasta.TcKimlikNo = hasta.TcKimlikNo;
            guncellenecekHasta.Ad = hasta.Ad;
            guncellenecekHasta.Soyad = hasta.Soyad;
            guncellenecekHasta.DogumTarihi = hasta.DogumTarihi;
            guncellenecekHasta.Cinsiyet = hasta.Cinsiyet;
            guncellenecekHasta.Telefon = hasta.Telefon;
            guncellenecekHasta.Eposta = hasta.Eposta;
            guncellenecekHasta.Adres = hasta.Adres;
            guncellenecekHasta.KanGrubu = hasta.KanGrubu;

            _context.SaveChanges();

            TempData["Basari"] = "Hasta kaydı başarıyla güncellendi.";

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

            string? rolAdi = HttpContext.Session.GetString("RolAdi");

            if (rolAdi != "Admin" && rolAdi != "Sekreter")
            {
                TempData["Hata"] = "Hasta silme işlemi için yetkiniz yok.";
                return RedirectToAction("Index");
            }

            var hasta = _context.Hastalar.FirstOrDefault(h => h.HastaId == id && h.AktifMi);

            if (hasta == null)
            {
                TempData["Hata"] = "Hasta kaydı bulunamadı.";
                return RedirectToAction("Index");
            }

            hasta.AktifMi = false;

            _context.SaveChanges();

            TempData["Basari"] = "Hasta kaydı başarıyla silindi.";

            return RedirectToAction("Index");
        }
    }
}