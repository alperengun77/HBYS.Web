using HBYS.Web.Data;
using HBYS.Web.Services;
using HBYS.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HBYS.Web.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;
        private readonly LogService _logService;

        public AccountController(AppDbContext context, LogService logService)
        {
            _context = context;
            _logService = logService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            string? mevcutKullanici = HttpContext.Session.GetString("KullaniciAdi");

            if (!string.IsNullOrEmpty(mevcutKullanici))
            {
                return RedirectToAction("Index", "Dashboard");
            }

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var kullanici = _context.Kullanicilar
                .Include(k => k.Rol)
                .Include(k => k.Doktor)
                .FirstOrDefault(k =>
                    k.KullaniciAdi == model.KullaniciAdi &&
                    k.Sifre == model.Sifre &&
                    k.AktifMi);

            if (kullanici == null)
            {
                _logService.Logla(
                    "Giriş",
                    "Başarısız Giriş",
                    "Kullanıcı Giriş Denemesi",
                    "Başarısız giriş denemesi yapıldı.",
                    model.KullaniciAdi
                );

                ModelState.AddModelError("", "Kullanıcı adı veya şifre hatalı.");
                return View(model);
            }

            HttpContext.Session.SetInt32("KullaniciId", kullanici.KullaniciId);
            HttpContext.Session.SetString("KullaniciAdi", kullanici.KullaniciAdi);
            HttpContext.Session.SetString("AdSoyad", kullanici.AdSoyad);
            HttpContext.Session.SetString("RolAdi", kullanici.Rol?.RolAdi ?? "");

            if (kullanici.DoktorId.HasValue)
            {
                HttpContext.Session.SetInt32("DoktorId", kullanici.DoktorId.Value);
            }
            else
            {
                HttpContext.Session.Remove("DoktorId");
            }

            _logService.Logla(
                "Giriş",
                "Başarılı Giriş",
                "Kullanıcı Girişi",
                "Kullanıcı sisteme başarıyla giriş yaptı.",
                kullanici.KullaniciAdi
            );

            return RedirectToAction("Index", "Dashboard");
        }

        public IActionResult Logout()
        {
            string? kullaniciAdi = HttpContext.Session.GetString("KullaniciAdi");

            _logService.Logla(
                "Giriş",
                "Çıkış",
                "Kullanıcı Çıkışı",
                "Kullanıcı sistemden çıkış yaptı.",
                kullaniciAdi
            );

            HttpContext.Session.Clear();

            return RedirectToAction("Login", "Account");
        }
    }
}