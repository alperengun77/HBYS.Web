using HBYS.Web.Data;
using HBYS.Web.Helpers;
using HBYS.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HBYS.Web.Controllers
{
    public class AcilServisController : Controller
    {
        private readonly AppDbContext _context;

        private static readonly string[] GelisSekilleri =
        {
            "Ayaktan",
            "Ambulans",
            "Sevk",
            "Polis Eşliğinde",
            "Yakını Tarafından"
        };

        private static readonly string[] TriyajSeviyeleri =
        {
            "Kırmızı",
            "Sarı",
            "Yeşil",
            "Siyah"
        };

        private static readonly string[] BasvuruDurumlari =
        {
            "Bekliyor",
            "Doktor Atandı",
            "Muayenede",
            "Gözlemde",
            "Yatış Verildi",
            "Taburcu",
            "Sevk Edildi"
        };

        private static readonly string[] BilincDurumlari =
        {
            "Açık",
            "Konfüze",
            "Uykuya Eğilimli",
            "Yanıtsız"
        };

        public AcilServisController(
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

        private bool AcilServisYetkisiVarMi()
        {
            string? rolAdi =
                HttpContext.Session.GetString(
                    "RolAdi"
                );

            return rolAdi == "Admin" ||
                   rolAdi == "Sekreter" ||
                   rolAdi == "Doktor";
        }

        private bool DoktorMu()
        {
            return HttpContext.Session.GetString(
                "RolAdi"
            ) == "Doktor";
        }

        private bool AdminMi()
        {
            return HttpContext.Session.GetString(
                "RolAdi"
            ) == "Admin";
        }

        private int? AktifDoktorId()
        {
            return HttpContext.Session.GetInt32(
                "DoktorId"
            );
        }

        private IQueryable<Doktor>
            AcilDoktorlariSorgusu()
        {
            return _context.Doktorlar
                .Include(d => d.Poliklinik)
                .Where(d =>
                    d.AktifMi &&
                    d.Poliklinik != null &&
                    d.Poliklinik.AktifMi &&
                    EF.Functions.Like(
                        d.Poliklinik.PoliklinikAdi,
                        "%Acil%"
                    )
                );
        }

        private bool AcilDoktoruMu(
            int doktorId
        )
        {
            return AcilDoktorlariSorgusu()
                .Any(d =>
                    d.DoktorId == doktorId
                );
        }

        private bool BasvuruyaErisebilirMi(
            AcilBasvuru basvuru
        )
        {
            if (!DoktorMu())
            {
                return true;
            }

            int? doktorId =
                AktifDoktorId();

            return doktorId.HasValue &&
                   basvuru.DoktorId ==
                   doktorId.Value;
        }

        private void SecimListeleriniHazirla(
            AcilBasvuru model
        )
        {
            ViewBag.Doktorlar =
                new SelectList(
                    AcilDoktorlariSorgusu()
                        .OrderBy(d => d.Ad)
                        .ThenBy(d => d.Soyad)
                        .Select(d => new
                        {
                            d.DoktorId,

                            AdSoyad =
                                (d.Unvan ?? "") +
                                " " +
                                d.Ad +
                                " " +
                                d.Soyad
                        })
                        .ToList(),

                    "DoktorId",
                    "AdSoyad",
                    model.DoktorId
                );

            ViewBag.GelisSekilleri =
                new SelectList(
                    GelisSekilleri,
                    model.GelisSekli
                );

            ViewBag.TriyajSeviyeleri =
                new SelectList(
                    TriyajSeviyeleri,
                    model.TriyajSeviyesi
                );

            ViewBag.BasvuruDurumlari =
                new SelectList(
                    BasvuruDurumlari,
                    model.Durum
                );

            ViewBag.BilincDurumlari =
                new SelectList(
                    BilincDurumlari,
                    model.BilincDurumu
                );

            ViewBag.DoktorMu =
                DoktorMu();
        }

        private void FiltreListeleriniHazirla(
            string? triyajSeviyesi,
            string? durum
        )
        {
            ViewBag.TriyajFiltreListesi =
                new SelectList(
                    TriyajSeviyeleri,
                    triyajSeviyesi
                );

            ViewBag.DurumFiltreListesi =
                new SelectList(
                    BasvuruDurumlari,
                    durum
                );
        }

        private void HastaBilgisiniHazirla(
            int hastaId
        )
        {
            Hasta? hasta =
                _context.Hastalar
                    .AsNoTracking()
                    .FirstOrDefault(h =>
                        h.HastaId == hastaId &&
                        h.AktifMi
                    );

            if (hasta == null)
            {
                ViewBag.HastaBilgisi =
                    string.Empty;

                ViewBag.HastaTc =
                    string.Empty;

                return;
            }

            ViewBag.HastaBilgisi =
                hasta.Ad +
                " " +
                hasta.Soyad;

            ViewBag.HastaTc =
                hasta.TcKimlikNo;
        }

        private void BasvuruyuDogrula(
            AcilBasvuru model
        )
        {
            bool hastaVarMi =
                _context.Hastalar.Any(h =>
                    h.HastaId == model.HastaId &&
                    h.AktifMi
                );

            if (!hastaVarMi)
            {
                ModelState.AddModelError(
                    "HastaId",
                    "Geçerli bir hasta seçilmelidir."
                );
            }

            if (model.BasvuruTarihi == default)
            {
                ModelState.AddModelError(
                    "BasvuruTarihi",
                    "Başvuru tarihi seçilmelidir."
                );
            }
            else if (
                model.BasvuruTarihi >
                DateTime.Now.AddMinutes(5)
            )
            {
                ModelState.AddModelError(
                    "BasvuruTarihi",
                    "Başvuru tarihi ileri bir tarih olamaz."
                );
            }

            if (
                !GelisSekilleri.Contains(
                    model.GelisSekli
                )
            )
            {
                ModelState.AddModelError(
                    "GelisSekli",
                    "Geçerli bir geliş şekli seçiniz."
                );
            }

            if (
                !TriyajSeviyeleri.Contains(
                    model.TriyajSeviyesi
                )
            )
            {
                ModelState.AddModelError(
                    "TriyajSeviyesi",
                    "Geçerli bir triyaj seviyesi seçiniz."
                );
            }

            if (
                !BasvuruDurumlari.Contains(
                    model.Durum
                )
            )
            {
                ModelState.AddModelError(
                    "Durum",
                    "Geçerli bir başvuru durumu seçiniz."
                );
            }

            if (
                model.DoktorId.HasValue &&
                !AcilDoktoruMu(
                    model.DoktorId.Value
                )
            )
            {
                ModelState.AddModelError(
                    "DoktorId",
                    "Yalnızca Acil Servis doktoru atanabilir."
                );
            }

            if (
                model.Durum == "Doktor Atandı" &&
                !model.DoktorId.HasValue
            )
            {
                ModelState.AddModelError(
                    "DoktorId",
                    "Doktor atandı durumunda bir doktor seçilmelidir."
                );
            }

            if (
                model.Durum == "Muayenede" &&
                !model.DoktorId.HasValue
            )
            {
                ModelState.AddModelError(
                    "DoktorId",
                    "Muayene işlemi için doktor atanmalıdır."
                );
            }

            if (
                model.TansiyonSistolik.HasValue &&
                model.TansiyonDiastolik.HasValue &&
                model.TansiyonSistolik.Value <=
                model.TansiyonDiastolik.Value
            )
            {
                ModelState.AddModelError(
                    "TansiyonSistolik",
                    "Sistolik tansiyon diyastolik tansiyondan büyük olmalıdır."
                );
            }

            bool sonucGerekliMi =
                model.Durum == "Yatış Verildi" ||
                model.Durum == "Taburcu" ||
                model.Durum == "Sevk Edildi";

            if (
                sonucGerekliMi &&
                string.IsNullOrWhiteSpace(
                    model.Sonuc
                )
            )
            {
                ModelState.AddModelError(
                    "Sonuc",
                    "Bu durum için sonuç açıklaması yazılmalıdır."
                );
            }
        }

        private void ModeliTemizle(
            AcilBasvuru model
        )
        {
            model.GelisSekli =
                model.GelisSekli.Trim();

            model.Sikayet =
                model.Sikayet.Trim();

            model.TriyajSeviyesi =
                model.TriyajSeviyesi.Trim();

            model.Durum =
                model.Durum.Trim();

            model.BilincDurumu =
                string.IsNullOrWhiteSpace(
                    model.BilincDurumu
                )
                    ? null
                    : model.BilincDurumu.Trim();

            model.MudahaleNotu =
                string.IsNullOrWhiteSpace(
                    model.MudahaleNotu
                )
                    ? null
                    : model.MudahaleNotu.Trim();

            model.Sonuc =
                string.IsNullOrWhiteSpace(
                    model.Sonuc
                )
                    ? null
                    : model.Sonuc.Trim();

            model.BasvuruTarihi =
                new DateTime(
                    model.BasvuruTarihi.Year,
                    model.BasvuruTarihi.Month,
                    model.BasvuruTarihi.Day,
                    model.BasvuruTarihi.Hour,
                    model.BasvuruTarihi.Minute,
                    0
                );
        }

        public IActionResult Index(
            string? arama,
            DateTime? tarih,
            string? triyajSeviyesi,
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

            if (!AcilServisYetkisiVarMi())
            {
                TempData["Hata"] =
                    "Acil Servis modülüne erişim yetkiniz yok.";

                return RedirectToAction(
                    "Index",
                    "Dashboard"
                );
            }

            IQueryable<AcilBasvuru> sorgu =
                _context.AcilBasvurular
                    .Include(a => a.Hasta)
                    .Include(a => a.Doktor)
                    .Where(a => a.AktifMi);

            if (DoktorMu())
            {
                int? doktorId =
                    AktifDoktorId();

                if (doktorId.HasValue)
                {
                    sorgu =
                        sorgu.Where(a =>
                            a.DoktorId ==
                            doktorId.Value
                        );
                }
                else
                {
                    sorgu =
                        sorgu.Where(a =>
                            false
                        );

                    TempData["Hata"] =
                        "Doktor kullanıcısı bir doktor kaydıyla eşleştirilmemiş.";
                }
            }

            if (!string.IsNullOrWhiteSpace(arama))
            {
                sorgu =
                    sorgu.Where(a =>
                        a.Hasta!.TcKimlikNo
                            .Contains(arama) ||

                        a.Hasta.Ad
                            .Contains(arama) ||

                        a.Hasta.Soyad
                            .Contains(arama) ||

                        a.Sikayet
                            .Contains(arama) ||

                        (
                            a.Doktor != null &&
                            (
                                a.Doktor.Ad
                                    .Contains(arama) ||

                                a.Doktor.Soyad
                                    .Contains(arama)
                            )
                        )
                    );
            }

            if (tarih.HasValue)
            {
                DateTime gunBaslangic =
                    tarih.Value.Date;

                DateTime gunBitis =
                    gunBaslangic.AddDays(1);

                sorgu =
                    sorgu.Where(a =>
                        a.BasvuruTarihi >=
                        gunBaslangic &&

                        a.BasvuruTarihi <
                        gunBitis
                    );
            }

            if (
                !string.IsNullOrWhiteSpace(
                    triyajSeviyesi
                )
            )
            {
                sorgu =
                    sorgu.Where(a =>
                        a.TriyajSeviyesi ==
                        triyajSeviyesi
                    );
            }

            if (
                !string.IsNullOrWhiteSpace(
                    durum
                )
            )
            {
                sorgu =
                    sorgu.Where(a =>
                        a.Durum == durum
                    );
            }

            ViewBag.Arama =
                arama;

            ViewBag.Tarih =
                tarih?
                    .ToString("yyyy-MM-dd");

            ViewBag.SeciliTriyaj =
                triyajSeviyesi;

            ViewBag.SeciliDurum =
                durum;

            ViewBag.AdminMi =
                AdminMi();

            FiltreListeleriniHazirla(
                triyajSeviyesi,
                durum
            );

            List<AcilBasvuru> liste =
                sorgu
                    .OrderBy(a =>
                        a.Durum == "Taburcu" ||
                        a.Durum == "Sevk Edildi"
                            ? 1
                            : 0
                    )
                    .ThenBy(a =>
                        a.TriyajSeviyesi ==
                        "Kırmızı"
                            ? 1
                            : a.TriyajSeviyesi ==
                              "Sarı"
                                ? 2
                                : a.TriyajSeviyesi ==
                                  "Yeşil"
                                    ? 3
                                    : 4
                    )
                    .ThenBy(a =>
                        a.BasvuruTarihi
                    )
                    .ToList();

            return View(liste);
        }

        [HttpGet]
        public IActionResult HastaGetir(
            string tcKimlikNo
        )
        {
            if (
                !GirisYapildiMi() ||
                !AcilServisYetkisiVarMi()
            )
            {
                return Json(new
                {
                    basarili = false,
                    mesaj =
                        "Bu işlem için yetkiniz yok."
                });
            }

            tcKimlikNo =
                tcKimlikNo?.Trim() ??
                string.Empty;

            if (
                !ValidationHelper
                    .TcKimlikNoGecerliMi(
                        tcKimlikNo
                    )
            )
            {
                return Json(new
                {
                    basarili = false,
                    mesaj =
                        "Geçerli bir TC kimlik numarası giriniz."
                });
            }

            Hasta? hasta =
                _context.Hastalar
                    .AsNoTracking()
                    .FirstOrDefault(h =>
                        h.AktifMi &&
                        h.TcKimlikNo ==
                        tcKimlikNo
                    );

            if (hasta == null)
            {
                return Json(new
                {
                    basarili = false,
                    hastaBulundu = false,
                    mesaj =
                        "Bu TC kimlik numarasına ait aktif hasta bulunamadı."
                });
            }

            return Json(new
            {
                basarili = true,

                hasta = new
                {
                    hastaId =
                        hasta.HastaId,

                    tcKimlikNo =
                        hasta.TcKimlikNo,

                    adSoyad =
                        hasta.Ad +
                        " " +
                        hasta.Soyad,

                    dogumTarihi =
                        hasta.DogumTarihi
                            .ToString(
                                "dd.MM.yyyy"
                            ),

                    kanGrubu =
                        hasta.KanGrubu ??
                        "Belirtilmemiş",

                    telefon =
                        hasta.Telefon ??
                        "Belirtilmemiş"
                }
            });
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

            if (!AcilServisYetkisiVarMi())
            {
                TempData["Hata"] =
                    "Acil başvurusu oluşturma yetkiniz yok.";

                return RedirectToAction(
                    "Index",
                    "Dashboard"
                );
            }

            AcilBasvuru model =
                new AcilBasvuru
                {
                    BasvuruTarihi =
                        DateTime.Now,

                    GelisSekli =
                        "Ayaktan",

                    TriyajSeviyesi =
                        "Yeşil",

                    Durum =
                        "Bekliyor",

                    BilincDurumu =
                        "Açık"
                };

            if (DoktorMu())
            {
                int? doktorId =
                    AktifDoktorId();

                if (
                    !doktorId.HasValue ||
                    !AcilDoktoruMu(
                        doktorId.Value
                    )
                )
                {
                    TempData["Hata"] =
                        "Yalnızca Acil Servis doktoru acil başvurusu oluşturabilir.";

                    return RedirectToAction(
                        "Index",
                        "Dashboard"
                    );
                }

                model.DoktorId =
                    doktorId.Value;

                model.Durum =
                    "Doktor Atandı";
            }

            SecimListeleriniHazirla(
                model
            );

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(
            AcilBasvuru model
        )
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            if (!AcilServisYetkisiVarMi())
            {
                TempData["Hata"] =
                    "Acil başvurusu oluşturma yetkiniz yok.";

                return RedirectToAction(
                    "Index",
                    "Dashboard"
                );
            }

            if (DoktorMu())
            {
                int? doktorId =
                    AktifDoktorId();

                if (
                    !doktorId.HasValue ||
                    !AcilDoktoruMu(
                        doktorId.Value
                    )
                )
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Yalnızca Acil Servis doktoru acil başvurusu oluşturabilir."
                    );
                }
                else
                {
                    model.DoktorId =
                        doktorId.Value;
                }
            }

            BasvuruyuDogrula(
                model
            );

            if (!ModelState.IsValid)
            {
                SecimListeleriniHazirla(
                    model
                );

                HastaBilgisiniHazirla(
                    model.HastaId
                );

                return View(model);
            }

            ModeliTemizle(
                model
            );

            if (
                model.DoktorId.HasValue &&
                model.Durum == "Bekliyor"
            )
            {
                model.Durum =
                    "Doktor Atandı";
            }

            model.AktifMi =
                true;

            model.KayitTarihi =
                DateTime.Now;

            _context.AcilBasvurular.Add(
                model
            );

            _context.SaveChanges();

            TempData["Basari"] =
                "Acil Servis başvurusu başarıyla oluşturuldu.";

            return RedirectToAction(
                "Index"
            );
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

            if (!AcilServisYetkisiVarMi())
            {
                TempData["Hata"] =
                    "Bu sayfayı görüntüleme yetkiniz yok.";

                return RedirectToAction(
                    "Index",
                    "Dashboard"
                );
            }

            AcilBasvuru? basvuru =
                _context.AcilBasvurular
                    .Include(a => a.Hasta)
                    .Include(a => a.Doktor)
                        .ThenInclude(d =>
                            d!.Poliklinik
                        )
                    .FirstOrDefault(a =>
                        a.AcilBasvuruId == id &&
                        a.AktifMi
                    );

            if (basvuru == null)
            {
                TempData["Hata"] =
                    "Acil Servis başvurusu bulunamadı.";

                return RedirectToAction(
                    "Index"
                );
            }

            if (
                !BasvuruyaErisebilirMi(
                    basvuru
                )
            )
            {
                TempData["Hata"] =
                    "Başka doktora ait Acil Servis kaydını görüntüleyemezsiniz.";

                return RedirectToAction(
                    "Index"
                );
            }

            return View(basvuru);
        }

        [HttpGet]
        public IActionResult Edit(
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

            if (!AcilServisYetkisiVarMi())
            {
                TempData["Hata"] =
                    "Bu işlemi yapma yetkiniz yok.";

                return RedirectToAction(
                    "Index",
                    "Dashboard"
                );
            }

            AcilBasvuru? basvuru =
                _context.AcilBasvurular
                    .Include(a => a.Hasta)
                    .Include(a => a.Doktor)
                    .FirstOrDefault(a =>
                        a.AcilBasvuruId == id &&
                        a.AktifMi
                    );

            if (basvuru == null)
            {
                TempData["Hata"] =
                    "Acil Servis başvurusu bulunamadı.";

                return RedirectToAction(
                    "Index"
                );
            }

            if (
                !BasvuruyaErisebilirMi(
                    basvuru
                )
            )
            {
                TempData["Hata"] =
                    "Başka doktora ait Acil Servis kaydını düzenleyemezsiniz.";

                return RedirectToAction(
                    "Index"
                );
            }

            SecimListeleriniHazirla(
                basvuru
            );

            HastaBilgisiniHazirla(
                basvuru.HastaId
            );

            return View(basvuru);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(
            AcilBasvuru model
        )
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            if (!AcilServisYetkisiVarMi())
            {
                TempData["Hata"] =
                    "Bu işlemi yapma yetkiniz yok.";

                return RedirectToAction(
                    "Index",
                    "Dashboard"
                );
            }

            AcilBasvuru? guncellenecek =
                _context.AcilBasvurular
                    .FirstOrDefault(a =>
                        a.AcilBasvuruId ==
                        model.AcilBasvuruId &&

                        a.AktifMi
                    );

            if (guncellenecek == null)
            {
                TempData["Hata"] =
                    "Acil Servis başvurusu bulunamadı.";

                return RedirectToAction(
                    "Index"
                );
            }

            if (
                !BasvuruyaErisebilirMi(
                    guncellenecek
                )
            )
            {
                TempData["Hata"] =
                    "Başka doktora ait Acil Servis kaydını düzenleyemezsiniz.";

                return RedirectToAction(
                    "Index"
                );
            }

            model.HastaId =
                guncellenecek.HastaId;

            if (DoktorMu())
            {
                model.DoktorId =
                    AktifDoktorId();
            }

            BasvuruyuDogrula(
                model
            );

            if (!ModelState.IsValid)
            {
                SecimListeleriniHazirla(
                    model
                );

                HastaBilgisiniHazirla(
                    model.HastaId
                );

                return View(model);
            }

            ModeliTemizle(
                model
            );

            guncellenecek.DoktorId =
                model.DoktorId;

            guncellenecek.BasvuruTarihi =
                model.BasvuruTarihi;

            guncellenecek.GelisSekli =
                model.GelisSekli;

            guncellenecek.Sikayet =
                model.Sikayet;

            guncellenecek.Ates =
                model.Ates;

            guncellenecek.Nabiz =
                model.Nabiz;

            guncellenecek.TansiyonSistolik =
                model.TansiyonSistolik;

            guncellenecek.TansiyonDiastolik =
                model.TansiyonDiastolik;

            guncellenecek.OksijenSaturasyonu =
                model.OksijenSaturasyonu;

            guncellenecek.SolunumSayisi =
                model.SolunumSayisi;

            guncellenecek.BilincDurumu =
                model.BilincDurumu;

            guncellenecek.TriyajSeviyesi =
                model.TriyajSeviyesi;

            guncellenecek.Durum =
                model.Durum;

            guncellenecek.MudahaleNotu =
                model.MudahaleNotu;

            guncellenecek.Sonuc =
                model.Sonuc;

            _context.SaveChanges();

            TempData["Basari"] =
                "Acil Servis başvurusu başarıyla güncellendi.";

            return RedirectToAction(
                "Details",
                new
                {
                    id =
                        guncellenecek
                            .AcilBasvuruId
                }
            );
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(
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

            if (!AdminMi())
            {
                TempData["Hata"] =
                    "Acil Servis kaydını yalnızca Admin silebilir.";

                return RedirectToAction(
                    "Index"
                );
            }

            AcilBasvuru? basvuru =
                _context.AcilBasvurular
                    .FirstOrDefault(a =>
                        a.AcilBasvuruId == id &&
                        a.AktifMi
                    );

            if (basvuru == null)
            {
                TempData["Hata"] =
                    "Acil Servis başvurusu bulunamadı.";

                return RedirectToAction(
                    "Index"
                );
            }

            basvuru.AktifMi =
                false;

            _context.SaveChanges();

            TempData["Basari"] =
                "Acil Servis başvurusu silindi.";

            return RedirectToAction(
                "Index"
            );
        }
    }
}