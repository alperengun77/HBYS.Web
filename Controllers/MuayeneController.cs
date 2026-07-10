using HBYS.Web.Data;
using HBYS.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using HBYS.Web.Helpers;

namespace HBYS.Web.Controllers
{
    public class MuayeneController : Controller
    {
        private readonly AppDbContext _context;

        public MuayeneController(AppDbContext context)
        {
            _context = context;
        }

        private bool GirisYapildiMi()
        {
            string? kullaniciAdi = HttpContext.Session.GetString("KullaniciAdi");
            return !string.IsNullOrEmpty(kullaniciAdi);
        }

        private bool MuayeneIslemiYetkisiVarMi()
        {
            string? rolAdi = HttpContext.Session.GetString("RolAdi");

            return rolAdi == "Admin" ||
                   rolAdi == "Doktor";
        }

        private void SecimListeleriniHazirla()
        {
            ViewBag.Hastalar = new SelectList(
                _context.Hastalar
                    .Where(h => h.AktifMi)
                    .OrderBy(h => h.Ad)
                    .ThenBy(h => h.Soyad)
                    .Select(h => new
                    {
                        h.HastaId,
                        HastaBilgisi = h.TcKimlikNo + " - " + h.Ad + " " + h.Soyad
                    })
                    .ToList(),
                "HastaId",
                "HastaBilgisi"
            );

            ViewBag.Doktorlar = new SelectList(
                _context.Doktorlar
                    .Include(d => d.Poliklinik)
                    .Where(d => d.AktifMi)
                    .OrderBy(d => d.Ad)
                    .ThenBy(d => d.Soyad)
                    .Select(d => new
                    {
                        d.DoktorId,
                        DoktorBilgisi = d.Ad + " " + d.Soyad + " - " + d.Poliklinik!.PoliklinikAdi
                    })
                    .ToList(),
                "DoktorId",
                "DoktorBilgisi"
            );

            var randevuListesi = _context.Randevular
                .Include(r => r.Hasta)
                .Include(r => r.Doktor)
                .Where(r => r.AktifMi && r.Durum != "Iptal")
                .OrderByDescending(r => r.RandevuTarihiSaati)
                .ToList()
                .Select(r => new
                {
                    r.RandevuId,
                    RandevuBilgisi =
                        r.RandevuTarihiSaati.ToString("dd.MM.yyyy HH:mm") +
                        " - " +
                        r.Hasta!.Ad + " " + r.Hasta.Soyad +
                        " / Dr. " +
                        r.Doktor!.Ad + " " + r.Doktor.Soyad +
                        " / " +
                        r.Durum
                })
                .ToList();

            ViewBag.Randevular = new SelectList(
                randevuListesi,
                "RandevuId",
                "RandevuBilgisi"
            );
        }

        public IActionResult Index(string? arama, DateTime? baslangicTarihi, DateTime? bitisTarihi)
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction("Login", "Account");
            }

