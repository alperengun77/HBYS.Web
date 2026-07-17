using HBYS.Web.Data;
using HBYS.Web.Models;
using HBYS.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HBYS.Web.Controllers
{
    public class HemsireGozlemController : Controller
    {
        private readonly AppDbContext _context;

        private static readonly string[] BilincDurumlari =
        {
            "Açık",
            "Konfüze",
            "Uykuya Eğilimli",
            "Yanıtsız"
        };

        private static readonly string[] BeslenmeDurumlari =
        {
            "Normal",
            "İştahsız",
            "Sıvı Diyet",
            "Yumuşak Diyet",
            "Ağızdan Beslenmiyor",
            "Sonda ile Besleniyor"
        };

        public HemsireGozlemController(
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

        private string RolAdi()
        {
            return HttpContext.Session.GetString(
                "RolAdi"
            ) ?? string.Empty;
        }

        private bool AdminMi()
        {
            return RolAdi() == "Admin";
        }

        private bool DoktorMu()
        {
            return RolAdi() == "Doktor";
        }

        private bool HemsireMi()
        {
            return RolAdi() == "Hemsire";
        }

        private int? AktifDoktorId()
        {
            return HttpContext.Session.GetInt32(
                "DoktorId"
            );
        }

        private int? AktifKullaniciId()
        {
            return HttpContext.Session.GetInt32(
                "KullaniciId"
            );
        }

        private bool GoruntulemeYetkisiVarMi()
        {
            string rolAdi = RolAdi();

            return rolAdi == "Admin" ||
                   rolAdi == "Yonetici" ||
                   rolAdi == "Doktor" ||
                   rolAdi == "Hemsire";
        }

        private bool KayitYetkisiVarMi()
        {
            string rolAdi = RolAdi();

            return rolAdi == "Admin" ||
                   rolAdi == "Doktor" ||
                   rolAdi == "Hemsire";
        }

        private IQueryable<HastaYatis>
            ErisilebilirYatislar(
                bool sadeceYatanHastalar
            )
        {
            IQueryable<HastaYatis> sorgu =
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
                    .Where(y => y.AktifMi);

            if (sadeceYatanHastalar)
            {
                sorgu =
                    sorgu.Where(y =>
                        y.Durum == "Yatıyor"
                    );
            }

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
                }
            }

            return sorgu;
        }

        private HemsireGozlem?
            GozlemKaydiniGetir(
                int id
            )
        {
            return _context.HemsireGozlemleri
                .Include(g => g.KaydedenKullanici)
                .Include(g => g.HastaYatis)
                    .ThenInclude(y => y!.Hasta)
                .Include(g => g.HastaYatis)
                    .ThenInclude(y => y!.Doktor)
                        .ThenInclude(d =>
                            d!.Poliklinik
                        )
                .Include(g => g.HastaYatis)
                    .ThenInclude(y => y!.Yatak)
                        .ThenInclude(y =>
                            y!.Oda
                        )
                            .ThenInclude(o =>
                                o!.Servis
                            )
                .FirstOrDefault(g =>
                    g.HemsireGozlemId == id &&
                    g.AktifMi
                );
        }

        private bool YatisaErisebilirMi(
            HastaYatis? yatis
        )
        {
            if (yatis == null)
            {
                return false;
            }

            if (!GoruntulemeYetkisiVarMi())
            {
                return false;
            }

            if (!DoktorMu())
            {
                return true;
            }

            int? doktorId =
                AktifDoktorId();

            return doktorId.HasValue &&
                   yatis.DoktorId ==
                   doktorId.Value;
        }

        private bool DuzenlemeYetkisiVarMi(
            HemsireGozlem gozlem
        )
        {
            if (AdminMi())
            {
                return true;
            }

            if (
                DoktorMu() &&
                YatisaErisebilirMi(
                    gozlem.HastaYatis
                )
            )
            {
                return true;
            }

            if (HemsireMi())
            {
                int? kullaniciId =
                    AktifKullaniciId();

                return kullaniciId.HasValue &&
                       gozlem.KaydedenKullaniciId ==
                       kullaniciId.Value;
            }

            return false;
        }

        private static string?
            BosMetniNullYap(
                string? deger
            )
        {
            return string.IsNullOrWhiteSpace(deger)
                ? null
                : deger.Trim();
        }

        private void SecimListeleriniHazirla(
            int? seciliHastaYatisId = null
        )
        {
            List<object> yatislar =
                ErisilebilirYatislar(true)
                    .OrderBy(y =>
                        y.Yatak!.Oda!.Servis!.ServisAdi
                    )
                    .ThenBy(y =>
                        y.Yatak!.Oda!.OdaNo
                    )
                    .ThenBy(y =>
                        y.Yatak!.YatakNo
                    )
                    .Select(y => new
                    {
                        y.HastaYatisId,

                        HastaBilgisi =
                            y.Hasta!.TcKimlikNo +
                            " - " +
                            y.Hasta.Ad +
                            " " +
                            y.Hasta.Soyad +
                            " | " +
                            y.Yatak!.Oda!.Servis!.ServisAdi +
                            " | Oda " +
                            y.Yatak.Oda.OdaNo +
                            " | Yatak " +
                            y.Yatak.YatakNo
                    })
                    .Cast<object>()
                    .ToList();

            ViewBag.HastaYatislari =
                new SelectList(
                    yatislar,
                    "HastaYatisId",
                    "HastaBilgisi",
                    seciliHastaYatisId
                );

            ViewBag.BilincDurumlari =
                new SelectList(
                    BilincDurumlari
                );

            ViewBag.BeslenmeDurumlari =
                new SelectList(
                    BeslenmeDurumlari
                );
        }

        private void FiltreListeleriniHazirla(
            int? servisId
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
        }

        private void GozlemKaydiniDogrula(
            HemsireGozlem model
        )
        {
            HastaYatis? yatis =
                ErisilebilirYatislar(true)
                    .AsNoTracking()
                    .FirstOrDefault(y =>
                        y.HastaYatisId ==
                        model.HastaYatisId
                    );

            if (yatis == null)
            {
                ModelState.AddModelError(
                    "HastaYatisId",
                    "Geçerli ve devam eden bir hasta yatışı seçiniz."
                );
            }
            else
            {
                if (
                    model.GozlemTarihi <
                    yatis.YatisTarihi
                )
                {
                    ModelState.AddModelError(
                        "GozlemTarihi",
                        "Gözlem tarihi hastanın yatış tarihinden önce olamaz."
                    );
                }
            }

            if (model.GozlemTarihi == default)
            {
                ModelState.AddModelError(
                    "GozlemTarihi",
                    "Gözlem tarihi seçiniz."
                );
            }
            else if (
                model.GozlemTarihi >
                DateTime.Now.AddMinutes(5)
            )
            {
                ModelState.AddModelError(
                    "GozlemTarihi",
                    "Gözlem tarihi ileri bir tarih olamaz."
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
                    "Büyük tansiyon küçük tansiyondan yüksek olmalıdır."
                );
            }

            if (
                !string.IsNullOrWhiteSpace(
                    model.BilincDurumu
                ) &&
                !BilincDurumlari.Contains(
                    model.BilincDurumu
                )
            )
            {
                ModelState.AddModelError(
                    "BilincDurumu",
                    "Geçerli bir bilinç durumu seçiniz."
                );
            }

            if (
                !string.IsNullOrWhiteSpace(
                    model.BeslenmeDurumu
                ) &&
                !BeslenmeDurumlari.Contains(
                    model.BeslenmeDurumu
                )
            )
            {
                ModelState.AddModelError(
                    "BeslenmeDurumu",
                    "Geçerli bir beslenme durumu seçiniz."
                );
            }

            bool bilgiGirildiMi =
                model.Ates.HasValue ||
                model.Nabiz.HasValue ||
                model.TansiyonSistolik.HasValue ||
                model.TansiyonDiastolik.HasValue ||
                model.OksijenSaturasyonu.HasValue ||
                model.SolunumSayisi.HasValue ||
                model.KanSekeri.HasValue ||
                model.AgriDuzeyi.HasValue ||
                model.IdrarMiktari.HasValue ||
                !string.IsNullOrWhiteSpace(
                    model.BilincDurumu
                ) ||
                !string.IsNullOrWhiteSpace(
                    model.BeslenmeDurumu
                ) ||
                !string.IsNullOrWhiteSpace(
                    model.VerilenIlaclar
                ) ||
                !string.IsNullOrWhiteSpace(
                    model.SerumTakibi
                ) ||
                !string.IsNullOrWhiteSpace(
                    model.BakimNotu
                );

            if (!bilgiGirildiMi)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "En az bir hayati bulgu veya bakım notu girilmelidir."
                );
            }

            if (
                model.KritikDurumVarMi &&
                string.IsNullOrWhiteSpace(
                    model.KritikDurumNotu
                )
            )
            {
                ModelState.AddModelError(
                    "KritikDurumNotu",
                    "Kritik durum işaretlendiyse açıklama yazılmalıdır."
                );
            }
        }

        private void ModeliTemizle(
            HemsireGozlem model
        )
        {
            model.GozlemTarihi =
                new DateTime(
                    model.GozlemTarihi.Year,
                    model.GozlemTarihi.Month,
                    model.GozlemTarihi.Day,
                    model.GozlemTarihi.Hour,
                    model.GozlemTarihi.Minute,
                    0
                );

            model.BilincDurumu =
                BosMetniNullYap(
                    model.BilincDurumu
                );

            model.BeslenmeDurumu =
                BosMetniNullYap(
                    model.BeslenmeDurumu
                );

            model.VerilenIlaclar =
                BosMetniNullYap(
                    model.VerilenIlaclar
                );

            model.SerumTakibi =
                BosMetniNullYap(
                    model.SerumTakibi
                );

            model.BakimNotu =
                BosMetniNullYap(
                    model.BakimNotu
                );

            model.KritikDurumNotu =
                model.KritikDurumVarMi
                    ? BosMetniNullYap(
                        model.KritikDurumNotu
                    )
                    : null;
        }

        public IActionResult Index(
            string? arama,
            int? servisId,
            DateTime? tarih,
            bool? kritikDurum
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
                    "Hemşire gözlem ekranına erişim yetkiniz yok.";

                return RedirectToAction(
                    "Index",
                    "Dashboard"
                );
            }

            DateTime seciliTarih =
                (tarih ?? DateTime.Today).Date;

            DateTime gunBitis =
                seciliTarih.AddDays(1);

            IQueryable<HastaYatis> yatisSorgusu =
                ErisilebilirYatislar(true);

            if (
                servisId.HasValue &&
                servisId.Value > 0
            )
            {
                yatisSorgusu =
                    yatisSorgusu.Where(y =>
                        y.Yatak!.Oda!.ServisId ==
                        servisId.Value
                    );
            }

            if (!string.IsNullOrWhiteSpace(arama))
            {
                string aramaMetni =
                    arama.Trim();

                yatisSorgusu =
                    yatisSorgusu.Where(y =>
                        y.Hasta!.TcKimlikNo.Contains(
                            aramaMetni
                        ) ||
                        y.Hasta.Ad.Contains(
                            aramaMetni
                        ) ||
                        y.Hasta.Soyad.Contains(
                            aramaMetni
                        ) ||
                        y.Doktor!.Ad.Contains(
                            aramaMetni
                        ) ||
                        y.Doktor.Soyad.Contains(
                            aramaMetni
                        ) ||
                        y.Yatak!.Oda!.Servis!.ServisAdi
                            .Contains(aramaMetni)
                    );
            }

            List<HastaYatis> yatislar =
                yatisSorgusu
                    .OrderBy(y =>
                        y.Yatak!.Oda!.Servis!.ServisAdi
                    )
                    .ThenBy(y =>
                        y.Yatak!.Oda!.OdaNo
                    )
                    .ThenBy(y =>
                        y.Yatak!.YatakNo
                    )
                    .ToList();

            List<int> yatisIdleri =
                yatislar
                    .Select(y => y.HastaYatisId)
                    .ToList();

            List<HemsireGozlem> gozlemler =
                _context.HemsireGozlemleri
                    .Include(g =>
                        g.KaydedenKullanici
                    )
                    .Where(g =>
                        g.AktifMi &&
                        yatisIdleri.Contains(
                            g.HastaYatisId
                        ) &&
                        g.GozlemTarihi >=
                        seciliTarih &&
                        g.GozlemTarihi <
                        gunBitis
                    )
                    .OrderByDescending(g =>
                        g.GozlemTarihi
                    )
                    .ToList();

            List<HemsireGozlemHastaViewModel>
                liste =
                    yatislar
                        .Select(y =>
                        {
                            List<HemsireGozlem>
                                hastaGozlemleri =
                                    gozlemler
                                        .Where(g =>
                                            g.HastaYatisId ==
                                            y.HastaYatisId
                                        )
                                        .OrderByDescending(g =>
                                            g.GozlemTarihi
                                        )
                                        .ToList();

                            return new
                                HemsireGozlemHastaViewModel
                            {
                                HastaYatis = y,

                                SonGozlem =
                                    hastaGozlemleri
                                        .FirstOrDefault(),

                                SeciliGunGozlemSayisi =
                                    hastaGozlemleri.Count
                            };
                        })
                        .ToList();

            if (kritikDurum.HasValue)
            {
                liste =
                    liste.Where(x =>
                        (
                            x.SonGozlem?
                                .KritikDurumVarMi ??
                            false
                        ) ==
                        kritikDurum.Value
                    )
                    .ToList();
            }

            ViewBag.Arama =
                arama;

            ViewBag.SeciliServisId =
                servisId;

            ViewBag.SeciliTarih =
                seciliTarih.ToString(
                    "yyyy-MM-dd"
                );

            ViewBag.SeciliKritikDurum =
                kritikDurum;

            ViewBag.KayitYetkisiVarMi =
                KayitYetkisiVarMi();

            ViewBag.AktifYatanHasta =
                yatislar.Count;

            ViewBag.GozlemSayisi =
                gozlemler.Count;

            ViewBag.KritikGozlemSayisi =
                gozlemler.Count(g =>
                    g.KritikDurumVarMi
                );

            ViewBag.GozlemBekleyenHasta =
                liste.Count(x =>
                    x.SeciliGunGozlemSayisi == 0
                );

            FiltreListeleriniHazirla(
                servisId
            );

            return View(liste);
        }

        public IActionResult Gecmis(
            int hastaYatisId
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
                    "Hemşire gözlem kayıtlarını görüntüleme yetkiniz yok.";

                return RedirectToAction(
                    "Index",
                    "Dashboard"
                );
            }

            HastaYatis? yatis =
                ErisilebilirYatislar(false)
                    .FirstOrDefault(y =>
                        y.HastaYatisId ==
                        hastaYatisId
                    );

            if (yatis == null)
            {
                TempData["Hata"] =
                    "Hasta yatış kaydı bulunamadı.";

                return RedirectToAction(
                    "Index"
                );
            }

            List<HemsireGozlem> gozlemler =
                _context.HemsireGozlemleri
                    .Include(g =>
                        g.KaydedenKullanici
                    )
                    .Where(g =>
                        g.AktifMi &&
                        g.HastaYatisId ==
                        hastaYatisId
                    )
                    .OrderByDescending(g =>
                        g.GozlemTarihi
                    )
                    .ToList();

            ViewBag.HastaYatis =
                yatis;

            ViewBag.KayitYetkisiVarMi =
                KayitYetkisiVarMi() &&
                yatis.Durum == "Yatıyor";

            ViewBag.AdminMi =
                AdminMi();

            ViewBag.DoktorMu =
                DoktorMu();

            ViewBag.HemsireMi =
                HemsireMi();

            ViewBag.AktifKullaniciId =
                AktifKullaniciId();

            return View(gozlemler);
        }

        [HttpGet]
        public IActionResult Create(
            int? hastaYatisId
        )
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            if (!KayitYetkisiVarMi())
            {
                TempData["Hata"] =
                    "Hemşire gözlem kaydı ekleme yetkiniz yok.";

                return RedirectToAction(
                    "Index"
                );
            }

            if (hastaYatisId.HasValue)
            {
                bool yatisVarMi =
                    ErisilebilirYatislar(true)
                        .Any(y =>
                            y.HastaYatisId ==
                            hastaYatisId.Value
                        );

                if (!yatisVarMi)
                {
                    TempData["Hata"] =
                        "Devam eden ve erişilebilir bir hasta yatışı bulunamadı.";

                    return RedirectToAction(
                        "Index"
                    );
                }
            }

            HemsireGozlem model =
                new HemsireGozlem
                {
                    HastaYatisId =
                        hastaYatisId ?? 0,

                    GozlemTarihi =
                        DateTime.Now,

                    BilincDurumu =
                        "Açık",

                    BeslenmeDurumu =
                        "Normal"
                };

            SecimListeleriniHazirla(
                hastaYatisId
            );

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(
            HemsireGozlem model
        )
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            if (!KayitYetkisiVarMi())
            {
                TempData["Hata"] =
                    "Hemşire gözlem kaydı ekleme yetkiniz yok.";

                return RedirectToAction(
                    "Index"
                );
            }

            int? kullaniciId =
                AktifKullaniciId();

            if (!kullaniciId.HasValue)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Oturum kullanıcı bilgisi bulunamadı."
                );
            }
            else
            {
                model.KaydedenKullaniciId =
                    kullaniciId.Value;
            }

            GozlemKaydiniDogrula(
                model
            );

            if (!ModelState.IsValid)
            {
                SecimListeleriniHazirla(
                    model.HastaYatisId
                );

                return View(model);
            }

            ModeliTemizle(
                model
            );

            model.AktifMi =
                true;

            model.KayitTarihi =
                DateTime.Now;

            _context.HemsireGozlemleri.Add(
                model
            );

            _context.SaveChanges();

            TempData["Basari"] =
                "Hemşire gözlem kaydı başarıyla oluşturuldu.";

            return RedirectToAction(
                "Gecmis",
                new
                {
                    hastaYatisId =
                        model.HastaYatisId
                }
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

            if (!GoruntulemeYetkisiVarMi())
            {
                TempData["Hata"] =
                    "Bu kaydı görüntüleme yetkiniz yok.";

                return RedirectToAction(
                    "Index",
                    "Dashboard"
                );
            }

            HemsireGozlem? gozlem =
                GozlemKaydiniGetir(
                    id
                );

            if (
                gozlem == null ||
                !YatisaErisebilirMi(
                    gozlem.HastaYatis
                )
            )
            {
                TempData["Hata"] =
                    "Hemşire gözlem kaydı bulunamadı veya erişim yetkiniz yok.";

                return RedirectToAction(
                    "Index"
                );
            }

            ViewBag.DuzenlemeYetkisiVarMi =
                DuzenlemeYetkisiVarMi(
                    gozlem
                );

            ViewBag.AdminMi =
                AdminMi();

            return View(gozlem);
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

            HemsireGozlem? gozlem =
                GozlemKaydiniGetir(
                    id
                );

            if (
                gozlem == null ||
                !DuzenlemeYetkisiVarMi(
                    gozlem
                )
            )
            {
                TempData["Hata"] =
                    "Bu gözlem kaydını düzenleme yetkiniz yok.";

                return RedirectToAction(
                    "Index"
                );
            }

            if (
                gozlem.HastaYatis == null ||
                gozlem.HastaYatis.Durum !=
                "Yatıyor"
            )
            {
                TempData["Hata"] =
                    "Taburcu edilmiş hastaya ait gözlem kaydı düzenlenemez.";

                return RedirectToAction(
                    "Details",
                    new
                    {
                        id
                    }
                );
            }

            ViewBag.HastaYatis =
                gozlem.HastaYatis;

            ViewBag.BilincDurumlari =
                new SelectList(
                    BilincDurumlari,
                    gozlem.BilincDurumu
                );

            ViewBag.BeslenmeDurumlari =
                new SelectList(
                    BeslenmeDurumlari,
                    gozlem.BeslenmeDurumu
                );

            return View(gozlem);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(
            HemsireGozlem model
        )
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            HemsireGozlem? guncellenecek =
                GozlemKaydiniGetir(
                    model.HemsireGozlemId
                );

            if (
                guncellenecek == null ||
                !DuzenlemeYetkisiVarMi(
                    guncellenecek
                )
            )
            {
                TempData["Hata"] =
                    "Bu gözlem kaydını düzenleme yetkiniz yok.";

                return RedirectToAction(
                    "Index"
                );
            }

            if (
                guncellenecek.HastaYatis == null ||
                guncellenecek.HastaYatis.Durum !=
                "Yatıyor"
            )
            {
                TempData["Hata"] =
                    "Taburcu edilmiş hastaya ait gözlem kaydı düzenlenemez.";

                return RedirectToAction(
                    "Details",
                    new
                    {
                        id =
                            model.HemsireGozlemId
                    }
                );
            }

            model.HastaYatisId =
                guncellenecek.HastaYatisId;

            model.KaydedenKullaniciId =
                guncellenecek.KaydedenKullaniciId;

            GozlemKaydiniDogrula(
                model
            );

            if (!ModelState.IsValid)
            {
                ViewBag.HastaYatis =
                    guncellenecek.HastaYatis;

                ViewBag.BilincDurumlari =
                    new SelectList(
                        BilincDurumlari,
                        model.BilincDurumu
                    );

                ViewBag.BeslenmeDurumlari =
                    new SelectList(
                        BeslenmeDurumlari,
                        model.BeslenmeDurumu
                    );

                return View(model);
            }

            ModeliTemizle(
                model
            );

            guncellenecek.GozlemTarihi =
                model.GozlemTarihi;

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

            guncellenecek.KanSekeri =
                model.KanSekeri;

            guncellenecek.AgriDuzeyi =
                model.AgriDuzeyi;

            guncellenecek.IdrarMiktari =
                model.IdrarMiktari;

            guncellenecek.BilincDurumu =
                model.BilincDurumu;

            guncellenecek.BeslenmeDurumu =
                model.BeslenmeDurumu;

            guncellenecek.VerilenIlaclar =
                model.VerilenIlaclar;

            guncellenecek.SerumTakibi =
                model.SerumTakibi;

            guncellenecek.BakimNotu =
                model.BakimNotu;

            guncellenecek.KritikDurumVarMi =
                model.KritikDurumVarMi;

            guncellenecek.KritikDurumNotu =
                model.KritikDurumNotu;

            _context.SaveChanges();

            TempData["Basari"] =
                "Hemşire gözlem kaydı başarıyla güncellendi.";

            return RedirectToAction(
                "Details",
                new
                {
                    id =
                        guncellenecek
                            .HemsireGozlemId
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
                    "Hemşire gözlem kaydını yalnızca Admin silebilir.";

                return RedirectToAction(
                    "Index"
                );
            }

            HemsireGozlem? gozlem =
                _context.HemsireGozlemleri
                    .FirstOrDefault(g =>
                        g.HemsireGozlemId == id &&
                        g.AktifMi
                    );

            if (gozlem == null)
            {
                TempData["Hata"] =
                    "Hemşire gözlem kaydı bulunamadı.";

                return RedirectToAction(
                    "Index"
                );
            }

            int hastaYatisId =
                gozlem.HastaYatisId;

            gozlem.AktifMi =
                false;

            _context.SaveChanges();

            TempData["Basari"] =
                "Hemşire gözlem kaydı silindi.";

            return RedirectToAction(
                "Gecmis",
                new
                {
                    hastaYatisId
                }
            );
        }
    }
}
