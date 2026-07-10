using HBYS.Web.Data;
using HBYS.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HBYS.Web.Controllers
{
    public class KullaniciController : Controller
    {
        private readonly AppDbContext _context;

        public KullaniciController(AppDbContext context)
        {
            _context = context;
        }

        private bool GirisYapildiMi()
        {
            string? kullaniciAdi = HttpContext.Session.GetString("KullaniciAdi");
            return !string.IsNullOrEmpty(kullaniciAdi);
        }

        private bool KullaniciIslemiYetkisiVarMi()
        {
            string? rolAdi = HttpContext.Session.GetString("RolAdi");

            return rolAdi == "Admin";
        }

        private void SecimListeleriniHazirla()
        {
            ViewBag.Roller = new SelectList(
                _context.Roller
                    .Where(r => r.AktifMi)
                    .OrderBy(r => r.RolAdi)
                    .ToList(),
                "RolId",
                "RolAdi"
            );

            var doktorListesi = _context.Doktorlar
                .Include(d => d.Poliklinik)
                .Where(d => d.AktifMi)
                .OrderBy(d => d.Ad)
                .ThenBy(d => d.Soyad)
                .ToList()
                .Select(d => new
                {
                    d.DoktorId,
                    DoktorBilgisi =
                        d.Ad + " " + d.Soyad +
                        " - " +
                        d.Poliklinik!.PoliklinikAdi
                })
                .ToList();

            ViewBag.Doktorlar = new SelectList(
                doktorListesi,
                "DoktorId",
                "DoktorBilgisi"
            );
        }

        public IActionResult Index(string? arama)
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction("Login", "Account");
            }

            if (!KullaniciIslemiYetkisiVarMi())
            {
                TempData["Hata"] = "Bu sayfaya erişim yetkiniz yok.";
                return RedirectToAction("Index", "Dashboard");
            }

            var kullanicilar = _context.Kullanicilar
                .Include(k => k.Rol)
                .Include(k => k.Doktor)
                .Where(k => k.AktifMi)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(arama))
            {
                kullanicilar = kullanicilar.Where(k =>
                    k.KullaniciAdi.Contains(arama) ||
                    k.AdSoyad.Contains(arama) ||
                    (k.Eposta != null && k.Eposta.Contains(arama)) ||
                    (k.Rol != null && k.Rol.RolAdi.Contains(arama)) ||
                    (k.Doktor != null && (k.Doktor.Ad.Contains(arama) || k.Doktor.Soyad.Contains(arama))));
            }

            ViewBag.Arama = arama;

            return View(kullanicilar
                .OrderByDescending(k => k.KayitTarihi)
                .ToList());
        }

        [HttpGet]
        public IActionResult Create()
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction("Login", "Account");
            }

            if (!KullaniciIslemiYetkisiVarMi())
            {
                TempData["Hata"] = "Bu işlem için yetkiniz yok.";
                return RedirectToAction("Index", "Dashboard");
            }

            SecimListeleriniHazirla();

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Kullanici kullanici)
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction("Login", "Account");
            }

            if (!KullaniciIslemiYetkisiVarMi())
            {
                TempData["Hata"] = "Bu işlem için yetkiniz yok.";
                return RedirectToAction("Index", "Dashboard");
            }

            if (kullanici.RolId <= 0)
            {
                ModelState.AddModelError("RolId", "Rol seçiniz.");
            }

            bool kullaniciAdiVarMi = _context.Kullanicilar.Any(k =>
                k.KullaniciAdi == kullanici.KullaniciAdi);

            if (kullaniciAdiVarMi)
            {
                ModelState.AddModelError("KullaniciAdi", "Bu kullanıcı adı zaten kullanılıyor.");
            }

            Rol? seciliRol = _context.Roller.FirstOrDefault(r =>
                r.RolId == kullanici.RolId &&
                r.AktifMi);

            if (seciliRol == null)
            {
                ModelState.AddModelError("RolId", "Seçilen rol bulunamadı.");
            }
            else
            {
                if (seciliRol.RolAdi == "Doktor" && kullanici.DoktorId == null)
                {
                    ModelState.AddModelError("DoktorId", "Doktor rolündeki kullanıcı için doktor seçiniz.");
                }

                if (seciliRol.RolAdi != "Doktor")
                {
                    kullanici.DoktorId = null;
                }
            }

            if (kullanici.DoktorId.HasValue)
            {
                bool doktorVarMi = _context.Doktorlar.Any(d =>
                    d.DoktorId == kullanici.DoktorId.Value &&
                    d.AktifMi);

                if (!doktorVarMi)
                {
                    ModelState.AddModelError("DoktorId", "Seçilen doktor bulunamadı.");
                }
            }

            if (!ModelState.IsValid)
            {
                SecimListeleriniHazirla();
                return View(kullanici);
            }

            kullanici.AktifMi = true;
            kullanici.KayitTarihi = DateTime.Now;

            _context.Kullanicilar.Add(kullanici);
            _context.SaveChanges();

            TempData["Basari"] = "Kullanıcı başarıyla oluşturuldu.";

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction("Login", "Account");
            }

            if (!KullaniciIslemiYetkisiVarMi())
            {
                TempData["Hata"] = "Bu işlem için yetkiniz yok.";
                return RedirectToAction("Index", "Dashboard");
            }

            var kullanici = _context.Kullanicilar
                .FirstOrDefault(k => k.KullaniciId == id && k.AktifMi);

            if (kullanici == null)
            {
                TempData["Hata"] = "Kullanıcı bulunamadı.";
                return RedirectToAction("Index");
            }

            SecimListeleriniHazirla();

            return View(kullanici);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Kullanici kullanici)
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction("Login", "Account");
            }

            if (!KullaniciIslemiYetkisiVarMi())
            {
                TempData["Hata"] = "Bu işlem için yetkiniz yok.";
                return RedirectToAction("Index", "Dashboard");
            }

            if (kullanici.RolId <= 0)
            {
                ModelState.AddModelError("RolId", "Rol seçiniz.");
            }

            bool kullaniciAdiBaskaKayittaVarMi = _context.Kullanicilar.Any(k =>
                k.KullaniciAdi == kullanici.KullaniciAdi &&
                k.KullaniciId != kullanici.KullaniciId);

            if (kullaniciAdiBaskaKayittaVarMi)
            {
                ModelState.AddModelError("KullaniciAdi", "Bu kullanıcı adı başka bir kullanıcıda kullanılıyor.");
            }

            Rol? seciliRol = _context.Roller.FirstOrDefault(r =>
                r.RolId == kullanici.RolId &&
                r.AktifMi);

            if (seciliRol == null)
            {
                ModelState.AddModelError("RolId", "Seçilen rol bulunamadı.");
            }
            else
            {
                if (seciliRol.RolAdi == "Doktor" && kullanici.DoktorId == null)
                {
                    ModelState.AddModelError("DoktorId", "Doktor rolündeki kullanıcı için doktor seçiniz.");
                }

                if (seciliRol.RolAdi != "Doktor")
                {
                    kullanici.DoktorId = null;
                }
            }

            if (kullanici.DoktorId.HasValue)
            {
                bool doktorVarMi = _context.Doktorlar.Any(d =>
                    d.DoktorId == kullanici.DoktorId.Value &&
                    d.AktifMi);

                if (!doktorVarMi)
                {
                    ModelState.AddModelError("DoktorId", "Seçilen doktor bulunamadı.");
                }
            }

            if (!ModelState.IsValid)
            {
                SecimListeleriniHazirla();
                return View(kullanici);
            }

            var guncellenecekKullanici = _context.Kullanicilar
                .FirstOrDefault(k => k.KullaniciId == kullanici.KullaniciId && k.AktifMi);

            if (guncellenecekKullanici == null)
            {
                TempData["Hata"] = "Kullanıcı bulunamadı.";
                return RedirectToAction("Index");
            }

            guncellenecekKullanici.KullaniciAdi = kullanici.KullaniciAdi;
            guncellenecekKullanici.Sifre = kullanici.Sifre;
            guncellenecekKullanici.AdSoyad = kullanici.AdSoyad;
            guncellenecekKullanici.Eposta = kullanici.Eposta;
            guncellenecekKullanici.RolId = kullanici.RolId;
            guncellenecekKullanici.DoktorId = kullanici.DoktorId;

            _context.SaveChanges();

            TempData["Basari"] = "Kullanıcı başarıyla güncellendi.";

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

            if (!KullaniciIslemiYetkisiVarMi())
            {
                TempData["Hata"] = "Bu işlem için yetkiniz yok.";
                return RedirectToAction("Index", "Dashboard");
            }

            int? mevcutKullaniciId = HttpContext.Session.GetInt32("KullaniciId");

            if (mevcutKullaniciId == id)
            {
                TempData["Hata"] = "Kendi hesabınızı silemezsiniz.";
                return RedirectToAction("Index");
            }

            var kullanici = _context.Kullanicilar
                .Include(k => k.Rol)
                .FirstOrDefault(k => k.KullaniciId == id && k.AktifMi);

            if (kullanici == null)
            {
                TempData["Hata"] = "Kullanıcı bulunamadı.";
                return RedirectToAction("Index");
            }

            if (kullanici.Rol != null && kullanici.Rol.RolAdi == "Admin")
            {
                int aktifAdminSayisi = _context.Kullanicilar
                    .Include(k => k.Rol)
                    .Count(k => k.AktifMi && k.Rol != null && k.Rol.RolAdi == "Admin");

                if (aktifAdminSayisi <= 1)
                {
                    TempData["Hata"] = "Sistemdeki son admin kullanıcısı silinemez.";
                    return RedirectToAction("Index");
                }
            }

            kullanici.AktifMi = false;

            _context.SaveChanges();

            TempData["Basari"] = "Kullanıcı başarıyla silindi.";

            return RedirectToAction("Index");
        }
    }
}