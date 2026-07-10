using HBYS.Web.Data;
using HBYS.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HBYS.Web.Controllers
{
    public class ReceteController : Controller
    {
        private readonly AppDbContext _context;

        public ReceteController(AppDbContext context)
        {
            _context = context;
        }

        private bool GirisYapildiMi()
        {
            string? kullaniciAdi = HttpContext.Session.GetString("KullaniciAdi");
            return !string.IsNullOrEmpty(kullaniciAdi);
        }

        private bool ReceteGormeYetkisiVarMi()
        {
            string? rolAdi = HttpContext.Session.GetString("RolAdi");

            return rolAdi == "Admin" ||
                   rolAdi == "Yonetici" ||
                   rolAdi == "Doktor" ||
                   rolAdi == "Eczane";
        }

        private bool ReceteIslemiYetkisiVarMi()
        {
            string? rolAdi = HttpContext.Session.GetString("RolAdi");

            return rolAdi == "Admin" ||
                   rolAdi == "Yonetici" ||
                   rolAdi == "Doktor";
        }

        private bool DoktorMu()
        {
            string? rolAdi = HttpContext.Session.GetString("RolAdi");
            return rolAdi == "Doktor";
        }

        private int? AktifDoktorId()
        {
            return HttpContext.Session.GetInt32("DoktorId");
        }

        private void SecimListeleriniHazirla(int? seciliMuayeneId = null)
        {
            var muayenelerQuery = _context.Muayeneler
                .Include(m => m.Hasta)
                .Include(m => m.Doktor)
                .Where(m => m.AktifMi);

            if (DoktorMu())
            {
                int? aktifDoktorId = AktifDoktorId();

                if (aktifDoktorId.HasValue)
                {
                    muayenelerQuery = muayenelerQuery.Where(m => m.DoktorId == aktifDoktorId.Value);
                }
                else
                {
                    muayenelerQuery = muayenelerQuery.Where(m => false);
                }
            }

            ViewBag.Muayeneler = new SelectList(
                muayenelerQuery
                    .OrderByDescending(m => m.MuayeneTarihi)
                    .Select(m => new
                    {
                        m.MuayeneId,
                        Bilgi = m.MuayeneTarihi.ToString("dd.MM.yyyy HH:mm") + " - " +
                                m.Hasta!.Ad + " " + m.Hasta.Soyad + " - Dr. " +
                                m.Doktor!.Ad + " " + m.Doktor.Soyad
                    })
                    .ToList(),
                "MuayeneId",
                "Bilgi",
                seciliMuayeneId
            );

            ViewBag.Ilaclar = new SelectList(
                _context.Ilaclar
                    .Where(i => i.AktifMi)
                    .OrderBy(i => i.IlacAdi)
                    .ToList(),
                "IlacAdi",
                "IlacAdi"
            );

            ViewBag.Dozlar = new SelectList(
                _context.IlacDozlari
                    .Where(d => d.AktifMi)
                    .OrderBy(d => d.DozAdi)
                    .ToList(),
                "DozAdi",
                "DozAdi"
            );

            ViewBag.KullanimSekilleri = new SelectList(
                _context.IlacKullanimSekilleri
                    .Where(k => k.AktifMi)
                    .OrderBy(k => k.KullanimSekliAdi)
                    .ToList(),
                "KullanimSekliAdi",
                "KullanimSekliAdi"
            );

            ViewBag.KullanimSureleri = new SelectList(
                _context.IlacKullanimSureleri
                    .Where(s => s.AktifMi)
                    .OrderBy(s => s.KullanimSuresiAdi)
                    .ToList(),
                "KullanimSuresiAdi",
                "KullanimSuresiAdi"
            );
        }

        public IActionResult Index(string? arama)
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction("Login", "Account");
            }

            if (!ReceteGormeYetkisiVarMi())
            {
                TempData["Hata"] = "Reçete ekranına erişim yetkiniz yok.";
                return RedirectToAction("Index", "Dashboard");
            }

            var receteler = _context.Receteler
                .Include(r => r.Muayene)
                    .ThenInclude(m => m!.Hasta)
                .Include(r => r.Muayene)
                    .ThenInclude(m => m!.Doktor)
                .Where(r => r.AktifMi)
                .AsQueryable();

            if (DoktorMu())
            {
                int? aktifDoktorId = AktifDoktorId();

                if (aktifDoktorId.HasValue)
                {
                    receteler = receteler.Where(r =>
                        r.Muayene != null &&
                        r.Muayene.DoktorId == aktifDoktorId.Value);
                }
                else
                {
                    receteler = receteler.Where(r => false);
                    TempData["Hata"] = "Bu doktor kullanıcısı herhangi bir doktor kaydıyla eşleştirilmemiş.";
                }
            }

            if (!string.IsNullOrWhiteSpace(arama))
            {
                receteler = receteler.Where(r =>
                    r.IlacAdi.Contains(arama) ||
                    r.Doz.Contains(arama) ||
                    r.KullanimSekli.Contains(arama) ||
                    r.KullanimSuresi.Contains(arama) ||
                    r.Muayene!.Hasta!.Ad.Contains(arama) ||
                    r.Muayene.Hasta.Soyad.Contains(arama));
            }

            ViewBag.Arama = arama;
            ViewBag.IslemYetkisiVarMi = ReceteIslemiYetkisiVarMi();

            List<Recete> liste = receteler
                .OrderByDescending(r => r.KayitTarihi)
                .ToList();

            return View(liste);
        }

        [HttpGet]
        public IActionResult Create()
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction("Login", "Account");
            }

            if (!ReceteIslemiYetkisiVarMi())
            {
                TempData["Hata"] = "Reçete ekleme yetkiniz yok.";
                return RedirectToAction("Index");
            }

            SecimListeleriniHazirla();

            Recete yeniRecete = new Recete
            {
                KayitTarihi = DateTime.Now,
                AktifMi = true
            };

            return View(yeniRecete);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Recete recete)
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction("Login", "Account");
            }

            if (!ReceteIslemiYetkisiVarMi())
            {
                TempData["Hata"] = "Reçete ekleme yetkiniz yok.";
                return RedirectToAction("Index");
            }

            if (recete.MuayeneId <= 0)
            {
                ModelState.AddModelError("MuayeneId", "Muayene seçiniz.");
            }

            if (string.IsNullOrWhiteSpace(recete.IlacAdi))
            {
                ModelState.AddModelError("IlacAdi", "İlaç seçiniz.");
            }

            if (string.IsNullOrWhiteSpace(recete.Doz))
            {
                ModelState.AddModelError("Doz", "Doz seçiniz.");
            }

            if (string.IsNullOrWhiteSpace(recete.KullanimSekli))
            {
                ModelState.AddModelError("KullanimSekli", "Kullanım şekli seçiniz.");
            }

            if (string.IsNullOrWhiteSpace(recete.KullanimSuresi))
            {
                ModelState.AddModelError("KullanimSuresi", "Kullanım süresi seçiniz.");
            }

            Muayene? muayene = _context.Muayeneler.FirstOrDefault(m =>
                m.MuayeneId == recete.MuayeneId &&
                m.AktifMi);

            if (muayene == null)
            {
                ModelState.AddModelError("MuayeneId", "Seçilen muayene bulunamadı.");
            }

            if (DoktorMu())
            {
                int? aktifDoktorId = AktifDoktorId();

                if (!aktifDoktorId.HasValue || muayene == null || muayene.DoktorId != aktifDoktorId.Value)
                {
                    ModelState.AddModelError("MuayeneId", "Başka doktora ait muayeneye reçete ekleyemezsiniz.");
                }
            }

            bool ilacVarMi = _context.Ilaclar.Any(i => i.AktifMi && i.IlacAdi == recete.IlacAdi);
            bool dozVarMi = _context.IlacDozlari.Any(d => d.AktifMi && d.DozAdi == recete.Doz);
            bool kullanimSekliVarMi = _context.IlacKullanimSekilleri.Any(k => k.AktifMi && k.KullanimSekliAdi == recete.KullanimSekli);
            bool kullanimSuresiVarMi = _context.IlacKullanimSureleri.Any(s => s.AktifMi && s.KullanimSuresiAdi == recete.KullanimSuresi);

            if (!ilacVarMi)
            {
                ModelState.AddModelError("IlacAdi", "Seçilen ilaç listede bulunamadı.");
            }

            if (!dozVarMi)
            {
                ModelState.AddModelError("Doz", "Seçilen doz listede bulunamadı.");
            }

            if (!kullanimSekliVarMi)
            {
                ModelState.AddModelError("KullanimSekli", "Seçilen kullanım şekli listede bulunamadı.");
            }

            if (!kullanimSuresiVarMi)
            {
                ModelState.AddModelError("KullanimSuresi", "Seçilen kullanım süresi listede bulunamadı.");
            }

            if (!ModelState.IsValid)
            {
                SecimListeleriniHazirla(recete.MuayeneId);
                return View(recete);
            }

            recete.AktifMi = true;
            recete.KayitTarihi = DateTime.Now;

            _context.Receteler.Add(recete);
            _context.SaveChanges();

            TempData["Basari"] = "Reçete başarıyla eklendi.";

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction("Login", "Account");
            }

            if (!ReceteIslemiYetkisiVarMi())
            {
                TempData["Hata"] = "Reçete düzenleme yetkiniz yok.";
                return RedirectToAction("Index");
            }

            Recete? recete = _context.Receteler
                .Include(r => r.Muayene)
                .FirstOrDefault(r => r.ReceteId == id && r.AktifMi);

            if (recete == null)
            {
                TempData["Hata"] = "Reçete bulunamadı.";
                return RedirectToAction("Index");
            }

            if (DoktorMu())
            {
                int? aktifDoktorId = AktifDoktorId();

                if (!aktifDoktorId.HasValue || recete.Muayene == null || recete.Muayene.DoktorId != aktifDoktorId.Value)
                {
                    TempData["Hata"] = "Başka doktora ait reçeteyi düzenleyemezsiniz.";
                    return RedirectToAction("Index");
                }
            }

            SecimListeleriniHazirla(recete.MuayeneId);

            return View(recete);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Recete recete)
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction("Login", "Account");
            }

            if (!ReceteIslemiYetkisiVarMi())
            {
                TempData["Hata"] = "Reçete düzenleme yetkiniz yok.";
                return RedirectToAction("Index");
            }

            Recete? guncellenecekRecete = _context.Receteler
                .Include(r => r.Muayene)
                .FirstOrDefault(r => r.ReceteId == recete.ReceteId && r.AktifMi);

            if (guncellenecekRecete == null)
            {
                TempData["Hata"] = "Reçete bulunamadı.";
                return RedirectToAction("Index");
            }

            if (DoktorMu())
            {
                int? aktifDoktorId = AktifDoktorId();

                if (!aktifDoktorId.HasValue || guncellenecekRecete.Muayene == null || guncellenecekRecete.Muayene.DoktorId != aktifDoktorId.Value)
                {
                    TempData["Hata"] = "Başka doktora ait reçeteyi düzenleyemezsiniz.";
                    return RedirectToAction("Index");
                }
            }

            if (recete.MuayeneId <= 0)
            {
                ModelState.AddModelError("MuayeneId", "Muayene seçiniz.");
            }

            if (string.IsNullOrWhiteSpace(recete.IlacAdi))
            {
                ModelState.AddModelError("IlacAdi", "İlaç seçiniz.");
            }

            if (string.IsNullOrWhiteSpace(recete.Doz))
            {
                ModelState.AddModelError("Doz", "Doz seçiniz.");
            }

            if (string.IsNullOrWhiteSpace(recete.KullanimSekli))
            {
                ModelState.AddModelError("KullanimSekli", "Kullanım şekli seçiniz.");
            }

            if (string.IsNullOrWhiteSpace(recete.KullanimSuresi))
            {
                ModelState.AddModelError("KullanimSuresi", "Kullanım süresi seçiniz.");
            }

            Muayene? muayene = _context.Muayeneler.FirstOrDefault(m =>
                m.MuayeneId == recete.MuayeneId &&
                m.AktifMi);

            if (muayene == null)
            {
                ModelState.AddModelError("MuayeneId", "Seçilen muayene bulunamadı.");
            }

            if (DoktorMu())
            {
                int? aktifDoktorId = AktifDoktorId();

                if (!aktifDoktorId.HasValue || muayene == null || muayene.DoktorId != aktifDoktorId.Value)
                {
                    ModelState.AddModelError("MuayeneId", "Başka doktora ait muayeneye reçete bağlayamazsınız.");
                }
            }

            bool ilacVarMi = _context.Ilaclar.Any(i => i.AktifMi && i.IlacAdi == recete.IlacAdi);
            bool dozVarMi = _context.IlacDozlari.Any(d => d.AktifMi && d.DozAdi == recete.Doz);
            bool kullanimSekliVarMi = _context.IlacKullanimSekilleri.Any(k => k.AktifMi && k.KullanimSekliAdi == recete.KullanimSekli);
            bool kullanimSuresiVarMi = _context.IlacKullanimSureleri.Any(s => s.AktifMi && s.KullanimSuresiAdi == recete.KullanimSuresi);

            if (!ilacVarMi)
            {
                ModelState.AddModelError("IlacAdi", "Seçilen ilaç listede bulunamadı.");
            }

            if (!dozVarMi)
            {
                ModelState.AddModelError("Doz", "Seçilen doz listede bulunamadı.");
            }

            if (!kullanimSekliVarMi)
            {
                ModelState.AddModelError("KullanimSekli", "Seçilen kullanım şekli listede bulunamadı.");
            }

            if (!kullanimSuresiVarMi)
            {
                ModelState.AddModelError("KullanimSuresi", "Seçilen kullanım süresi listede bulunamadı.");
            }

            if (!ModelState.IsValid)
            {
                SecimListeleriniHazirla(recete.MuayeneId);
                return View(recete);
            }

            guncellenecekRecete.MuayeneId = recete.MuayeneId;
            guncellenecekRecete.IlacAdi = recete.IlacAdi;
            guncellenecekRecete.Doz = recete.Doz;
            guncellenecekRecete.KullanimSekli = recete.KullanimSekli;
            guncellenecekRecete.KullanimSuresi = recete.KullanimSuresi;
            guncellenecekRecete.Aciklama = recete.Aciklama;

            _context.Receteler.Update(guncellenecekRecete);
            _context.SaveChanges();

            TempData["Basari"] = "Reçete başarıyla güncellendi.";

            return RedirectToAction("Index");
        }

        public IActionResult Details(int id)
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction("Login", "Account");
            }

            if (!ReceteGormeYetkisiVarMi())
            {
                TempData["Hata"] = "Reçete detayına erişim yetkiniz yok.";
                return RedirectToAction("Index", "Dashboard");
            }

            Recete? recete = _context.Receteler
                .Include(r => r.Muayene)
                    .ThenInclude(m => m!.Hasta)
                .Include(r => r.Muayene)
                    .ThenInclude(m => m!.Doktor)
                .FirstOrDefault(r => r.ReceteId == id && r.AktifMi);

            if (recete == null)
            {
                TempData["Hata"] = "Reçete bulunamadı.";
                return RedirectToAction("Index");
            }

            if (DoktorMu())
            {
                int? aktifDoktorId = AktifDoktorId();

                if (!aktifDoktorId.HasValue || recete.Muayene == null || recete.Muayene.DoktorId != aktifDoktorId.Value)
                {
                    TempData["Hata"] = "Başka doktora ait reçete detayını görüntüleyemezsiniz.";
                    return RedirectToAction("Index");
                }
            }

            return View(recete);
        }

        public IActionResult Delete(int id)
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction("Login", "Account");
            }

            if (!ReceteIslemiYetkisiVarMi())
            {
                TempData["Hata"] = "Reçete silme yetkiniz yok.";
                return RedirectToAction("Index");
            }

            Recete? recete = _context.Receteler
                .Include(r => r.Muayene)
                .FirstOrDefault(r => r.ReceteId == id && r.AktifMi);

            if (recete == null)
            {
                TempData["Hata"] = "Reçete bulunamadı.";
                return RedirectToAction("Index");
            }

            if (DoktorMu())
            {
                int? aktifDoktorId = AktifDoktorId();

                if (!aktifDoktorId.HasValue || recete.Muayene == null || recete.Muayene.DoktorId != aktifDoktorId.Value)
                {
                    TempData["Hata"] = "Başka doktora ait reçeteyi silemezsiniz.";
                    return RedirectToAction("Index");
                }
            }

            recete.AktifMi = false;

            _context.Receteler.Update(recete);
            _context.SaveChanges();

            TempData["Basari"] = "Reçete başarıyla silindi.";

            return RedirectToAction("Index");
        }
    }
}