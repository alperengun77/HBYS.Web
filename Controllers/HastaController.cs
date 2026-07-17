using HBYS.Web.Data;
using HBYS.Web.Helpers;
using HBYS.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
            string? kullaniciAdi =
                HttpContext.Session.GetString("KullaniciAdi");

            return !string.IsNullOrEmpty(kullaniciAdi);
        }

        private bool HastaIslemiYetkisiVarMi()
        {
            string? rolAdi =
                HttpContext.Session.GetString("RolAdi");

            return rolAdi == "Admin" ||
                   rolAdi == "Sekreter" ||
                   rolAdi == "Doktor";
        }

        private bool DoktorMu()
        {
            string? rolAdi =
                HttpContext.Session.GetString("RolAdi");

            return rolAdi == "Doktor";
        }

        private int? AktifDoktorId()
        {
            return HttpContext.Session.GetInt32("DoktorId");
        }

        /*
            Doktorun bu hastayla herhangi bir aktif
            randevu veya muayene bağlantısı var mı
            kontrol edilir.

            Admin ve sekreter için bu kontrol
            doğrudan true döndürür.
        */
        private bool HastaKaydinaErisebilirMi(int hastaId)
        {
            if (!DoktorMu())
            {
                return true;
            }

            int? doktorId = AktifDoktorId();

            if (!doktorId.HasValue)
            {
                return false;
            }

            bool randevusuVarMi =
                _context.Randevular.Any(r =>
                    r.AktifMi &&
                    r.HastaId == hastaId &&
                    r.DoktorId == doktorId.Value
                );

            bool muayenesiVarMi =
                _context.Muayeneler.Any(m =>
                    m.AktifMi &&
                    m.HastaId == hastaId &&
                    m.DoktorId == doktorId.Value
                );

            return randevusuVarMi || muayenesiVarMi;
        }

        public IActionResult Index(
            string? arama,
            string? cinsiyet,
            string? kanGrubu
        )
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            if (!HastaIslemiYetkisiVarMi())
            {
                TempData["Hata"] =
                    "Bu sayfaya erişim yetkiniz yok.";

                return RedirectToAction(
                    "Index",
                    "Dashboard"
                );
            }

            IQueryable<Hasta> hastalar =
                _context.Hastalar
                    .Where(h => h.AktifMi);

            /*
                Doktor giriş yaptıysa sadece bu doktorla
                randevu veya muayene bağlantısı bulunan
                hastalar listelenir.
            */
            if (DoktorMu())
            {
                int? doktorId = AktifDoktorId();

                if (!doktorId.HasValue)
                {
                    hastalar =
                        hastalar.Where(h => false);

                    TempData["Hata"] =
                        "Doktor kullanıcısı herhangi bir doktor kaydıyla eşleştirilmemiş.";
                }
                else
                {
                    int aktifDoktorId =
                        doktorId.Value;

                    hastalar =
                        hastalar.Where(h =>
                            _context.Randevular.Any(r =>
                                r.AktifMi &&
                                r.HastaId == h.HastaId &&
                                r.DoktorId == aktifDoktorId
                            ) ||
                            _context.Muayeneler.Any(m =>
                                m.AktifMi &&
                                m.HastaId == h.HastaId &&
                                m.DoktorId == aktifDoktorId
                            )
                        );
                }
            }

            if (!string.IsNullOrWhiteSpace(arama))
            {
                hastalar =
                    hastalar.Where(h =>
                        h.TcKimlikNo.Contains(arama) ||
                        h.Ad.Contains(arama) ||
                        h.Soyad.Contains(arama) ||
                        (
                            h.Telefon != null &&
                            h.Telefon.Contains(arama)
                        ) ||
                        (
                            h.Eposta != null &&
                            h.Eposta.Contains(arama)
                        )
                    );
            }

            if (!string.IsNullOrWhiteSpace(cinsiyet))
            {
                hastalar =
                    hastalar.Where(h =>
                        h.Cinsiyet == cinsiyet
                    );
            }

            if (!string.IsNullOrWhiteSpace(kanGrubu))
            {
                hastalar =
                    hastalar.Where(h =>
                        h.KanGrubu == kanGrubu
                    );
            }

            ViewBag.Arama = arama;
            ViewBag.Cinsiyet = cinsiyet;
            ViewBag.KanGrubu = kanGrubu;
            ViewBag.DoktorMu = DoktorMu();

            List<Hasta> hastaListesi =
                hastalar
                    .OrderByDescending(h =>
                        h.KayitTarihi
                    )
                    .ToList();

            return View(hastaListesi);
        }

        [HttpGet]
        public IActionResult Create()
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            if (!HastaIslemiYetkisiVarMi())
            {
                TempData["Hata"] =
                    "Bu işlem için yetkiniz yok.";

                return RedirectToAction("Index");
            }

            return View();
        }

        public IActionResult Details(int id)
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            if (!HastaIslemiYetkisiVarMi())
            {
                TempData["Hata"] =
                    "Bu sayfayı görüntüleme yetkiniz yok.";

                return RedirectToAction(
                    "Index",
                    "Dashboard"
                );
            }

            /*
                Doktor başka doktora ait bir hasta
                kaydına URL üzerinden de erişemez.
            */
            if (!HastaKaydinaErisebilirMi(id))
            {
                TempData["Hata"] =
                    "Bu hasta sizin randevu veya muayene kayıtlarınızla bağlantılı değildir.";

                return RedirectToAction("Index");
            }

            Hasta? hasta =
                _context.Hastalar
                    .FirstOrDefault(h =>
                        h.HastaId == id &&
                        h.AktifMi
                    );

            if (hasta == null)
            {
                TempData["Hata"] =
                    "Hasta kaydı bulunamadı.";

                return RedirectToAction("Index");
            }

            int? doktorId =
                DoktorMu()
                    ? AktifDoktorId()
                    : null;

            /*
                Önce hastanın bütün aktif randevularının
                sorgusu hazırlanır.
            */
            IQueryable<Randevu> randevuSorgusu =
                _context.Randevular
                    .Include(r => r.Doktor)
                    .Include(r => r.Poliklinik)
                    .Where(r =>
                        r.HastaId == id &&
                        r.AktifMi
                    );

            /*
                Doktor giriş yaptıysa sadece bu doktora
                ait randevular bırakılır.
            */
            if (doktorId.HasValue)
            {
                int aktifDoktorId =
                    doktorId.Value;

                randevuSorgusu =
                    randevuSorgusu.Where(r =>
                        r.DoktorId == aktifDoktorId
                    );
            }

            ViewBag.Randevular =
                randevuSorgusu
                    .OrderByDescending(r =>
                        r.RandevuTarihiSaati
                    )
                    .ToList();

            /*
                Hastanın muayene sorgusu hazırlanır.
            */
            IQueryable<Muayene> muayeneSorgusu =
                _context.Muayeneler
                    .Include(m => m.Doktor)
                    .Where(m =>
                        m.HastaId == id &&
                        m.AktifMi
                    );

            /*
                Doktor yalnızca kendi yaptığı
                muayeneleri görebilir.
            */
            if (doktorId.HasValue)
            {
                int aktifDoktorId =
                    doktorId.Value;

                muayeneSorgusu =
                    muayeneSorgusu.Where(m =>
                        m.DoktorId == aktifDoktorId
                    );
            }

            ViewBag.Muayeneler =
                muayeneSorgusu
                    .OrderByDescending(m =>
                        m.MuayeneTarihi
                    )
                    .ToList();

            /*
                Reçeteler muayene üzerinden doktora
                bağlanır.
            */
            IQueryable<Recete> receteSorgusu =
                _context.Receteler
                    .Include(r => r.Muayene)
                        .ThenInclude(m => m!.Doktor)
                    .Where(r =>
                        r.AktifMi &&
                        r.Muayene != null &&
                        r.Muayene.AktifMi &&
                        r.Muayene.HastaId == id
                    );

            /*
                Doktor yalnızca kendi muayenesinde
                oluşturulan reçeteleri görür.
            */
            if (doktorId.HasValue)
            {
                int aktifDoktorId =
                    doktorId.Value;

                receteSorgusu =
                    receteSorgusu.Where(r =>
                        r.Muayene != null &&
                        r.Muayene.DoktorId ==
                        aktifDoktorId
                    );
            }

            ViewBag.Receteler =
                receteSorgusu
                    .OrderByDescending(r =>
                        r.KayitTarihi
                    )
                    .ToList();

            /*
                Tahliller de muayene üzerinden
                doktora bağlanır.
            */
            IQueryable<Tahlil> tahlilSorgusu =
                _context.Tahliller
                    .Include(t => t.Muayene)
                        .ThenInclude(m => m!.Doktor)
                    .Where(t =>
                        t.AktifMi &&
                        t.Muayene != null &&
                        t.Muayene.AktifMi &&
                        t.Muayene.HastaId == id
                    );

            /*
                Doktor yalnızca kendi muayenesinde
                istenen tahlilleri görür.
            */
            if (doktorId.HasValue)
            {
                int aktifDoktorId =
                    doktorId.Value;

                tahlilSorgusu =
                    tahlilSorgusu.Where(t =>
                        t.Muayene != null &&
                        t.Muayene.DoktorId ==
                        aktifDoktorId
                    );
            }

            ViewBag.Tahliller =
                tahlilSorgusu
                    .OrderByDescending(t =>
                        t.IstenmeTarihi
                    )
                    .ToList();

            ViewBag.DoktorMu =
                DoktorMu();

            return View(hasta);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Hasta hasta)
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            if (!HastaIslemiYetkisiVarMi())
            {
                TempData["Hata"] =
                    "Bu işlem için yetkiniz yok.";

                return RedirectToAction("Index");
            }

            if (!ValidationHelper.TcKimlikNoGecerliMi(
                    hasta.TcKimlikNo))
            {
                ModelState.AddModelError(
                    "TcKimlikNo",
                    "TC kimlik no 11 haneli olmalı ve sadece rakamlardan oluşmalıdır."
                );
            }

            if (hasta.DogumTarihi == default)
            {
                ModelState.AddModelError(
                    "DogumTarihi",
                    "Doğum tarihi seçiniz."
                );
            }
            else if (
                !ValidationHelper.DogumTarihiGecerliMi(
                    hasta.DogumTarihi
                )
            )
            {
                ModelState.AddModelError(
                    "DogumTarihi",
                    "Doğum tarihi bugünden ileri bir tarih olamaz."
                );
            }

            if (!ValidationHelper.TelefonGecerliMi(
                    hasta.Telefon))
            {
                ModelState.AddModelError(
                    "Telefon",
                    "Telefon numarası sadece rakamlardan oluşmalı ve 10 veya 11 haneli olmalıdır."
                );
            }

            bool tcVarMi =
                _context.Hastalar.Any(h =>
                    h.TcKimlikNo ==
                    hasta.TcKimlikNo &&
                    h.AktifMi
                );

            if (tcVarMi)
            {
                ModelState.AddModelError(
                    "TcKimlikNo",
                    "Bu TC kimlik numarası ile kayıtlı aktif hasta zaten var."
                );
            }

            if (!ModelState.IsValid)
            {
                return View(hasta);
            }

            hasta.DogumTarihi =
                hasta.DogumTarihi.Date;

            hasta.AktifMi = true;
            hasta.KayitTarihi = DateTime.Now;

            _context.Hastalar.Add(hasta);
            _context.SaveChanges();

            TempData["Basari"] =
                "Hasta kaydı başarıyla eklendi.";

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            if (!HastaIslemiYetkisiVarMi())
            {
                TempData["Hata"] =
                    "Bu işlem için yetkiniz yok.";

                return RedirectToAction("Index");
            }

            /*
                Doktor başka doktora ait hastayı
                güncelleme sayfasından da açamaz.
            */
            if (!HastaKaydinaErisebilirMi(id))
            {
                TempData["Hata"] =
                    "Bu hasta kaydını güncelleme yetkiniz yok.";

                return RedirectToAction("Index");
            }

            Hasta? hasta =
                _context.Hastalar
                    .FirstOrDefault(h =>
                        h.HastaId == id &&
                        h.AktifMi
                    );

            if (hasta == null)
            {
                TempData["Hata"] =
                    "Hasta kaydı bulunamadı.";

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
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            if (!HastaIslemiYetkisiVarMi())
            {
                TempData["Hata"] =
                    "Bu işlem için yetkiniz yok.";

                return RedirectToAction("Index");
            }

            /*
                Doktor form üzerinde HastaId değerini
                değiştirerek başka hastayı güncelleyemez.
            */
            if (!HastaKaydinaErisebilirMi(
                    hasta.HastaId))
            {
                TempData["Hata"] =
                    "Bu hasta kaydını güncelleme yetkiniz yok.";

                return RedirectToAction("Index");
            }

            if (!ValidationHelper.TcKimlikNoGecerliMi(
                    hasta.TcKimlikNo))
            {
                ModelState.AddModelError(
                    "TcKimlikNo",
                    "TC kimlik no 11 haneli olmalı ve sadece rakamlardan oluşmalıdır."
                );
            }

            if (hasta.DogumTarihi == default)
            {
                ModelState.AddModelError(
                    "DogumTarihi",
                    "Doğum tarihi seçiniz."
                );
            }
            else if (
                !ValidationHelper.DogumTarihiGecerliMi(
                    hasta.DogumTarihi
                )
            )
            {
                ModelState.AddModelError(
                    "DogumTarihi",
                    "Doğum tarihi bugünden ileri bir tarih olamaz."
                );
            }

            if (!ValidationHelper.TelefonGecerliMi(
                    hasta.Telefon))
            {
                ModelState.AddModelError(
                    "Telefon",
                    "Telefon numarası sadece rakamlardan oluşmalı ve 10 veya 11 haneli olmalıdır."
                );
            }

            bool tcBaskaHastadaVarMi =
                _context.Hastalar.Any(h =>
                    h.TcKimlikNo ==
                    hasta.TcKimlikNo &&
                    h.HastaId !=
                    hasta.HastaId &&
                    h.AktifMi
                );

            if (tcBaskaHastadaVarMi)
            {
                ModelState.AddModelError(
                    "TcKimlikNo",
                    "Bu TC kimlik numarası başka bir hastada kullanılıyor."
                );
            }

            if (!ModelState.IsValid)
            {
                return View(hasta);
            }

            Hasta? guncellenecekHasta =
                _context.Hastalar
                    .FirstOrDefault(h =>
                        h.HastaId ==
                        hasta.HastaId &&
                        h.AktifMi
                    );

            if (guncellenecekHasta == null)
            {
                TempData["Hata"] =
                    "Hasta kaydı bulunamadı.";

                return RedirectToAction("Index");
            }

            guncellenecekHasta.TcKimlikNo =
                hasta.TcKimlikNo;

            guncellenecekHasta.Ad =
                hasta.Ad;

            guncellenecekHasta.Soyad =
                hasta.Soyad;

            guncellenecekHasta.DogumTarihi =
                hasta.DogumTarihi.Date;

            guncellenecekHasta.Cinsiyet =
                hasta.Cinsiyet;

            guncellenecekHasta.Telefon =
                hasta.Telefon;

            guncellenecekHasta.Eposta =
                hasta.Eposta;

            guncellenecekHasta.Adres =
                hasta.Adres;

            guncellenecekHasta.KanGrubu =
                hasta.KanGrubu;

            _context.SaveChanges();

            TempData["Basari"] =
                "Hasta kaydı başarıyla güncellendi.";

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(int id)
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            string? rolAdi =
                HttpContext.Session.GetString("RolAdi");

            if (rolAdi != "Admin" &&
                rolAdi != "Sekreter")
            {
                TempData["Hata"] =
                    "Hasta silme işlemi için yetkiniz yok.";

                return RedirectToAction("Index");
            }

            Hasta? hasta =
                _context.Hastalar
                    .FirstOrDefault(h =>
                        h.HastaId == id &&
                        h.AktifMi
                    );

            if (hasta == null)
            {
                TempData["Hata"] =
                    "Hasta kaydı bulunamadı.";

                return RedirectToAction("Index");
            }

            hasta.AktifMi = false;

            _context.SaveChanges();

            TempData["Basari"] =
                "Hasta kaydı başarıyla silindi.";

            return RedirectToAction("Index");
        }
    }
}