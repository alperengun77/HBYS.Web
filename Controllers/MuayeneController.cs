using HBYS.Web.Data;
using HBYS.Web.Helpers;
using HBYS.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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
            string? kullaniciAdi =
                HttpContext.Session.GetString("KullaniciAdi");

            return !string.IsNullOrEmpty(kullaniciAdi);
        }

        private bool MuayeneIslemiYetkisiVarMi()
        {
            string? rolAdi =
                HttpContext.Session.GetString("RolAdi");

            return rolAdi == "Admin" ||
                   rolAdi == "Doktor";
        }

        private bool DoktorMu()
        {
            return HttpContext.Session.GetString("RolAdi")
                   == "Doktor";
        }

        private int? AktifDoktorId()
        {
            return HttpContext.Session.GetInt32("DoktorId");
        }

        private IQueryable<Randevu> UygunRandevular()
        {
            DateTime bugun = DateTime.Today;
            DateTime yarin = bugun.AddDays(1);

            IQueryable<Randevu> sorgu =
                _context.Randevular
                    .Include(r => r.Hasta)
                    .Include(r => r.Doktor)
                    .Include(r => r.Poliklinik)
                    .Where(r =>
                        r.AktifMi &&
                        r.Durum != "Iptal" &&
                        r.Durum != "Tamamlandi" &&
                        r.RandevuTarihiSaati >= bugun &&
                        r.RandevuTarihiSaati < yarin &&
                        !_context.Muayeneler.Any(m =>
                            m.AktifMi &&
                            m.RandevuId == r.RandevuId
                        )
                    );

            if (DoktorMu())
            {
                int? doktorId = AktifDoktorId();

                if (doktorId.HasValue)
                {
                    sorgu = sorgu.Where(r =>
                        r.DoktorId == doktorId.Value
                    );
                }
                else
                {
                    sorgu = sorgu.Where(r => false);
                }
            }

            return sorgu;
        }

        [HttpGet]
        public IActionResult RandevulariGetir(
            string tcKimlikNo
        )
        {
            if (!GirisYapildiMi() ||
                !MuayeneIslemiYetkisiVarMi())
            {
                return Json(new
                {
                    basarili = false,
                    mesaj = "Bu işlem için yetkiniz yok."
                });
            }

            tcKimlikNo =
                tcKimlikNo?.Trim() ?? string.Empty;

            if (!ValidationHelper.TcKimlikNoGecerliMi(
                    tcKimlikNo))
            {
                return Json(new
                {
                    basarili = false,
                    mesaj =
                        "TC kimlik numarası 11 rakamdan oluşmalıdır."
                });
            }

            Hasta? hasta =
                _context.Hastalar.FirstOrDefault(h =>
                    h.AktifMi &&
                    h.TcKimlikNo == tcKimlikNo
                );

            if (hasta == null)
            {
                return Json(new
                {
                    basarili = false,
                    mesaj =
                        "Bu TC kimlik numarasına ait aktif hasta bulunamadı."
                });
            }

            List<Randevu> randevular =
                UygunRandevular()
                    .Where(r =>
                        r.HastaId == hasta.HastaId
                    )
                    .OrderBy(r =>
                        r.RandevuTarihiSaati
                    )
                    .ToList();

            if (randevular.Count == 0)
            {
                return Json(new
                {
                    basarili = false,

                    hastaBulundu = true,

                    hasta = new
                    {
                        hastaId = hasta.HastaId,

                        adSoyad =
                            hasta.Ad + " " +
                            hasta.Soyad,

                        tcKimlikNo =
                            hasta.TcKimlikNo
                    },

                    mesaj =
                        "Hastanın bugün için muayeneye uygun bekleyen randevusu bulunmuyor."
                });
            }

            var randevuListesi =
                randevular.Select(r => new
                {
                    randevuId =
                        r.RandevuId,

                    hastaId =
                        r.HastaId,

                    doktorId =
                        r.DoktorId,

                    doktorAdi =
                        (r.Doktor?.Unvan ?? "") +
                        " " +
                        (r.Doktor?.Ad ?? "") +
                        " " +
                        (r.Doktor?.Soyad ?? ""),

                    poliklinikAdi =
                        r.Poliklinik?.PoliklinikAdi
                        ?? "",

                    tarihSaat =
                        r.RandevuTarihiSaati
                            .ToString(
                                "dd.MM.yyyy HH:mm"
                            ),

                    secenekMetni =
                        r.RandevuTarihiSaati
                            .ToString("HH:mm") +
                        " - " +
                        (r.Doktor?.Unvan ?? "") +
                        " " +
                        (r.Doktor?.Ad ?? "") +
                        " " +
                        (r.Doktor?.Soyad ?? "") +
                        " - " +
                        (r.Poliklinik
                            ?.PoliklinikAdi ?? "")
                })
                .ToList();

            return Json(new
            {
                basarili = true,

                hasta = new
                {
                    hastaId =
                        hasta.HastaId,

                    adSoyad =
                        hasta.Ad + " " +
                        hasta.Soyad,

                    tcKimlikNo =
                        hasta.TcKimlikNo
                },

                randevular =
                    randevuListesi
            });
        }

        public IActionResult Index(
            string? arama,
            DateTime? baslangicTarihi,
            DateTime? bitisTarihi
        )
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            var muayeneler =
                _context.Muayeneler
                    .Include(m => m.Hasta)
                    .Include(m => m.Doktor)
                    .Include(m => m.Randevu)
                    .Where(m => m.AktifMi)
                    .AsQueryable();

            if (DoktorMu())
            {
                int? doktorId =
                    AktifDoktorId();

                if (doktorId.HasValue)
                {
                    muayeneler =
                        muayeneler.Where(m =>
                            m.DoktorId ==
                            doktorId.Value
                        );
                }
                else
                {
                    muayeneler =
                        muayeneler.Where(m => false);

                    TempData["Hata"] =
                        "Doktor kullanıcısı bir doktor kaydıyla eşleştirilmemiş.";
                }
            }

            if (!string.IsNullOrWhiteSpace(arama))
            {
                muayeneler =
                    muayeneler.Where(m =>
                        m.Hasta!.TcKimlikNo
                            .Contains(arama) ||

                        m.Hasta.Ad
                            .Contains(arama) ||

                        m.Hasta.Soyad
                            .Contains(arama) ||

                        m.Doktor!.Ad
                            .Contains(arama) ||

                        m.Doktor.Soyad
                            .Contains(arama) ||

                        (
                            m.Sikayet != null &&
                            m.Sikayet.Contains(arama)
                        ) ||

                        (
                            m.Tani != null &&
                            m.Tani.Contains(arama)
                        )
                    );
            }

            if (baslangicTarihi.HasValue)
            {
                DateTime baslangic =
                    baslangicTarihi.Value.Date;

                muayeneler =
                    muayeneler.Where(m =>
                        m.MuayeneTarihi >=
                        baslangic
                    );
            }

            if (bitisTarihi.HasValue)
            {
                DateTime bitis =
                    bitisTarihi.Value
                        .Date
                        .AddDays(1);

                muayeneler =
                    muayeneler.Where(m =>
                        m.MuayeneTarihi < bitis
                    );
            }

            ViewBag.Arama =
                arama;

            ViewBag.BaslangicTarihi =
                baslangicTarihi?
                    .ToString("yyyy-MM-dd");

            ViewBag.BitisTarihi =
                bitisTarihi?
                    .ToString("yyyy-MM-dd");

            return View(
                muayeneler
                    .OrderByDescending(m =>
                        m.MuayeneTarihi
                    )
                    .ToList()
            );
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

            if (!MuayeneIslemiYetkisiVarMi())
            {
                TempData["Hata"] =
                    "Bu işlem için yetkiniz yok.";

                return RedirectToAction("Index");
            }

            Muayene yeniMuayene =
                new Muayene
                {
                    MuayeneTarihi =
                        DateTime.Now
                };

            return View(yeniMuayene);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(
            Muayene muayene
        )
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            if (!MuayeneIslemiYetkisiVarMi())
            {
                TempData["Hata"] =
                    "Bu işlem için yetkiniz yok.";

                return RedirectToAction("Index");
            }

            Randevu? seciliRandevu = null;

            if (!muayene.RandevuId.HasValue ||
                muayene.RandevuId.Value <= 0)
            {
                ModelState.AddModelError(
                    "RandevuId",
                    "Muayene oluşturmak için hastanın geçerli bir randevusu seçilmelidir."
                );
            }
            else
            {
                seciliRandevu =
                    UygunRandevular()
                        .FirstOrDefault(r =>
                            r.RandevuId ==
                            muayene.RandevuId.Value
                        );

                if (seciliRandevu == null)
                {
                    ModelState.AddModelError(
                        "RandevuId",
                        "Seçilen randevu geçersiz, iptal edilmiş, tamamlanmış veya bugüne ait değildir."
                    );
                }
                else
                {
                    /*
                        Hasta ve doktor bilgileri
                        ekrandaki gizli alanlardan değil,
                        doğrudan randevudan alınır.
                    */

                    muayene.HastaId =
                        seciliRandevu.HastaId;

                    muayene.DoktorId =
                        seciliRandevu.DoktorId;
                }
            }

            if (muayene.MuayeneTarihi == default)
            {
                muayene.MuayeneTarihi =
                    DateTime.Now;
            }

            if (muayene.MuayeneTarihi >
                DateTime.Now)
            {
                ModelState.AddModelError(
                    "MuayeneTarihi",
                    "Muayene tarihi ileri bir tarih olamaz."
                );
            }

            if (!ModelState.IsValid)
            {
                return View(muayene);
            }

            muayene.MuayeneTarihi =
                ValidationHelper
                    .SaniyeVeSaliseyiTemizle(
                        muayene.MuayeneTarihi
                    );

            muayene.AktifMi = true;

            _context.Muayeneler.Add(muayene);

            seciliRandevu!.Durum =
                "Tamamlandi";

            _context.SaveChanges();

            TempData["Basari"] =
                "Muayene kaydı başarıyla oluşturuldu.";

            return RedirectToAction("Index");
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

            if (!MuayeneIslemiYetkisiVarMi())
            {
                TempData["Hata"] =
                    "Bu sayfayı görüntüleme yetkiniz yok.";

                return RedirectToAction(
                    "Index",
                    "Dashboard"
                );
            }

            Muayene? muayene =
                _context.Muayeneler
                    .Include(m => m.Hasta)
                    .Include(m => m.Doktor)
                    .Include(m => m.Randevu)
                    .FirstOrDefault(m =>
                        m.MuayeneId == id &&
                        m.AktifMi
                    );

            if (muayene == null)
            {
                TempData["Hata"] =
                    "Muayene kaydı bulunamadı.";

                return RedirectToAction("Index");
            }

            if (DoktorMu() &&
                AktifDoktorId() !=
                muayene.DoktorId)
            {
                TempData["Hata"] =
                    "Başka doktora ait muayeneyi görüntüleyemezsiniz.";

                return RedirectToAction("Index");
            }

            ViewBag.Receteler =
                _context.Receteler
                    .Where(r =>
                        r.MuayeneId == id &&
                        r.AktifMi
                    )
                    .OrderByDescending(r =>
                        r.KayitTarihi
                    )
                    .ToList();

            ViewBag.Tahliller =
                _context.Tahliller
                    .Where(t =>
                        t.MuayeneId == id &&
                        t.AktifMi
                    )
                    .OrderByDescending(t =>
                        t.IstenmeTarihi
                    )
                    .ToList();

            return View(muayene);
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

            if (!MuayeneIslemiYetkisiVarMi())
            {
                TempData["Hata"] =
                    "Bu işlem için yetkiniz yok.";

                return RedirectToAction("Index");
            }

            Muayene? muayene =
                _context.Muayeneler
                    .Include(m => m.Hasta)
                    .Include(m => m.Doktor)
                    .Include(m => m.Randevu)
                    .FirstOrDefault(m =>
                        m.MuayeneId == id &&
                        m.AktifMi
                    );

            if (muayene == null)
            {
                TempData["Hata"] =
                    "Muayene kaydı bulunamadı.";

                return RedirectToAction("Index");
            }

            if (DoktorMu() &&
                AktifDoktorId() !=
                muayene.DoktorId)
            {
                TempData["Hata"] =
                    "Başka doktora ait muayeneyi düzenleyemezsiniz.";

                return RedirectToAction("Index");
            }

            return View(muayene);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(
            Muayene muayene
        )
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            if (!MuayeneIslemiYetkisiVarMi())
            {
                TempData["Hata"] =
                    "Bu işlem için yetkiniz yok.";

                return RedirectToAction("Index");
            }

            Muayene? guncellenecekMuayene =
                _context.Muayeneler
                    .Include(m => m.Hasta)
                    .Include(m => m.Doktor)
                    .Include(m => m.Randevu)
                    .FirstOrDefault(m =>
                        m.MuayeneId ==
                        muayene.MuayeneId &&

                        m.AktifMi
                    );

            if (guncellenecekMuayene == null)
            {
                TempData["Hata"] =
                    "Muayene kaydı bulunamadı.";

                return RedirectToAction("Index");
            }

            if (DoktorMu() &&
                AktifDoktorId() !=
                guncellenecekMuayene.DoktorId)
            {
                TempData["Hata"] =
                    "Başka doktora ait muayeneyi düzenleyemezsiniz.";

                return RedirectToAction("Index");
            }

            if (!guncellenecekMuayene
                    .RandevuId.HasValue)
            {
                ModelState.AddModelError(
                    "RandevuId",
                    "Randevusuz muayene kaydı güncellenemez. Önce geçerli bir randevu oluşturulmalıdır."
                );
            }

            if (muayene.MuayeneTarihi == default)
            {
                ModelState.AddModelError(
                    "MuayeneTarihi",
                    "Muayene tarihi boş bırakılamaz."
                );
            }
            else if (
                muayene.MuayeneTarihi >
                DateTime.Now
            )
            {
                ModelState.AddModelError(
                    "MuayeneTarihi",
                    "Muayene tarihi ileri bir tarih olamaz."
                );
            }

            if (!ModelState.IsValid)
            {
                muayene.Hasta =
                    guncellenecekMuayene.Hasta;

                muayene.Doktor =
                    guncellenecekMuayene.Doktor;

                muayene.Randevu =
                    guncellenecekMuayene.Randevu;

                muayene.HastaId =
                    guncellenecekMuayene.HastaId;

                muayene.DoktorId =
                    guncellenecekMuayene.DoktorId;

                muayene.RandevuId =
                    guncellenecekMuayene.RandevuId;

                return View(muayene);
            }

            /*
                Hasta, doktor ve randevu
                güncelleme sırasında değiştirilmez.
            */

            guncellenecekMuayene.Sikayet =
                muayene.Sikayet;

            guncellenecekMuayene.Tani =
                muayene.Tani;

            guncellenecekMuayene.TedaviNotu =
                muayene.TedaviNotu;

            guncellenecekMuayene.MuayeneTarihi =
                ValidationHelper
                    .SaniyeVeSaliseyiTemizle(
                        muayene.MuayeneTarihi
                    );

            _context.SaveChanges();

            TempData["Basari"] =
                "Muayene kaydı başarıyla güncellendi.";

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
                rolAdi != "Doktor")
            {
                TempData["Hata"] =
                    "Muayene silme işlemi için yetkiniz yok.";

                return RedirectToAction("Index");
            }

            Muayene? muayene =
                _context.Muayeneler
                    .Include(m => m.Randevu)
                    .FirstOrDefault(m =>
                        m.MuayeneId == id &&
                        m.AktifMi
                    );

            if (muayene == null)
            {
                TempData["Hata"] =
                    "Muayene kaydı bulunamadı.";

                return RedirectToAction("Index");
            }

            if (DoktorMu() &&
                AktifDoktorId() !=
                muayene.DoktorId)
            {
                TempData["Hata"] =
                    "Başka doktora ait muayeneyi silemezsiniz.";

                return RedirectToAction("Index");
            }

            muayene.AktifMi = false;

            /*
                Muayene silinirse randevu tekrar
                bekleyen duruma alınır.
            */

            if (muayene.Randevu != null)
            {
                muayene.Randevu.Durum =
                    "Bekliyor";
            }

            _context.SaveChanges();

            TempData["Basari"] =
                "Muayene kaydı başarıyla silindi.";

            return RedirectToAction("Index");
        }
    }
}