            var muayeneler = _context.Muayeneler
                .Include(m => m.Hasta)
                .Include(m => m.Doktor)
                .Include(m => m.Randevu)
                .Where(m => m.AktifMi)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(arama))
            {
                muayeneler = muayeneler.Where(m =>
                    m.Hasta!.TcKimlikNo.Contains(arama) ||
                    m.Hasta.Ad.Contains(arama) ||
                    m.Hasta.Soyad.Contains(arama) ||
                    m.Doktor!.Ad.Contains(arama) ||
                    m.Doktor.Soyad.Contains(arama) ||
                    (m.Sikayet != null && m.Sikayet.Contains(arama)) ||
                    (m.Tani != null && m.Tani.Contains(arama)));
            }

            if (baslangicTarihi.HasValue)
            {
                DateTime baslangic = baslangicTarihi.Value.Date;
                muayeneler = muayeneler.Where(m => m.MuayeneTarihi >= baslangic);
            }

            if (bitisTarihi.HasValue)
            {
                DateTime bitis = bitisTarihi.Value.Date.AddDays(1);
                muayeneler = muayeneler.Where(m => m.MuayeneTarihi < bitis);
            }

            ViewBag.Arama = arama;
            ViewBag.BaslangicTarihi = baslangicTarihi?.ToString("yyyy-MM-dd");
            ViewBag.BitisTarihi = bitisTarihi?.ToString("yyyy-MM-dd");

            return View(muayeneler
                .OrderByDescending(m => m.MuayeneTarihi)
                .ToList());
        }

        [HttpGet]
        public IActionResult Create()
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction("Login", "Account");
            }

            if (!MuayeneIslemiYetkisiVarMi())
            {
                TempData["Hata"] = "Bu işlem için yetkiniz yok.";
                return RedirectToAction("Index");
            }

            SecimListeleriniHazirla();

            Muayene yeniMuayene = new Muayene
            {
                MuayeneTarihi = DateTime.Now
            };

            return View(yeniMuayene);
        }
        public IActionResult Details(int id)
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction("Login", "Account");
            }

            if (!MuayeneIslemiYetkisiVarMi())
            {
                TempData["Hata"] = "Bu sayfayı görüntüleme yetkiniz yok.";
                return RedirectToAction("Index", "Dashboard");
            }

            var muayene = _context.Muayeneler
                .Include(m => m.Hasta)
                .Include(m => m.Doktor)
                .Include(m => m.Randevu)
                .FirstOrDefault(m => m.MuayeneId == id && m.AktifMi);

            if (muayene == null)
            {
                TempData["Hata"] = "Muayene kaydı bulunamadı.";
                return RedirectToAction("Index");
            }

            ViewBag.Receteler = _context.Receteler
                .Where(r => r.MuayeneId == id && r.AktifMi)
                .OrderByDescending(r => r.KayitTarihi)
                .ToList();

            ViewBag.Tahliller = _context.Tahliller
                .Where(t => t.MuayeneId == id && t.AktifMi)
                .OrderByDescending(t => t.IstenmeTarihi)
                .ToList();

            return View(muayene);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Muayene muayene)
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction("Login", "Account");
            }

            if (!MuayeneIslemiYetkisiVarMi())
            {
                TempData["Hata"] = "Bu işlem için yetkiniz yok.";
                return RedirectToAction("Index");
            }

            if (muayene.HastaId <= 0)
            {
                ModelState.AddModelError("HastaId", "Hasta seçiniz.");
            }

            if (muayene.DoktorId <= 0)
            {
                ModelState.AddModelError("DoktorId", "Doktor seçiniz.");
            }

            Randevu? seciliRandevu = null;

            if (muayene.RandevuId.HasValue)
            {
                seciliRandevu = _context.Randevular.FirstOrDefault(r =>
                    r.RandevuId == muayene.RandevuId.Value &&
                    r.AktifMi);

                if (seciliRandevu == null)
                {
                    ModelState.AddModelError("RandevuId", "Seçilen randevu bulunamadı.");
                }
                else
                {
                    if (seciliRandevu.HastaId != muayene.HastaId)
                    {
                        ModelState.AddModelError("HastaId", "Seçilen hasta, randevudaki hasta ile uyuşmuyor.");
                    }

                    if (seciliRandevu.DoktorId != muayene.DoktorId)
                    {
                        ModelState.AddModelError("DoktorId", "Seçilen doktor, randevudaki doktor ile uyuşmuyor.");
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                SecimListeleriniHazirla();
                return View(muayene);
            }

            muayene.AktifMi = true;

            if (muayene.MuayeneTarihi == DateTime.MinValue)
            {
                muayene.MuayeneTarihi = DateTime.Now;
            }
            muayene.MuayeneTarihi = ValidationHelper.SaniyeVeSaliseyiTemizle(muayene.MuayeneTarihi);

            _context.Muayeneler.Add(muayene);

            if (seciliRandevu != null)
            {
                seciliRandevu.Durum = "Tamamlandi";
            }

            _context.SaveChanges();

            TempData["Basari"] = "Muayene kaydı başarıyla oluşturuldu.";

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction("Login", "Account");
            }

            if (!MuayeneIslemiYetkisiVarMi())
            {
                TempData["Hata"] = "Bu işlem için yetkiniz yok.";
                return RedirectToAction("Index");
            }

            var muayene = _context.Muayeneler
                .FirstOrDefault(m => m.MuayeneId == id && m.AktifMi);

            if (muayene == null)
            {
                TempData["Hata"] = "Muayene kaydı bulunamadı.";
                return RedirectToAction("Index");
            }

            SecimListeleriniHazirla();

            return View(muayene);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Muayene muayene)
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction("Login", "Account");
            }

            if (!MuayeneIslemiYetkisiVarMi())
            {
                TempData["Hata"] = "Bu işlem için yetkiniz yok.";
                return RedirectToAction("Index");
            }

            if (muayene.HastaId <= 0)
            {
                ModelState.AddModelError("HastaId", "Hasta seçiniz.");
            }

            if (muayene.DoktorId <= 0)
            {
                ModelState.AddModelError("DoktorId", "Doktor seçiniz.");
            }

            if (muayene.RandevuId.HasValue)
            {
                var seciliRandevu = _context.Randevular.FirstOrDefault(r =>
                    r.RandevuId == muayene.RandevuId.Value &&
                    r.AktifMi);

                if (seciliRandevu == null)
                {
                    ModelState.AddModelError("RandevuId", "Seçilen randevu bulunamadı.");
                }
                else
                {
                    if (seciliRandevu.HastaId != muayene.HastaId)
                    {
                        ModelState.AddModelError("HastaId", "Seçilen hasta, randevudaki hasta ile uyuşmuyor.");
                    }

                    if (seciliRandevu.DoktorId != muayene.DoktorId)
                    {
                        ModelState.AddModelError("DoktorId", "Seçilen doktor, randevudaki doktor ile uyuşmuyor.");
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                SecimListeleriniHazirla();
                return View(muayene);
            }

            var guncellenecekMuayene = _context.Muayeneler
                .FirstOrDefault(m => m.MuayeneId == muayene.MuayeneId && m.AktifMi);

            if (guncellenecekMuayene == null)
            {
                TempData["Hata"] = "Muayene kaydı bulunamadı.";
                return RedirectToAction("Index");
            }

            guncellenecekMuayene.HastaId = muayene.HastaId;
            guncellenecekMuayene.DoktorId = muayene.DoktorId;
            guncellenecekMuayene.RandevuId = muayene.RandevuId;
            guncellenecekMuayene.Sikayet = muayene.Sikayet;
            guncellenecekMuayene.Tani = muayene.Tani;
            guncellenecekMuayene.TedaviNotu = muayene.TedaviNotu;
            guncellenecekMuayene.MuayeneTarihi = ValidationHelper.SaniyeVeSaliseyiTemizle(muayene.MuayeneTarihi);

            _context.SaveChanges();

            TempData["Basari"] = "Muayene kaydı başarıyla güncellendi.";

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

            if (rolAdi != "Admin" && rolAdi != "Doktor")
            {
                TempData["Hata"] = "Muayene silme işlemi için yetkiniz yok.";
                return RedirectToAction("Index");
            }

            var muayene = _context.Muayeneler
                .FirstOrDefault(m => m.MuayeneId == id && m.AktifMi);

            if (muayene == null)
            {
                TempData["Hata"] = "Muayene kaydı bulunamadı.";
                return RedirectToAction("Index");
            }

            muayene.AktifMi = false;

            _context.SaveChanges();

            TempData["Basari"] = "Muayene kaydı başarıyla silindi.";

            return RedirectToAction("Index");
        }
    }
}
