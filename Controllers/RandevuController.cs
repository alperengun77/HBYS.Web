using System.Globalization;
using HBYS.Web.Data;
using HBYS.Web.Helpers;
using HBYS.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HBYS.Web.Controllers
{
    public class RandevuController : Controller
    {
        private readonly AppDbContext _context;

        private static readonly TimeSpan RandevuBaslangicSaati =
            new TimeSpan(9, 0, 0);

        /*
            Hastane saat 16:00'da randevu kabulünü bitirir.
            Son 15 dakikalık randevu başlangıcı 15:45'tir.
        */
        private static readonly TimeSpan RandevuBitisSaati =
            new TimeSpan(15, 45, 0);

        private static readonly TimeSpan OgleArasiBaslangicSaati =
            new TimeSpan(12, 0, 0);

        private static readonly TimeSpan OgleArasiBitisSaati =
            new TimeSpan(13, 0, 0);

        public RandevuController(AppDbContext context)
        {
            _context = context;
        }

        private bool GirisYapildiMi()
        {
            string? kullaniciAdi =
                HttpContext.Session.GetString("KullaniciAdi");

            return !string.IsNullOrWhiteSpace(kullaniciAdi);
        }

        private bool RandevuGormeYetkisiVarMi()
        {
            string? rolAdi =
                HttpContext.Session.GetString("RolAdi");

            return rolAdi == "Admin" ||
                   rolAdi == "Yonetici" ||
                   rolAdi == "Sekreter" ||
                   rolAdi == "Doktor";
        }

        private bool RandevuIslemiYetkisiVarMi()
        {
            return RandevuGormeYetkisiVarMi();
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

        private IQueryable<Poliklinik>
            RandevuyaAcikPoliklinikler()
        {
            return _context.Poliklinikler
                .Where(p =>
                    p.AktifMi &&
                    !EF.Functions.Like(
                        p.PoliklinikAdi,
                        "%Acil%"
                    )
                );
        }

        private IQueryable<Doktor>
            RandevuyaAcikDoktorlar()
        {
            return _context.Doktorlar
                .Include(d => d.Poliklinik)
                .Where(d =>
                    d.AktifMi &&
                    d.Poliklinik != null &&
                    d.Poliklinik.AktifMi &&
                    !EF.Functions.Like(
                        d.Poliklinik.PoliklinikAdi,
                        "%Acil%"
                    )
                );
        }

        private bool PoliklinikRandevuyaAcikMi(
            int poliklinikId
        )
        {
            return RandevuyaAcikPoliklinikler()
                .Any(p =>
                    p.PoliklinikId == poliklinikId
                );
        }

        private bool RandevuCalismaSaatindeMi(
            DateTime tarih
        )
        {
            TimeSpan saat =
                tarih.TimeOfDay;

            bool calismaSaatindeMi =
                saat >= RandevuBaslangicSaati &&
                saat <= RandevuBitisSaati;

            bool ogleArasindaMi =
                saat >= OgleArasiBaslangicSaati &&
                saat < OgleArasiBitisSaati;

            return calismaSaatindeMi &&
                   !ogleArasindaMi;
        }

        private List<string> RandevuSaatleriniGetir()
        {
            List<string> saatler =
                new List<string>();

            for (
                TimeSpan saat = RandevuBaslangicSaati;
                saat <= RandevuBitisSaati;
                saat = saat.Add(
                    TimeSpan.FromMinutes(15)
                )
            )
            {
                bool ogleArasindaMi =
                    saat >= OgleArasiBaslangicSaati &&
                    saat < OgleArasiBitisSaati;

                if (ogleArasindaMi)
                {
                    continue;
                }

                saatler.Add(
                    saat.ToString(@"hh\:mm")
                );
            }

            return saatler;
        }

        private List<string>
            BosRandevuSaatleriniGetir(
                int doktorId,
                DateTime tarih,
                int? haricRandevuId = null
            )
        {
            DateTime secilenGun =
                tarih.Date;

            bool doktorRandevuyaAcikMi =
                RandevuyaAcikDoktorlar()
                    .Any(d =>
                        d.DoktorId == doktorId
                    );

            if (!doktorRandevuyaAcikMi)
            {
                return new List<string>();
            }

            Randevu? haricRandevu = null;

            if (haricRandevuId.HasValue)
            {
                haricRandevu =
                    _context.Randevular
                        .FirstOrDefault(r =>
                            r.RandevuId ==
                            haricRandevuId.Value &&

                            r.DoktorId ==
                            doktorId &&

                            r.AktifMi
                        );
            }

            if (secilenGun < DateTime.Today)
            {
                return new List<string>();
            }

            DateTime ertesiGun =
                secilenGun.AddDays(1);

            IQueryable<Randevu> doluRandevular =
                _context.Randevular
                    .Where(r =>
                        r.AktifMi &&

                        r.DoktorId ==
                        doktorId &&

                        r.Durum !=
                        "Iptal" &&

                        r.RandevuTarihiSaati >=
                        secilenGun &&

                        r.RandevuTarihiSaati <
                        ertesiGun
                    );

            if (haricRandevuId.HasValue)
            {
                doluRandevular =
                    doluRandevular.Where(r =>
                        r.RandevuId !=
                        haricRandevuId.Value
                    );
            }

            HashSet<TimeSpan> doluSaatler =
                doluRandevular
                    .Select(r =>
                        r.RandevuTarihiSaati
                    )
                    .ToList()
                    .Select(tarihSaat =>
                        tarihSaat.TimeOfDay
                    )
                    .ToHashSet();

            List<string> bosSaatler =
                new List<string>();

            foreach (
                string saatMetni
                in RandevuSaatleriniGetir()
            )
            {
                TimeSpan saat =
                    TimeSpan.ParseExact(
                        saatMetni,
                        @"hh\:mm",
                        CultureInfo.InvariantCulture
                    );

                DateTime randevuTarihiSaati =
                    secilenGun.Add(saat);

                if (
                    randevuTarihiSaati <=
                    DateTime.Now
                )
                {
                    continue;
                }

                if (!doluSaatler.Contains(saat))
                {
                    bosSaatler.Add(saatMetni);
                }
            }

            if (
                haricRandevu != null &&

                haricRandevu
                    .RandevuTarihiSaati.Date ==
                secilenGun &&

                RandevuCalismaSaatindeMi(
                    haricRandevu
                        .RandevuTarihiSaati
                )
            )
            {
                string mevcutSaat =
                    haricRandevu
                        .RandevuTarihiSaati
                        .ToString("HH:mm");

                if (!bosSaatler.Contains(mevcutSaat))
                {
                    bosSaatler.Insert(
                        0,
                        mevcutSaat
                    );
                }
            }

            return bosSaatler;
        }

        private void SecimListeleriniHazirla(
            int? seciliPoliklinikId = null,
            int? seciliDoktorId = null,
            int? seciliHastaId = null
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

                            AdSoyad =
                                h.Ad + " " +
                                h.Soyad + " - " +
                                h.TcKimlikNo
                        })
                        .ToList(),

                    "HastaId",
                    "AdSoyad",
                    seciliHastaId
                );

            ViewBag.Poliklinikler =
                new SelectList(
                    RandevuyaAcikPoliklinikler()
                        .OrderBy(p =>
                            p.PoliklinikAdi
                        )
                        .ToList(),

                    "PoliklinikId",
                    "PoliklinikAdi",
                    seciliPoliklinikId
                );

            IQueryable<Doktor> doktorlarQuery =
                RandevuyaAcikDoktorlar();

            if (
                seciliPoliklinikId.HasValue &&
                seciliPoliklinikId.Value > 0
            )
            {
                doktorlarQuery =
                    doktorlarQuery.Where(d =>
                        d.PoliklinikId ==
                        seciliPoliklinikId.Value
                    );
            }

            if (DoktorMu())
            {
                int? aktifDoktorId =
                    AktifDoktorId();

                if (aktifDoktorId.HasValue)
                {
                    doktorlarQuery =
                        doktorlarQuery.Where(d =>
                            d.DoktorId ==
                            aktifDoktorId.Value
                        );
                }
                else
                {
                    doktorlarQuery =
                        doktorlarQuery.Where(d =>
                            false
                        );
                }
            }

            ViewBag.Doktorlar =
                new SelectList(
                    doktorlarQuery
                        .OrderBy(d => d.Ad)
                        .ThenBy(d => d.Soyad)
                        .Select(d => new
                        {
                            d.DoktorId,

                            AdSoyad =
                                d.Ad + " " +
                                d.Soyad + " - " +
                                d.Poliklinik!
                                    .PoliklinikAdi
                        })
                        .ToList(),

                    "DoktorId",
                    "AdSoyad",
                    seciliDoktorId
                );

            ViewBag.Durumlar =
                new SelectList(
                    new List<string>
                    {
                        "Bekliyor",
                        "Tamamlandi",
                        "Iptal"
                    }
                );
        }

        private void RandevuModeliniDogrula(
            Randevu randevu,
            string randevuTarihi,
            string randevuSaati,
            int? haricRandevuId = null,
            bool gecmisMevcutRandevuyaIzinVer = false
        )
        {
            bool tarihSaatOlustuMu =
                false;

            if (
                string.IsNullOrWhiteSpace(
                    randevuTarihi
                )
            )
            {
                ModelState.AddModelError(
                    "RandevuTarihiSaati",
                    "Randevu tarihi seçiniz."
                );
            }

            if (
                string.IsNullOrWhiteSpace(
                    randevuSaati
                )
            )
            {
                ModelState.AddModelError(
                    "RandevuTarihiSaati",
                    "Randevu saati seçiniz."
                );
            }

            if (
                !string.IsNullOrWhiteSpace(
                    randevuTarihi
                ) &&

                !string.IsNullOrWhiteSpace(
                    randevuSaati
                )
            )
            {
                tarihSaatOlustuMu =
                    DateTime.TryParseExact(
                        $"{randevuTarihi} {randevuSaati}",
                        "yyyy-MM-dd HH:mm",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out DateTime olusanTarihSaat
                    );

                if (!tarihSaatOlustuMu)
                {
                    ModelState.AddModelError(
                        "RandevuTarihiSaati",
                        "Randevu tarihi veya saati geçersiz."
                    );
                }
                else
                {
                    randevu.RandevuTarihiSaati =
                        ValidationHelper
                            .SaniyeVeSaliseyiTemizle(
                                olusanTarihSaat
                            );

                    bool gecmisMi =
                        randevu
                            .RandevuTarihiSaati <=
                        DateTime.Now;

                    if (
                        gecmisMi &&
                        !gecmisMevcutRandevuyaIzinVer
                    )
                    {
                        ModelState.AddModelError(
                            "RandevuTarihiSaati",
                            "Geçmiş tarih veya saate randevu oluşturulamaz."
                        );
                    }

                    if (
                        !ValidationHelper
                            .RandevuSaatiGecerliMi(
                                randevu
                                    .RandevuTarihiSaati
                            )
                    )
                    {
                        ModelState.AddModelError(
                            "RandevuTarihiSaati",
                            "Randevu saati 15 dakikalık aralıklardan biri olmalıdır."
                        );
                    }

                    if (
                        !RandevuCalismaSaatindeMi(
                            randevu
                                .RandevuTarihiSaati
                        )
                    )
                    {
                        ModelState.AddModelError(
                            "RandevuTarihiSaati",
                            "Randevu saati 09:00 ile 16:00 arasında olmalı ve 12:00 ile 13:00 arasındaki öğle arasına denk gelmemelidir."
                        );
                    }
                }
            }

            if (randevu.HastaId <= 0)
            {
                ModelState.AddModelError(
                    "HastaId",
                    "Hasta seçiniz."
                );
            }

            if (randevu.PoliklinikId <= 0)
            {
                ModelState.AddModelError(
                    "PoliklinikId",
                    "Poliklinik seçiniz."
                );
            }
            else if (
                !PoliklinikRandevuyaAcikMi(
                    randevu.PoliklinikId
                )
            )
            {
                ModelState.AddModelError(
                    "PoliklinikId",
                    "Acil Servis için randevu oluşturulamaz."
                );
            }

            if (randevu.DoktorId <= 0)
            {
                ModelState.AddModelError(
                    "DoktorId",
                    "Doktor seçiniz."
                );
            }

            if (
                randevu.DoktorId > 0 &&
                randevu.PoliklinikId > 0
            )
            {
                bool doktorPoliklinikUyumluMu =
                    RandevuyaAcikDoktorlar()
                        .Any(d =>
                            d.DoktorId ==
                            randevu.DoktorId &&

                            d.PoliklinikId ==
                            randevu.PoliklinikId
                        );

                if (!doktorPoliklinikUyumluMu)
                {
                    ModelState.AddModelError(
                        "DoktorId",
                        "Seçilen doktor bu poliklinikte görevli değil veya poliklinik randevuya kapalıdır."
                    );
                }
            }

            if (
                tarihSaatOlustuMu &&
                randevu.DoktorId > 0
            )
            {
                bool randevuCakismasiVarMi =
                    _context.Randevular
                        .Any(r =>
                            r.AktifMi &&

                            (
                                !haricRandevuId.HasValue ||
                                r.RandevuId !=
                                haricRandevuId.Value
                            ) &&

                            r.DoktorId ==
                            randevu.DoktorId &&

                            r.RandevuTarihiSaati ==
                            randevu
                                .RandevuTarihiSaati &&

                            r.Durum !=
                            "Iptal"
                        );

                if (randevuCakismasiVarMi)
                {
                    ModelState.AddModelError(
                        "RandevuTarihiSaati",
                        "Seçilen saat dolu. Lütfen doktorun boş saatlerinden birini seçiniz."
                    );
                }
            }
        }

        [HttpGet]
        public IActionResult DoktorlariGetir(
            int poliklinikId
        )
        {
            if (
                !GirisYapildiMi() ||
                !PoliklinikRandevuyaAcikMi(
                    poliklinikId
                )
            )
            {
                return Json(
                    new List<object>()
                );
            }

            IQueryable<Doktor> doktorlarQuery =
                RandevuyaAcikDoktorlar()
                    .Where(d =>
                        d.PoliklinikId ==
                        poliklinikId
                    );

            if (DoktorMu())
            {
                int? aktifDoktorId =
                    AktifDoktorId();

                if (!aktifDoktorId.HasValue)
                {
                    return Json(
                        new List<object>()
                    );
                }

                doktorlarQuery =
                    doktorlarQuery.Where(d =>
                        d.DoktorId ==
                        aktifDoktorId.Value
                    );
            }

            var doktorlar =
                doktorlarQuery
                    .OrderBy(d => d.Ad)
                    .ThenBy(d => d.Soyad)
                    .Select(d => new
                    {
                        doktorId =
                            d.DoktorId,

                        adSoyad =
                            d.Ad + " " +
                            d.Soyad
                    })
                    .ToList();

            return Json(doktorlar);
        }

        [HttpGet]
        public IActionResult BosSaatleriGetir(
            int doktorId,
            string randevuTarihi,
            int? haricRandevuId = null
        )
        {
            if (!GirisYapildiMi())
            {
                return Json(
                    new List<string>()
                );
            }

            if (
                DoktorMu() &&
                AktifDoktorId() != doktorId
            )
            {
                return Json(
                    new List<string>()
                );
            }

            bool tarihGecerliMi =
                DateTime.TryParseExact(
                    randevuTarihi,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime tarih
                );

            if (!tarihGecerliMi)
            {
                return Json(
                    new List<string>()
                );
            }

            List<string> bosSaatler =
                BosRandevuSaatleriniGetir(
                    doktorId,
                    tarih,
                    haricRandevuId
                );

            return Json(bosSaatler);
        }

        public IActionResult Index(
            string? arama,
            DateTime? baslangicTarihi,
            DateTime? bitisTarihi,
            int? doktorId,
            int? poliklinikId,
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

            if (!RandevuGormeYetkisiVarMi())
            {
                TempData["Hata"] =
                    "Randevu ekranına erişim yetkiniz yok.";

                return RedirectToAction(
                    "Index",
                    "Dashboard"
                );
            }

            IQueryable<Randevu> randevular =
                _context.Randevular
                    .Include(r => r.Hasta)
                    .Include(r => r.Doktor)
                    .Include(r => r.Poliklinik)
                    .Where(r =>
                        r.AktifMi &&

                        r.Poliklinik != null &&

                        !EF.Functions.Like(
                            r.Poliklinik
                                .PoliklinikAdi,
                            "%Acil%"
                        )
                    );

            if (DoktorMu())
            {
                int? aktifDoktorId =
                    AktifDoktorId();

                if (aktifDoktorId.HasValue)
                {
                    randevular =
                        randevular.Where(r =>
                            r.DoktorId ==
                            aktifDoktorId.Value
                        );

                    doktorId =
                        aktifDoktorId.Value;
                }
                else
                {
                    randevular =
                        randevular.Where(r =>
                            false
                        );

                    TempData["Hata"] =
                        "Bu doktor kullanıcısı herhangi bir doktor kaydıyla eşleştirilmemiş.";
                }
            }

            if (!string.IsNullOrWhiteSpace(arama))
            {
                randevular =
                    randevular.Where(r =>
                        r.Hasta!.Ad.Contains(arama) ||
                        r.Hasta.Soyad.Contains(arama) ||
                        r.Hasta.TcKimlikNo.Contains(arama) ||
                        r.Doktor!.Ad.Contains(arama) ||
                        r.Doktor.Soyad.Contains(arama)
                    );
            }

            if (baslangicTarihi.HasValue)
            {
                randevular =
                    randevular.Where(r =>
                        r.RandevuTarihiSaati >=
                        baslangicTarihi.Value.Date
                    );
            }

            if (bitisTarihi.HasValue)
            {
                DateTime bitis =
                    bitisTarihi.Value
                        .Date
                        .AddDays(1);

                randevular =
                    randevular.Where(r =>
                        r.RandevuTarihiSaati <
                        bitis
                    );
            }

            if (
                poliklinikId.HasValue &&
                poliklinikId.Value > 0
            )
            {
                randevular =
                    randevular.Where(r =>
                        r.PoliklinikId ==
                        poliklinikId.Value
                    );
            }

            if (
                doktorId.HasValue &&
                doktorId.Value > 0
            )
            {
                randevular =
                    randevular.Where(r =>
                        r.DoktorId ==
                        doktorId.Value
                    );
            }

            if (!string.IsNullOrWhiteSpace(durum))
            {
                randevular =
                    randevular.Where(r =>
                        r.Durum == durum
                    );
            }

            SecimListeleriniHazirla(
                poliklinikId,
                doktorId
            );

            ViewBag.Arama =
                arama;

            ViewBag.BaslangicTarihi =
                baslangicTarihi?
                    .ToString("yyyy-MM-dd");

            ViewBag.BitisTarihi =
                bitisTarihi?
                    .ToString("yyyy-MM-dd");

            ViewBag.SeciliPoliklinikId =
                poliklinikId;

            ViewBag.SeciliDoktorId =
                doktorId;

            ViewBag.SeciliDurum =
                durum;

            ViewBag.DoktorMu =
                DoktorMu();

            List<Randevu> liste =
                randevular
                    .OrderByDescending(r =>
                        r.RandevuTarihiSaati
                    )
                    .ToList();

            return View(liste);
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

            if (!RandevuIslemiYetkisiVarMi())
            {
                TempData["Hata"] =
                    "Randevu ekleme yetkiniz yok.";

                return RedirectToAction("Index");
            }

            int? seciliPoliklinikId = null;
            int? seciliDoktorId = null;

            if (DoktorMu())
            {
                int? aktifDoktorId =
                    AktifDoktorId();

                Doktor? doktor =
                    aktifDoktorId.HasValue
                        ? RandevuyaAcikDoktorlar()
                            .FirstOrDefault(d =>
                                d.DoktorId ==
                                aktifDoktorId.Value
                            )
                        : null;

                if (doktor == null)
                {
                    TempData["Hata"] =
                        "Acil Servis doktorları için randevu oluşturulamaz veya doktor kaydı eşleştirilmemiştir.";

                    return RedirectToAction("Index");
                }

                seciliDoktorId =
                    doktor.DoktorId;

                seciliPoliklinikId =
                    doktor.PoliklinikId;
            }

            DateTime varsayilanTarih =
                DateTime.Today;

            if (
                DateTime.Now >
                DateTime.Today.Add(
                    RandevuBitisSaati
                )
            )
            {
                varsayilanTarih =
                    DateTime.Today.AddDays(1);
            }

            SecimListeleriniHazirla(
                seciliPoliklinikId,
                seciliDoktorId
            );

            List<string> saatler =
                seciliDoktorId.HasValue
                    ? BosRandevuSaatleriniGetir(
                        seciliDoktorId.Value,
                        varsayilanTarih
                    )
                    : new List<string>();

            ViewBag.RandevuTarihi =
                varsayilanTarih
                    .ToString("yyyy-MM-dd");

            ViewBag.RandevuSaati =
                string.Empty;

            ViewBag.Saatler =
                saatler;

            ViewBag.DoktorMu =
                DoktorMu();

            ViewBag.MinRandevuTarihi =
                DateTime.Today
                    .ToString("yyyy-MM-dd");

            Randevu yeniRandevu =
                new Randevu
                {
                    RandevuTarihiSaati =
                        varsayilanTarih
                            .AddHours(9),

                    Durum =
                        "Bekliyor"
                };

            return View(yeniRandevu);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(
            Randevu randevu,
            string randevuTarihi,
            string randevuSaati
        )
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            if (!RandevuIslemiYetkisiVarMi())
            {
                TempData["Hata"] =
                    "Randevu ekleme yetkiniz yok.";

                return RedirectToAction("Index");
            }

            if (DoktorMu())
            {
                int? aktifDoktorId =
                    AktifDoktorId();

                Doktor? doktor =
                    aktifDoktorId.HasValue
                        ? RandevuyaAcikDoktorlar()
                            .FirstOrDefault(d =>
                                d.DoktorId ==
                                aktifDoktorId.Value
                            )
                        : null;

                if (doktor == null)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Acil Servis doktorları için randevu oluşturulamaz veya doktor kaydı bulunamadı."
                    );
                }
                else
                {
                    randevu.DoktorId =
                        doktor.DoktorId;

                    randevu.PoliklinikId =
                        doktor.PoliklinikId;
                }
            }

            RandevuModeliniDogrula(
                randevu,
                randevuTarihi,
                randevuSaati
            );

            if (!ModelState.IsValid)
            {
                SecimListeleriniHazirla(
                    randevu.PoliklinikId,
                    randevu.DoktorId,
                    randevu.HastaId
                );

                DateTime.TryParseExact(
                    randevuTarihi,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime secilenTarih
                );

                ViewBag.RandevuTarihi =
                    randevuTarihi;

                ViewBag.RandevuSaati =
                    randevuSaati;

                ViewBag.Saatler =
                    randevu.DoktorId > 0 &&
                    secilenTarih != default
                        ? BosRandevuSaatleriniGetir(
                            randevu.DoktorId,
                            secilenTarih
                        )
                        : new List<string>();

                ViewBag.DoktorMu =
                    DoktorMu();

                ViewBag.MinRandevuTarihi =
                    DateTime.Today
                        .ToString("yyyy-MM-dd");

                return View(randevu);
            }

            randevu.AktifMi =
                true;

            randevu.KayitTarihi =
                DateTime.Now;

            _context.Randevular.Add(randevu);
            _context.SaveChanges();

            TempData["Basari"] =
                "Randevu başarıyla eklendi.";

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

            if (!RandevuIslemiYetkisiVarMi())
            {
                TempData["Hata"] =
                    "Randevu düzenleme yetkiniz yok.";

                return RedirectToAction("Index");
            }

            Randevu? randevu =
                _context.Randevular
                    .Include(r => r.Doktor)
                    .Include(r => r.Poliklinik)
                    .FirstOrDefault(r =>
                        r.RandevuId == id &&
                        r.AktifMi
                    );

            if (randevu == null)
            {
                TempData["Hata"] =
                    "Randevu bulunamadı.";

                return RedirectToAction("Index");
            }

            if (
                DoktorMu() &&
                AktifDoktorId() !=
                randevu.DoktorId
            )
            {
                TempData["Hata"] =
                    "Başka doktora ait randevuyu düzenleyemezsiniz.";

                return RedirectToAction("Index");
            }

            if (
                randevu.Poliklinik != null &&

                randevu.Poliklinik
                    .PoliklinikAdi.Contains(
                        "Acil",
                        StringComparison.OrdinalIgnoreCase
                    )
            )
            {
                TempData["Hata"] =
                    "Acil Servis kayıtları randevu modülünde düzenlenemez.";

                return RedirectToAction("Index");
            }

            SecimListeleriniHazirla(
                randevu.PoliklinikId,
                randevu.DoktorId,
                randevu.HastaId
            );

            string seciliSaat =
                randevu
                    .RandevuTarihiSaati
                    .ToString("HH:mm");

            List<string> saatler =
                BosRandevuSaatleriniGetir(
                    randevu.DoktorId,
                    randevu
                        .RandevuTarihiSaati.Date,
                    randevu.RandevuId
                );

            if (
                !saatler.Contains(seciliSaat) &&

                RandevuCalismaSaatindeMi(
                    randevu
                        .RandevuTarihiSaati
                )
            )
            {
                saatler.Insert(
                    0,
                    seciliSaat
                );
            }

            ViewBag.RandevuTarihi =
                randevu
                    .RandevuTarihiSaati
                    .ToString("yyyy-MM-dd");

            ViewBag.RandevuSaati =
                RandevuCalismaSaatindeMi(
                    randevu.RandevuTarihiSaati
                )
                    ? seciliSaat
                    : string.Empty;

            ViewBag.Saatler =
                saatler;

            ViewBag.DoktorMu =
                DoktorMu();

            return View(randevu);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(
            Randevu randevu,
            string randevuTarihi,
            string randevuSaati
        )
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            if (!RandevuIslemiYetkisiVarMi())
            {
                TempData["Hata"] =
                    "Randevu düzenleme yetkiniz yok.";

                return RedirectToAction("Index");
            }

            Randevu? guncellenecekRandevu =
                _context.Randevular
                    .FirstOrDefault(r =>
                        r.RandevuId ==
                        randevu.RandevuId &&

                        r.AktifMi
                    );

            if (guncellenecekRandevu == null)
            {
                TempData["Hata"] =
                    "Randevu bulunamadı.";

                return RedirectToAction("Index");
            }

            if (DoktorMu())
            {
                int? aktifDoktorId =
                    AktifDoktorId();

                if (
                    !aktifDoktorId.HasValue ||

                    guncellenecekRandevu
                        .DoktorId !=
                    aktifDoktorId.Value
                )
                {
                    TempData["Hata"] =
                        "Başka doktora ait randevuyu düzenleyemezsiniz.";

                    return RedirectToAction("Index");
                }

                Doktor? doktor =
                    RandevuyaAcikDoktorlar()
                        .FirstOrDefault(d =>
                            d.DoktorId ==
                            aktifDoktorId.Value
                        );

                if (doktor == null)
                {
                    ModelState.AddModelError(
                        string.Empty,
                        "Acil Servis doktorları için randevu oluşturulamaz."
                    );
                }
                else
                {
                    randevu.DoktorId =
                        doktor.DoktorId;

                    randevu.PoliklinikId =
                        doktor.PoliklinikId;
                }
            }

            DateTime.TryParseExact(
                $"{randevuTarihi} {randevuSaati}",
                "yyyy-MM-dd HH:mm",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out DateTime yeniTarihSaat
            );

            bool mevcutTarihSaatKorunuyor =
                yeniTarihSaat != default &&

                ValidationHelper
                    .SaniyeVeSaliseyiTemizle(
                        yeniTarihSaat
                    ) ==

                guncellenecekRandevu
                    .RandevuTarihiSaati &&

                RandevuCalismaSaatindeMi(
                    guncellenecekRandevu
                        .RandevuTarihiSaati
                );

            RandevuModeliniDogrula(
                randevu,
                randevuTarihi,
                randevuSaati,
                randevu.RandevuId,
                mevcutTarihSaatKorunuyor
            );

            if (!ModelState.IsValid)
            {
                SecimListeleriniHazirla(
                    randevu.PoliklinikId,
                    randevu.DoktorId,
                    randevu.HastaId
                );

                DateTime.TryParseExact(
                    randevuTarihi,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime secilenTarih
                );

                ViewBag.RandevuTarihi =
                    randevuTarihi;

                ViewBag.RandevuSaati =
                    randevuSaati;

                ViewBag.Saatler =
                    randevu.DoktorId > 0 &&
                    secilenTarih != default
                        ? BosRandevuSaatleriniGetir(
                            randevu.DoktorId,
                            secilenTarih,
                            randevu.RandevuId
                        )
                        : new List<string>();

                ViewBag.DoktorMu =
                    DoktorMu();

                return View(randevu);
            }

            guncellenecekRandevu.HastaId =
                randevu.HastaId;

            guncellenecekRandevu.DoktorId =
                randevu.DoktorId;

            guncellenecekRandevu.PoliklinikId =
                randevu.PoliklinikId;

            guncellenecekRandevu
                .RandevuTarihiSaati =
                randevu.RandevuTarihiSaati;

            guncellenecekRandevu.Durum =
                randevu.Durum;

            guncellenecekRandevu.Aciklama =
                randevu.Aciklama;

            _context.SaveChanges();

            TempData["Basari"] =
                "Randevu başarıyla güncellendi.";

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

            Randevu? randevu =
                _context.Randevular
                    .Include(r => r.Hasta)
                    .Include(r => r.Doktor)
                    .Include(r => r.Poliklinik)
                    .FirstOrDefault(r =>
                        r.RandevuId == id &&

                        r.AktifMi &&

                        r.Poliklinik != null &&

                        !EF.Functions.Like(
                            r.Poliklinik
                                .PoliklinikAdi,
                            "%Acil%"
                        )
                    );

            if (randevu == null)
            {
                TempData["Hata"] =
                    "Randevu bulunamadı.";

                return RedirectToAction("Index");
            }

            if (
                DoktorMu() &&
                AktifDoktorId() !=
                randevu.DoktorId
            )
            {
                TempData["Hata"] =
                    "Başka doktora ait randevu detayını görüntüleyemezsiniz.";

                return RedirectToAction("Index");
            }

            return View(randevu);
        }

        public IActionResult Delete(int id)
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            if (!RandevuIslemiYetkisiVarMi())
            {
                TempData["Hata"] =
                    "Randevu silme yetkiniz yok.";

                return RedirectToAction("Index");
            }

            Randevu? randevu =
                _context.Randevular
                    .Include(r => r.Poliklinik)
                    .FirstOrDefault(r =>
                        r.RandevuId == id &&

                        r.AktifMi &&

                        r.Poliklinik != null &&

                        !EF.Functions.Like(
                            r.Poliklinik
                                .PoliklinikAdi,
                            "%Acil%"
                        )
                    );

            if (randevu == null)
            {
                TempData["Hata"] =
                    "Randevu bulunamadı.";

                return RedirectToAction("Index");
            }

            if (
                DoktorMu() &&
                AktifDoktorId() !=
                randevu.DoktorId
            )
            {
                TempData["Hata"] =
                    "Başka doktora ait randevuyu silemezsiniz.";

                return RedirectToAction("Index");
            }

            randevu.AktifMi =
                false;

            _context.SaveChanges();

            TempData["Basari"] =
                "Randevu başarıyla silindi.";

            return RedirectToAction("Index");
        }
    }
}