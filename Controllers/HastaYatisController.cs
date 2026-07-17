using HBYS.Web.Data;
using HBYS.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HBYS.Web.Controllers
{
    public class HastaYatisController : Controller
    {
        private readonly AppDbContext _context;

        private static readonly string[] YatisTurleri =
        {
            "Poliklinik",
            "Acil Servis",
            "Sevk",
            "Doğrudan Yatış"
        };

        public HastaYatisController(
            AppDbContext context
        )
        {
            _context = context;
        }

        private bool GirisYapildiMi()
        {
            string? kullaniciAdi =
                HttpContext.Session.GetString(
                    "KullaniciAdi"
                );

            return !string.IsNullOrWhiteSpace(
                kullaniciAdi
            );
        }

        private bool GoruntulemeYetkisiVarMi()
        {
            string? rolAdi =
                HttpContext.Session.GetString(
                    "RolAdi"
                );

            return rolAdi == "Admin" ||
                   rolAdi == "Yonetici" ||
                   rolAdi == "Sekreter" ||
                   rolAdi == "Doktor";
        }

        private bool YatisYetkisiVarMi()
        {
            string? rolAdi =
                HttpContext.Session.GetString(
                    "RolAdi"
                );

            return rolAdi == "Admin" ||
                   rolAdi == "Sekreter" ||
                   rolAdi == "Doktor";
        }

        private bool TaburcuYetkisiVarMi()
        {
            string? rolAdi =
                HttpContext.Session.GetString(
                    "RolAdi"
                );

            return rolAdi == "Admin" ||
                   rolAdi == "Doktor";
        }

        private bool DoktorMu()
        {
            return HttpContext.Session.GetString(
                "RolAdi"
            ) == "Doktor";
        }

        private int? AktifDoktorId()
        {
            return HttpContext.Session.GetInt32(
                "DoktorId"
            );
        }

        private bool KaydaErisebilirMi(
            HastaYatis yatis
        )
        {
            if (!DoktorMu())
            {
                return true;
            }

            int? doktorId = AktifDoktorId();

            return doktorId.HasValue &&
                   yatis.DoktorId == doktorId.Value;
        }

        private static string? BosMetniNullYap(
            string? deger
        )
        {
            return string.IsNullOrWhiteSpace(deger)
                ? null
                : deger.Trim();
        }

        private int HastaYasiHesapla(
            DateTime dogumTarihi
        )
        {
            int yas =
                DateTime.Today.Year -
                dogumTarihi.Year;

            if (
                dogumTarihi.Date >
                DateTime.Today.AddYears(-yas)
            )
            {
                yas--;
            }

            return yas;
        }

        private bool OdaHastayaUygunMu(
            Hasta hasta,
            Oda oda,
            out string hataMesaji
        )
        {
            hataMesaji = string.Empty;

            string kisitlama =
                oda.CinsiyetKisitlamasi;

            if (kisitlama == "Karma")
            {
                return true;
            }

            if (kisitlama == "Çocuk")
            {
                int yas =
                    HastaYasiHesapla(
                        hasta.DogumTarihi
                    );

                if (yas < 18)
                {
                    return true;
                }

                hataMesaji =
                    "Seçilen oda yalnızca çocuk hastalar içindir.";

                return false;
            }

            if (
                string.IsNullOrWhiteSpace(
                    hasta.Cinsiyet
                )
            )
            {
                hataMesaji =
                    "Hastanın cinsiyet bilgisi eksik olduğu için bu oda seçilemez.";

                return false;
            }

            bool uygunMu =
                string.Equals(
                    hasta.Cinsiyet.Trim(),
                    kisitlama,
                    StringComparison.OrdinalIgnoreCase
                );

            if (!uygunMu)
            {
                hataMesaji =
                    $"Seçilen oda yalnızca {kisitlama} hastalar içindir.";
            }

            return uygunMu;
        }

        private void SecimListeleriniHazirla(
            HastaYatis model
        )
        {
            ViewBag.Hastalar =
                new SelectList(
                    _context.Hastalar
                        .Where(h => h.AktifMi)
                        .OrderBy(h => h.Ad)
                        .ThenBy(h => h.Soyad)
                        .Select(h => new
                        {
                            h.HastaId,

                            HastaBilgisi =
                                h.TcKimlikNo +
                                " - " +
                                h.Ad +
                                " " +
                                h.Soyad
                        })
                        .ToList(),

                    "HastaId",
                    "HastaBilgisi",
                    model.HastaId
                );

            IQueryable<Doktor> doktorSorgusu =
                _context.Doktorlar
                    .Include(d => d.Poliklinik)
                    .Where(d => d.AktifMi);

            if (DoktorMu())
            {
                int? doktorId =
                    AktifDoktorId();

                if (doktorId.HasValue)
                {
                    doktorSorgusu =
                        doktorSorgusu.Where(d =>
                            d.DoktorId ==
                            doktorId.Value
                        );
                }
                else
                {
                    doktorSorgusu =
                        doktorSorgusu.Where(d =>
                            false
                        );
                }
            }

            ViewBag.Doktorlar =
                new SelectList(
                    doktorSorgusu
                        .OrderBy(d => d.Ad)
                        .ThenBy(d => d.Soyad)
                        .Select(d => new
                        {
                            d.DoktorId,

                            DoktorBilgisi =
                                (d.Unvan ?? "") +
                                " " +
                                d.Ad +
                                " " +
                                d.Soyad +
                                " - " +
                                (
                                    d.Poliklinik != null
                                        ? d.Poliklinik.PoliklinikAdi
                                        : "Poliklinik Yok"
                                )
                        })
                        .ToList(),

                    "DoktorId",
                    "DoktorBilgisi",
                    model.DoktorId
                );

            ViewBag.Yataklar =
                new SelectList(
                    _context.Yataklar
                        .Include(y => y.Oda)
                            .ThenInclude(o => o!.Servis)
                        .Where(y =>
                            y.AktifMi &&
                            y.Durum == "Boş" &&
                            y.Oda != null &&
                            y.Oda.AktifMi &&
                            y.Oda.Servis != null &&
                            y.Oda.Servis.AktifMi
                        )
                        .OrderBy(y =>
                            y.Oda!.Servis!.ServisAdi
                        )
                        .ThenBy(y =>
                            y.Oda!.OdaNo
                        )
                        .ThenBy(y =>
                            y.YatakNo
                        )
                        .Select(y => new
                        {
                            y.YatakId,

                            YatakBilgisi =
                                y.Oda!.Servis!.ServisAdi +
                                " | Oda " +
                                y.Oda.OdaNo +
                                " | Yatak " +
                                y.YatakNo +
                                " | " +
                                y.Oda.CinsiyetKisitlamasi
                        })
                        .ToList(),

                    "YatakId",
                    "YatakBilgisi",
                    model.YatakId
                );

            ViewBag.YatisTurleri =
                new SelectList(
                    YatisTurleri,
                    model.YatisTuru
                );

            ViewBag.DoktorMu =
                DoktorMu();
        }

        private void FiltreListeleriniHazirla(
            int? servisId,
            string? durum
        )
        {
            ViewBag.Servisler =
                new SelectList(
                    _context.Servisler
                        .Where(s => s.AktifMi)
                        .OrderBy(s => s.ServisAdi)
                        .ToList(),

                    "ServisId",
                    "ServisAdi",
                    servisId
                );

            ViewBag.Durumlar =
                new SelectList(
                    new List<string>
                    {
                        "Yatıyor",
                        "Taburcu"
                    },
                    durum
                );
        }

        public IActionResult Index(
            string? arama,
            int? servisId,
            string? durum
        )
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            if (!GoruntulemeYetkisiVarMi())
            {
                TempData["Hata"] =
                    "Hasta yatış ekranına erişim yetkiniz yok.";

                return RedirectToAction(
                    "Index",
                    "Dashboard"
                );
            }

            IQueryable<HastaYatis> sorgu =
                _context.HastaYatislari
                    .Include(y => y.Hasta)
                    .Include(y => y.Doktor)
                    .Include(y => y.Yatak)
                        .ThenInclude(y => y!.Oda)
                            .ThenInclude(o => o!.Servis)
                    .Where(y => y.AktifMi);

            if (DoktorMu())
            {
                int? doktorId =
                    AktifDoktorId();

                if (doktorId.HasValue)
                {
                    sorgu =
                        sorgu.Where(y =>
                            y.DoktorId ==
                            doktorId.Value
                        );
                }
                else
                {
                    sorgu =
                        sorgu.Where(y => false);

                    TempData["Hata"] =
                        "Doktor kullanıcısı bir doktor kaydıyla eşleştirilmemiş.";
                }
            }

            if (!string.IsNullOrWhiteSpace(arama))
            {
                string aramaMetni =
                    arama.Trim();

                sorgu =
                    sorgu.Where(y =>
                        y.Hasta!.TcKimlikNo
                            .Contains(aramaMetni) ||

                        y.Hasta.Ad
                            .Contains(aramaMetni) ||

                        y.Hasta.Soyad
                            .Contains(aramaMetni) ||

                        y.Doktor!.Ad
                            .Contains(aramaMetni) ||

                        y.Doktor.Soyad
                            .Contains(aramaMetni) ||

                        y.YatisNedeni
                            .Contains(aramaMetni)
                    );
            }

            if (
                servisId.HasValue &&
                servisId.Value > 0
            )
            {
                sorgu =
                    sorgu.Where(y =>
                        y.Yatak!.Oda!.ServisId ==
                        servisId.Value
                    );
            }

            if (!string.IsNullOrWhiteSpace(durum))
            {
                sorgu =
                    sorgu.Where(y =>
                        y.Durum == durum
                    );
            }

            DateTime bugun =
                DateTime.Today;

            DateTime yarin =
                bugun.AddDays(1);

            ViewBag.AktifYatanHasta =
                _context.HastaYatislari.Count(y =>
                    y.AktifMi &&
                    y.Durum == "Yatıyor"
                );

            ViewBag.BugunYatanHasta =
                _context.HastaYatislari.Count(y =>
                    y.AktifMi &&
                    y.YatisTarihi >= bugun &&
                    y.YatisTarihi < yarin
                );

            ViewBag.BugunTaburcu =
                _context.HastaYatislari.Count(y =>
                    y.AktifMi &&
                    y.Durum == "Taburcu" &&
                    y.TaburcuTarihi.HasValue &&
                    y.TaburcuTarihi.Value >= bugun &&
                    y.TaburcuTarihi.Value < yarin
                );

            ViewBag.BosYatak =
                _context.Yataklar.Count(y =>
                    y.AktifMi &&
                    y.Durum == "Boş"
                );

            ViewBag.Arama = arama;

            ViewBag.SeciliServisId =
                servisId;

            ViewBag.SeciliDurum =
                durum;

            ViewBag.YatisYetkisiVarMi =
                YatisYetkisiVarMi();

            ViewBag.TaburcuYetkisiVarMi =
                TaburcuYetkisiVarMi();

            FiltreListeleriniHazirla(
                servisId,
                durum
            );

            List<HastaYatis> liste =
                sorgu
                    .OrderBy(y =>
                        y.Durum == "Yatıyor"
                            ? 0
                            : 1
                    )
                    .ThenByDescending(y =>
                        y.YatisTarihi
                    )
                    .ToList();

            return View(liste);
        }

        [HttpGet]
        public IActionResult Create(
            int? yatakId,
            int? hastaId
        )
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            if (!YatisYetkisiVarMi())
            {
                TempData["Hata"] =
                    "Hasta yatırma yetkiniz yok.";

                return RedirectToAction("Index");
            }

            HastaYatis model =
                new HastaYatis
                {
                    YatisTarihi =
                        DateTime.Now,

                    YatisTuru =
                        "Poliklinik",

                    Durum =
                        "Yatıyor",

                    YatakId =
                        yatakId ?? 0,

                    HastaId =
                        hastaId ?? 0
                };

            if (DoktorMu())
            {
                int? doktorId =
                    AktifDoktorId();

                if (!doktorId.HasValue)
                {
                    TempData["Hata"] =
                        "Doktor hesabı bir doktor kaydıyla eşleştirilmemiş.";

                    return RedirectToAction("Index");
                }

                model.DoktorId =
                    doktorId.Value;
            }

            SecimListeleriniHazirla(model);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(
            HastaYatis model
        )
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            if (!YatisYetkisiVarMi())
            {
                TempData["Hata"] =
                    "Hasta yatırma yetkiniz yok.";

                return RedirectToAction("Index");
            }

            if (DoktorMu())
            {
                int? doktorId =
                    AktifDoktorId();

                if (doktorId.HasValue)
                {
                    model.DoktorId =
                        doktorId.Value;
                }
                else
                {
                    ModelState.AddModelError(
                        "DoktorId",
                        "Doktor hesabı bir doktor kaydıyla eşleştirilmemiş."
                    );
                }
            }

            model.YatisNedeni =
                model.YatisNedeni?.Trim() ??
                string.Empty;

            model.OnTani =
                BosMetniNullYap(
                    model.OnTani
                );

            Hasta? hasta =
                _context.Hastalar
                    .FirstOrDefault(h =>
                        h.HastaId ==
                        model.HastaId &&

                        h.AktifMi
                    );

            if (hasta == null)
            {
                ModelState.AddModelError(
                    "HastaId",
                    "Geçerli bir hasta seçiniz."
                );
            }

            bool hastaZatenYatiyorMu =
                _context.HastaYatislari.Any(y =>
                    y.AktifMi &&
                    y.HastaId ==
                    model.HastaId &&
                    y.Durum == "Yatıyor"
                );

            if (hastaZatenYatiyorMu)
            {
                ModelState.AddModelError(
                    "HastaId",
                    "Seçilen hastanın devam eden bir yatış kaydı bulunmaktadır."
                );
            }

            bool doktorVarMi =
                _context.Doktorlar.Any(d =>
                    d.DoktorId ==
                    model.DoktorId &&

                    d.AktifMi
                );

            if (!doktorVarMi)
            {
                ModelState.AddModelError(
                    "DoktorId",
                    "Geçerli bir doktor seçiniz."
                );
            }

            Yatak? yatak =
                _context.Yataklar
                    .Include(y => y.Oda)
                        .ThenInclude(o => o!.Servis)
                    .FirstOrDefault(y =>
                        y.YatakId ==
                        model.YatakId &&

                        y.AktifMi
                    );

            if (yatak == null)
            {
                ModelState.AddModelError(
                    "YatakId",
                    "Geçerli bir yatak seçiniz."
                );
            }
            else if (yatak.Durum != "Boş")
            {
                ModelState.AddModelError(
                    "YatakId",
                    "Seçilen yatak artık boş değildir."
                );
            }
            else if (
                hasta != null &&
                yatak.Oda != null
            )
            {
                bool odaUygunMu =
                    OdaHastayaUygunMu(
                        hasta,
                        yatak.Oda,
                        out string hataMesaji
                    );

                if (!odaUygunMu)
                {
                    ModelState.AddModelError(
                        "YatakId",
                        hataMesaji
                    );
                }
            }

            if (
                !YatisTurleri.Contains(
                    model.YatisTuru
                )
            )
            {
                ModelState.AddModelError(
                    "YatisTuru",
                    "Geçerli bir yatış türü seçiniz."
                );
            }

            if (model.YatisTarihi == default)
            {
                ModelState.AddModelError(
                    "YatisTarihi",
                    "Yatış tarihi seçiniz."
                );
            }
            else if (
                model.YatisTarihi >
                DateTime.Now.AddMinutes(5)
            )
            {
                ModelState.AddModelError(
                    "YatisTarihi",
                    "Yatış tarihi ileri bir tarih olamaz."
                );
            }

            if (!ModelState.IsValid)
            {
                SecimListeleriniHazirla(model);

                return View(model);
            }

            using var transaction =
                _context.Database.BeginTransaction();

            try
            {
                Yatak? guncelYatak =
                    _context.Yataklar
                        .FirstOrDefault(y =>
                            y.YatakId ==
                            model.YatakId &&

                            y.AktifMi
                        );

                if (
                    guncelYatak == null ||
                    guncelYatak.Durum != "Boş"
                )
                {
                    transaction.Rollback();

                    ModelState.AddModelError(
                        "YatakId",
                        "Seçilen yatak başka bir hasta tarafından kullanıma alındı."
                    );

                    SecimListeleriniHazirla(model);

                    return View(model);
                }

                bool devamEdenYatisVarMi =
                    _context.HastaYatislari.Any(y =>
                        y.AktifMi &&
                        y.HastaId ==
                        model.HastaId &&
                        y.Durum == "Yatıyor"
                    );

                if (devamEdenYatisVarMi)
                {
                    transaction.Rollback();

                    ModelState.AddModelError(
                        "HastaId",
                        "Hastanın devam eden başka bir yatış kaydı bulunmaktadır."
                    );

                    SecimListeleriniHazirla(model);

                    return View(model);
                }

                model.YatisTarihi =
                    new DateTime(
                        model.YatisTarihi.Year,
                        model.YatisTarihi.Month,
                        model.YatisTarihi.Day,
                        model.YatisTarihi.Hour,
                        model.YatisTarihi.Minute,
                        0
                    );

                model.Durum =
                    "Yatıyor";

                model.TaburcuTarihi =
                    null;

                model.TaburcuNedeni =
                    null;

                model.TaburcuNotu =
                    null;

                model.AktifMi =
                    true;

                model.KayitTarihi =
                    DateTime.Now;

                guncelYatak.Durum =
                    "Dolu";

                _context.HastaYatislari.Add(model);

                _context.SaveChanges();

                transaction.Commit();

                TempData["Basari"] =
                    "Hasta başarıyla yatırıldı ve yatak dolu durumuna getirildi.";

                return RedirectToAction(
                    "Details",
                    new
                    {
                        id =
                            model.HastaYatisId
                    }
                );
            }
            catch (DbUpdateException)
            {
                transaction.Rollback();

                ModelState.AddModelError(
                    string.Empty,
                    "Yatış kaydı oluşturulamadı. Hasta veya yatak başka bir işlemde kullanılmış olabilir."
                );

                SecimListeleriniHazirla(model);

                return View(model);
            }
        }

        public IActionResult Details(
            int id
        )
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            if (!GoruntulemeYetkisiVarMi())
            {
                TempData["Hata"] =
                    "Bu sayfayı görüntüleme yetkiniz yok.";

                return RedirectToAction(
                    "Index",
                    "Dashboard"
                );
            }

            HastaYatis? yatis =
                _context.HastaYatislari
                    .Include(y => y.Hasta)
                    .Include(y => y.Doktor)
                        .ThenInclude(d =>
                            d!.Poliklinik
                        )
                    .Include(y => y.Yatak)
                        .ThenInclude(y =>
                            y!.Oda
                        )
                            .ThenInclude(o =>
                                o!.Servis
                            )
                    .FirstOrDefault(y =>
                        y.HastaYatisId == id &&
                        y.AktifMi
                    );

            if (yatis == null)
            {
                TempData["Hata"] =
                    "Hasta yatış kaydı bulunamadı.";

                return RedirectToAction("Index");
            }

            if (!KaydaErisebilirMi(yatis))
            {
                TempData["Hata"] =
                    "Başka doktora ait yatış kaydını görüntüleyemezsiniz.";

                return RedirectToAction("Index");
            }

            ViewBag.TaburcuYetkisiVarMi =
                TaburcuYetkisiVarMi();

            return View(yatis);
        }

        [HttpGet]
        public IActionResult Taburcu(
            int id
        )
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            if (!TaburcuYetkisiVarMi())
            {
                TempData["Hata"] =
                    "Hasta taburcu etme yetkiniz yok.";

                return RedirectToAction("Index");
            }

            HastaYatis? yatis =
                _context.HastaYatislari
                    .Include(y => y.Hasta)
                    .Include(y => y.Doktor)
                    .Include(y => y.Yatak)
                        .ThenInclude(y =>
                            y!.Oda
                        )
                            .ThenInclude(o =>
                                o!.Servis
                            )
                    .FirstOrDefault(y =>
                        y.HastaYatisId == id &&
                        y.AktifMi &&
                        y.Durum == "Yatıyor"
                    );

            if (yatis == null)
            {
                TempData["Hata"] =
                    "Devam eden yatış kaydı bulunamadı.";

                return RedirectToAction("Index");
            }

            if (!KaydaErisebilirMi(yatis))
            {
                TempData["Hata"] =
                    "Başka doktora ait hastayı taburcu edemezsiniz.";

                return RedirectToAction("Index");
            }

            ViewBag.TaburcuTarihi =
                DateTime.Now.ToString(
                    "yyyy-MM-ddTHH:mm"
                );

            ViewBag.TaburcuNedeni =
                string.Empty;

            ViewBag.TaburcuNotu =
                string.Empty;

            return View(yatis);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Taburcu(
            int hastaYatisId,
            DateTime taburcuTarihi,
            string taburcuNedeni,
            string? taburcuNotu
        )
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            if (!TaburcuYetkisiVarMi())
            {
                TempData["Hata"] =
                    "Hasta taburcu etme yetkiniz yok.";

                return RedirectToAction("Index");
            }

            HastaYatis? yatis =
                _context.HastaYatislari
                    .Include(y => y.Hasta)
                    .Include(y => y.Doktor)
                    .Include(y => y.Yatak)
                        .ThenInclude(y =>
                            y!.Oda
                        )
                            .ThenInclude(o =>
                                o!.Servis
                            )
                    .FirstOrDefault(y =>
                        y.HastaYatisId ==
                        hastaYatisId &&

                        y.AktifMi &&
                        y.Durum == "Yatıyor"
                    );

            if (yatis == null)
            {
                TempData["Hata"] =
                    "Devam eden yatış kaydı bulunamadı.";

                return RedirectToAction("Index");
            }

            if (!KaydaErisebilirMi(yatis))
            {
                TempData["Hata"] =
                    "Başka doktora ait hastayı taburcu edemezsiniz.";

                return RedirectToAction("Index");
            }

            taburcuNedeni =
                taburcuNedeni?.Trim() ??
                string.Empty;

            taburcuNotu =
                BosMetniNullYap(
                    taburcuNotu
                );

            if (taburcuTarihi == default)
            {
                ModelState.AddModelError(
                    "taburcuTarihi",
                    "Taburcu tarihi seçilmelidir."
                );
            }
            else if (
                taburcuTarihi >
                DateTime.Now.AddMinutes(5)
            )
            {
                ModelState.AddModelError(
                    "taburcuTarihi",
                    "Taburcu tarihi ileri bir tarih olamaz."
                );
            }
            else if (
                taburcuTarihi <
                yatis.YatisTarihi
            )
            {
                ModelState.AddModelError(
                    "taburcuTarihi",
                    "Taburcu tarihi yatış tarihinden önce olamaz."
                );
            }

            if (string.IsNullOrWhiteSpace(taburcuNedeni))
            {
                ModelState.AddModelError(
                    "taburcuNedeni",
                    "Taburcu nedeni yazılmalıdır."
                );
            }
            else if (taburcuNedeni.Length > 500)
            {
                ModelState.AddModelError(
                    "taburcuNedeni",
                    "Taburcu nedeni en fazla 500 karakter olabilir."
                );
            }

            if (
                taburcuNotu != null &&
                taburcuNotu.Length > 1000
            )
            {
                ModelState.AddModelError(
                    "taburcuNotu",
                    "Taburcu notu en fazla 1000 karakter olabilir."
                );
            }

            if (!ModelState.IsValid)
            {
                ViewBag.TaburcuTarihi =
                    taburcuTarihi == default
                        ? DateTime.Now.ToString(
                            "yyyy-MM-ddTHH:mm"
                        )
                        : taburcuTarihi.ToString(
                            "yyyy-MM-ddTHH:mm"
                        );

                ViewBag.TaburcuNedeni =
                    taburcuNedeni;

                ViewBag.TaburcuNotu =
                    taburcuNotu;

                return View(yatis);
            }

            using var transaction =
                _context.Database.BeginTransaction();

            try
            {
                yatis.TaburcuTarihi =
                    new DateTime(
                        taburcuTarihi.Year,
                        taburcuTarihi.Month,
                        taburcuTarihi.Day,
                        taburcuTarihi.Hour,
                        taburcuTarihi.Minute,
                        0
                    );

                yatis.TaburcuNedeni =
                    taburcuNedeni;

                yatis.TaburcuNotu =
                    taburcuNotu;

                yatis.Durum =
                    "Taburcu";

                if (yatis.Yatak != null)
                {
                    yatis.Yatak.Durum =
                        "Temizlikte";
                }

                _context.SaveChanges();

                transaction.Commit();

                TempData["Basari"] =
                    "Hasta başarıyla taburcu edildi. Yatak temizlikte durumuna getirildi.";

                return RedirectToAction(
                    "Details",
                    new
                    {
                        id =
                            yatis.HastaYatisId
                    }
                );
            }
            catch
            {
                transaction.Rollback();

                ModelState.AddModelError(
                    string.Empty,
                    "Taburcu işlemi sırasında hata oluştu."
                );

                ViewBag.TaburcuTarihi =
                    taburcuTarihi.ToString(
                        "yyyy-MM-ddTHH:mm"
                    );

                ViewBag.TaburcuNedeni =
                    taburcuNedeni;

                ViewBag.TaburcuNotu =
                    taburcuNotu;

                return View(yatis);
            }
        }
    }
}