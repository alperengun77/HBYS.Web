using HBYS.Web.Data;
using HBYS.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HBYS.Web.Controllers
{
    public class PoliklinikController : Controller
    {
        private readonly AppDbContext _context;

        public PoliklinikController(AppDbContext context)
        {
            _context = context;
        }

        private bool GirisYapildiMi()
        {
            string? kullaniciAdi = HttpContext.Session.GetString("KullaniciAdi");
            return !string.IsNullOrEmpty(kullaniciAdi);
        }

        private bool PoliklinikIslemiYetkisiVarMi()
        {
            string? rolAdi = HttpContext.Session.GetString("RolAdi");

            return rolAdi == "Admin" ||
                   rolAdi == "Yonetici";
        }

        public IActionResult Index(string? arama)
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction("Login", "Account");
            }

            var poliklinikler = _context.Poliklinikler
                .Include(p => p.Doktorlar)
                .Where(p => p.AktifMi)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(arama))
            {
                poliklinikler = poliklinikler.Where(p =>
                    p.PoliklinikAdi.Contains(arama) ||
                    (p.Aciklama != null && p.Aciklama.Contains(arama)));
            }

            ViewBag.Arama = arama;

            return View(poliklinikler.OrderBy(p => p.PoliklinikAdi).ToList());
        }

        [HttpGet]
        public IActionResult Create()
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction("Login", "Account");
            }

            if (!PoliklinikIslemiYetkisiVarMi())
            {
                TempData["Hata"] = "Bu işlem için yetkiniz yok.";
                return RedirectToAction("Index");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Poliklinik poliklinik)
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction("Login", "Account");
            }

            if (!PoliklinikIslemiYetkisiVarMi())
            {
                TempData["Hata"] = "Bu işlem için yetkiniz yok.";
                return RedirectToAction("Index");
            }

            bool poliklinikVarMi = _context.Poliklinikler.Any(p =>
                p.PoliklinikAdi == poliklinik.PoliklinikAdi &&
                p.AktifMi);

            if (poliklinikVarMi)
            {
                ModelState.AddModelError("PoliklinikAdi", "Bu poliklinik adı ile kayıtlı aktif poliklinik zaten var.");
            }

            if (!ModelState.IsValid)
            {
                return View(poliklinik);
            }

            poliklinik.AktifMi = true;

            _context.Poliklinikler.Add(poliklinik);
            _context.SaveChanges();

            TempData["Basari"] = "Poliklinik kaydı başarıyla eklendi.";

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction("Login", "Account");
            }

            if (!PoliklinikIslemiYetkisiVarMi())
            {
                TempData["Hata"] = "Bu işlem için yetkiniz yok.";
                return RedirectToAction("Index");
            }

            var poliklinik = _context.Poliklinikler
                .FirstOrDefault(p => p.PoliklinikId == id && p.AktifMi);

            if (poliklinik == null)
            {
                TempData["Hata"] = "Poliklinik kaydı bulunamadı.";
                return RedirectToAction("Index");
            }

            return View(poliklinik);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Poliklinik poliklinik)
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction("Login", "Account");
            }

            if (!PoliklinikIslemiYetkisiVarMi())
            {
                TempData["Hata"] = "Bu işlem için yetkiniz yok.";
                return RedirectToAction("Index");
            }

            bool poliklinikAdiBaskaKayittaVarMi = _context.Poliklinikler.Any(p =>
                p.PoliklinikAdi == poliklinik.PoliklinikAdi &&
                p.PoliklinikId != poliklinik.PoliklinikId &&
                p.AktifMi);

            if (poliklinikAdiBaskaKayittaVarMi)
            {
                ModelState.AddModelError("PoliklinikAdi", "Bu poliklinik adı başka bir kayıtta kullanılıyor.");
            }

            if (!ModelState.IsValid)
            {
                return View(poliklinik);
            }

            var guncellenecekPoliklinik = _context.Poliklinikler
                .FirstOrDefault(p => p.PoliklinikId == poliklinik.PoliklinikId && p.AktifMi);

            if (guncellenecekPoliklinik == null)
            {
                TempData["Hata"] = "Poliklinik kaydı bulunamadı.";
                return RedirectToAction("Index");
            }

            guncellenecekPoliklinik.PoliklinikAdi = poliklinik.PoliklinikAdi;
            guncellenecekPoliklinik.Aciklama = poliklinik.Aciklama;

            _context.SaveChanges();

            TempData["Basari"] = "Poliklinik kaydı başarıyla güncellendi.";

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

            if (!PoliklinikIslemiYetkisiVarMi())
            {
                TempData["Hata"] = "Poliklinik silme işlemi için yetkiniz yok.";
                return RedirectToAction("Index");
            }

            var poliklinik = _context.Poliklinikler
                .FirstOrDefault(p => p.PoliklinikId == id && p.AktifMi);

            if (poliklinik == null)
            {
                TempData["Hata"] = "Poliklinik kaydı bulunamadı.";
                return RedirectToAction("Index");
            }

            bool aktifDoktorVarMi = _context.Doktorlar.Any(d =>
                d.PoliklinikId == id &&
                d.AktifMi);

            if (aktifDoktorVarMi)
            {
                TempData["Hata"] = "Bu polikliniğe bağlı aktif doktor olduğu için silinemez.";
                return RedirectToAction("Index");
            }

            poliklinik.AktifMi = false;

            _context.SaveChanges();

            TempData["Basari"] = "Poliklinik kaydı başarıyla silindi.";

            return RedirectToAction("Index");
        }
    }
}
