using HBYS.Web.Data;
using HBYS.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HBYS.Web.Controllers
{
    public class LogController : Controller
    {
        private readonly AppDbContext _context;

        public LogController(AppDbContext context)
        {
            _context = context;
        }

        private bool GirisYapildiMi()
        {
            string? kullaniciAdi = HttpContext.Session.GetString("KullaniciAdi");
            return !string.IsNullOrEmpty(kullaniciAdi);
        }

        private bool LogYetkisiVarMi()
        {
            string? rolAdi = HttpContext.Session.GetString("RolAdi");

            return rolAdi == "Admin" ||
                   rolAdi == "Yonetici";
        }

        public IActionResult Index(string? modul, string? islemTipi, string? kullaniciAdi, DateTime? baslangicTarihi, DateTime? bitisTarihi)
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction("Login", "Account");
            }

            if (!LogYetkisiVarMi())
            {
                TempData["Hata"] = "Log kayıtlarını görüntüleme yetkiniz yok.";
                return RedirectToAction("Index", "Dashboard");
            }

            var loglar = _context.LogKayitlari.AsQueryable();

            if (!string.IsNullOrWhiteSpace(modul))
            {
                loglar = loglar.Where(l => l.Modul == modul);
            }

            if (!string.IsNullOrWhiteSpace(islemTipi))
            {
                loglar = loglar.Where(l => l.IslemTipi == islemTipi);
            }

            if (!string.IsNullOrWhiteSpace(kullaniciAdi))
            {
                loglar = loglar.Where(l => l.KullaniciAdi.Contains(kullaniciAdi));
            }

            if (baslangicTarihi.HasValue)
            {
                loglar = loglar.Where(l => l.Tarih >= baslangicTarihi.Value.Date);
            }

            if (bitisTarihi.HasValue)
            {
                DateTime bitis = bitisTarihi.Value.Date.AddDays(1);
                loglar = loglar.Where(l => l.Tarih < bitis);
            }

            ViewBag.Moduller = _context.LogKayitlari
                .Select(l => l.Modul)
                .Distinct()
                .OrderBy(m => m)
                .ToList();

            ViewBag.IslemTipleri = _context.LogKayitlari
                .Select(l => l.IslemTipi)
                .Distinct()
                .OrderBy(i => i)
                .ToList();

            ViewBag.SeciliModul = modul;
            ViewBag.SeciliIslemTipi = islemTipi;
            ViewBag.KullaniciAdi = kullaniciAdi;
            ViewBag.BaslangicTarihi = baslangicTarihi?.ToString("yyyy-MM-dd");
            ViewBag.BitisTarihi = bitisTarihi?.ToString("yyyy-MM-dd");

            List<LogKaydi> liste = loglar
                .OrderByDescending(l => l.Tarih)
                .Take(500)
                .ToList();

            return View(liste);
        }
    }
}