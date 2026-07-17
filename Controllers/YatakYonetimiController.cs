using HBYS.Web.Data;
using HBYS.Web.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace HBYS.Web.Controllers
{
    public class YatakYonetimiController : Controller
    {
        private readonly AppDbContext _context;

        private static readonly string[] OdaTipleri =
        {
            "Standart",
            "Tek Kişilik",
            "Çift Kişilik",
            "İzolasyon",
            "Yoğun Bakım"
        };

        private static readonly string[] CinsiyetKisitlamalari =
        {
            "Karma",
            "Kadın",
            "Erkek",
            "Çocuk"
        };

        private static readonly string[] YatakDurumlari =
        {
            "Boş",
            "Dolu",
            "Temizlikte",
            "Bakımda",
            "Rezerve"
        };

        public YatakYonetimiController(
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

        private bool YonetimYetkisiVarMi()
        {
            string? rolAdi =
                HttpContext.Session.GetString(
                    "RolAdi"
                );

            return rolAdi == "Admin" ||
                   rolAdi == "Yonetici";
        }

        private bool YatisIslemiYetkisiVarMi()
        {
            string? rolAdi =
                HttpContext.Session.GetString(
                    "RolAdi"
                );

            return rolAdi == "Admin" ||
                   rolAdi == "Sekreter" ||
                   rolAdi == "Doktor";
        }

        private IActionResult? GirisVeyaYetkiKontrolu(
            bool yonetimYetkisiGerekli
        )
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction(
                    "Login",
                    "Account"
                );
            }

            bool yetkiliMi =
                yonetimYetkisiGerekli
                    ? YonetimYetkisiVarMi()
                    : GoruntulemeYetkisiVarMi();

            if (!yetkiliMi)
            {
                TempData["Hata"] =
                    yonetimYetkisiGerekli
                        ? "Servis, oda ve yatak yönetimi için yetkiniz yok."
                        : "Yatak yönetimi ekranına erişim yetkiniz yok.";

                return RedirectToAction(
                    "Index",
                    "Dashboard"
                );
            }

            return null;
        }

        private static string? BosMetniNullYap(
            string? deger
        )
        {
            return string.IsNullOrWhiteSpace(deger)
                ? null
                : deger.Trim();
        }

        private void ServisListesiniHazirla(
            int? seciliServisId = null
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
                    seciliServisId
                );
        }

        private void OdaListesiniHazirla(
            int? seciliOdaId = null
        )
        {
            ViewBag.Odalar =
                new SelectList(
                    _context.Odalar
                        .Include(o => o.Servis)
                        .Where(o =>
                            o.AktifMi &&
                            o.Servis != null &&
                            o.Servis.AktifMi
                        )
                        .OrderBy(o =>
                            o.Servis!.ServisAdi
                        )
                        .ThenBy(o => o.OdaNo)
                        .Select(o => new
                        {
                            o.OdaId,

                            OdaBilgisi =
                                o.Servis!.ServisAdi +
                                " - Oda " +
                                o.OdaNo
                        })
                        .ToList(),

                    "OdaId",
                    "OdaBilgisi",
                    seciliOdaId
                );
        }

        private void OdaSecenekleriniHazirla(
            Oda model
        )
        {
            ServisListesiniHazirla(
                model.ServisId
            );

            ViewBag.OdaTipleri =
                new SelectList(
                    OdaTipleri,
                    model.OdaTipi
                );

            ViewBag.CinsiyetKisitlamalari =
                new SelectList(
                    CinsiyetKisitlamalari,
                    model.CinsiyetKisitlamasi
                );
        }

        private void YatakSecenekleriniHazirla(
            Yatak model
        )
        {
            OdaListesiniHazirla(
                model.OdaId
            );

            ViewBag.YatakDurumlari =
                new SelectList(
                    YatakDurumlari,
                    model.Durum
                );
        }

        public IActionResult Index(
            int? servisId,
            string? arama
        )
        {
            IActionResult? kontrol =
                GirisVeyaYetkiKontrolu(false);

            if (kontrol != null)
            {
                return kontrol;
            }

            IQueryable<Servis> sorgu =
                _context.Servisler
                    .Include(s => s.Odalar)
                        .ThenInclude(o => o.Yataklar)
                            .ThenInclude(y =>
                                y.HastaYatislari
                            )
                                .ThenInclude(h =>
                                    h.Hasta
                                )
                    .Where(s => s.AktifMi);

            if (
                servisId.HasValue &&
                servisId.Value > 0
            )
            {
                sorgu =
                    sorgu.Where(s =>
                        s.ServisId ==
                        servisId.Value
                    );
            }

            if (!string.IsNullOrWhiteSpace(arama))
            {
                string aramaMetni =
                    arama.Trim();

                sorgu =
                    sorgu.Where(s =>
                        s.ServisAdi
                            .Contains(aramaMetni) ||

                        (
                            s.Kat != null &&
                            s.Kat.Contains(aramaMetni)
                        ) ||

                        s.Odalar.Any(o =>
                            o.AktifMi &&
                            (
                                o.OdaNo.Contains(
                                    aramaMetni
                                ) ||

                                o.OdaTipi.Contains(
                                    aramaMetni
                                ) ||

                                o.Yataklar.Any(y =>
                                    y.AktifMi &&
                                    (
                                        y.YatakNo.Contains(
                                            aramaMetni
                                        ) ||

                                        y.HastaYatislari.Any(h =>
                                            h.AktifMi &&
                                            h.Durum == "Yatıyor" &&
                                            h.Hasta != null &&
                                            (
                                                h.Hasta.Ad.Contains(
                                                    aramaMetni
                                                ) ||

                                                h.Hasta.Soyad.Contains(
                                                    aramaMetni
                                                ) ||

                                                h.Hasta.TcKimlikNo.Contains(
                                                    aramaMetni
                                                )
                                            )
                                        )
                                    )
                                )
                            )
                        )
                    );
            }

            List<Servis> servisler =
                sorgu
                    .OrderBy(s => s.ServisAdi)
                    .ToList();

            ViewBag.ServisFiltreListesi =
                new SelectList(
                    _context.Servisler
                        .Where(s => s.AktifMi)
                        .OrderBy(s => s.ServisAdi)
                        .ToList(),

                    "ServisId",
                    "ServisAdi",
                    servisId
                );

            ViewBag.Arama =
                arama;

            ViewBag.YonetimYetkisiVarMi =
                YonetimYetkisiVarMi();

            ViewBag.YatisIslemiYetkisiVarMi =
                YatisIslemiYetkisiVarMi();

            ViewBag.ToplamServis =
                _context.Servisler.Count(s =>
                    s.AktifMi
                );

            ViewBag.ToplamOda =
                _context.Odalar.Count(o =>
                    o.AktifMi
                );

            int toplamYatak =
                _context.Yataklar.Count(y =>
                    y.AktifMi
                );

            int bosYatak =
                _context.Yataklar.Count(y =>
                    y.AktifMi &&
                    y.Durum == "Boş"
                );

            int doluYatak =
                _context.Yataklar.Count(y =>
                    y.AktifMi &&
                    y.Durum == "Dolu"
                );

            ViewBag.ToplamYatak =
                toplamYatak;

            ViewBag.BosYatak =
                bosYatak;

            ViewBag.DoluYatak =
                doluYatak;

            ViewBag.DolulukOrani =
                toplamYatak == 0
                    ? 0
                    : Math.Round(
                        doluYatak *
                        100.0 /
                        toplamYatak,
                        1
                    );

            return View(servisler);
        }

        [HttpGet]
        public IActionResult CreateServis()
        {
            IActionResult? kontrol =
                GirisVeyaYetkiKontrolu(true);

            if (kontrol != null)
            {
                return kontrol;
            }

            return View(new Servis());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateServis(
            Servis model
        )
        {
            IActionResult? kontrol =
                GirisVeyaYetkiKontrolu(true);

            if (kontrol != null)
            {
                return kontrol;
            }

            model.ServisAdi =
                model.ServisAdi?.Trim() ??
                string.Empty;

            model.Kat =
                BosMetniNullYap(model.Kat);

            model.Aciklama =
                BosMetniNullYap(
                    model.Aciklama
                );

            bool ayniServisVarMi =
                _context.Servisler.Any(s =>
                    s.AktifMi &&
                    s.ServisAdi ==
                    model.ServisAdi
                );

            if (ayniServisVarMi)
            {
                ModelState.AddModelError(
                    "ServisAdi",
                    "Bu servis adı zaten kullanılıyor."
                );
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            model.AktifMi = true;
            model.KayitTarihi = DateTime.Now;

            _context.Servisler.Add(model);
            _context.SaveChanges();

            TempData["Basari"] =
                "Servis başarıyla eklendi.";

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult EditServis(
            int id
        )
        {
            IActionResult? kontrol =
                GirisVeyaYetkiKontrolu(true);

            if (kontrol != null)
            {
                return kontrol;
            }

            Servis? servis =
                _context.Servisler
                    .FirstOrDefault(s =>
                        s.ServisId == id &&
                        s.AktifMi
                    );

            if (servis == null)
            {
                TempData["Hata"] =
                    "Servis bulunamadı.";

                return RedirectToAction("Index");
            }

            return View(servis);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditServis(
            Servis model
        )
        {
            IActionResult? kontrol =
                GirisVeyaYetkiKontrolu(true);

            if (kontrol != null)
            {
                return kontrol;
            }

            Servis? guncellenecek =
                _context.Servisler
                    .FirstOrDefault(s =>
                        s.ServisId ==
                        model.ServisId &&

                        s.AktifMi
                    );

            if (guncellenecek == null)
            {
                TempData["Hata"] =
                    "Servis bulunamadı.";

                return RedirectToAction("Index");
            }

            model.ServisAdi =
                model.ServisAdi?.Trim() ??
                string.Empty;

            model.Kat =
                BosMetniNullYap(model.Kat);

            model.Aciklama =
                BosMetniNullYap(
                    model.Aciklama
                );

            bool ayniServisVarMi =
                _context.Servisler.Any(s =>
                    s.AktifMi &&
                    s.ServisId !=
                    model.ServisId &&
                    s.ServisAdi ==
                    model.ServisAdi
                );

            if (ayniServisVarMi)
            {
                ModelState.AddModelError(
                    "ServisAdi",
                    "Bu servis adı başka bir kayıtta kullanılıyor."
                );
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            guncellenecek.ServisAdi =
                model.ServisAdi;

            guncellenecek.Kat =
                model.Kat;

            guncellenecek.Aciklama =
                model.Aciklama;

            _context.SaveChanges();

            TempData["Basari"] =
                "Servis başarıyla güncellendi.";

            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteServis(
            int id
        )
        {
            IActionResult? kontrol =
                GirisVeyaYetkiKontrolu(true);

            if (kontrol != null)
            {
                return kontrol;
            }

            Servis? servis =
                _context.Servisler
                    .FirstOrDefault(s =>
                        s.ServisId == id &&
                        s.AktifMi
                    );

            if (servis == null)
            {
                TempData["Hata"] =
                    "Servis bulunamadı.";

                return RedirectToAction("Index");
            }

            bool aktifOdaVarMi =
                _context.Odalar.Any(o =>
                    o.ServisId == id &&
                    o.AktifMi
                );

            if (aktifOdaVarMi)
            {
                TempData["Hata"] =
                    "Aktif odaları bulunan servis silinemez.";

                return RedirectToAction("Index");
            }

            servis.AktifMi = false;

            _context.SaveChanges();

            TempData["Basari"] =
                "Servis başarıyla silindi.";

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult CreateOda(
            int? servisId
        )
        {
            IActionResult? kontrol =
                GirisVeyaYetkiKontrolu(true);

            if (kontrol != null)
            {
                return kontrol;
            }

            Oda model =
                new Oda
                {
                    ServisId =
                        servisId ?? 0,

                    OdaTipi =
                        "Standart",

                    CinsiyetKisitlamasi =
                        "Karma"
                };

            OdaSecenekleriniHazirla(model);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateOda(
            Oda model
        )
        {
            IActionResult? kontrol =
                GirisVeyaYetkiKontrolu(true);

            if (kontrol != null)
            {
                return kontrol;
            }

            model.OdaNo =
                model.OdaNo?.Trim() ??
                string.Empty;

            model.Aciklama =
                BosMetniNullYap(
                    model.Aciklama
                );

            bool servisVarMi =
                _context.Servisler.Any(s =>
                    s.ServisId ==
                    model.ServisId &&
                    s.AktifMi
                );

            if (!servisVarMi)
            {
                ModelState.AddModelError(
                    "ServisId",
                    "Geçerli bir servis seçiniz."
                );
            }

            if (!OdaTipleri.Contains(model.OdaTipi))
            {
                ModelState.AddModelError(
                    "OdaTipi",
                    "Geçerli bir oda tipi seçiniz."
                );
            }

            if (
                !CinsiyetKisitlamalari.Contains(
                    model.CinsiyetKisitlamasi
                )
            )
            {
                ModelState.AddModelError(
                    "CinsiyetKisitlamasi",
                    "Geçerli bir cinsiyet kısıtlaması seçiniz."
                );
            }

            bool ayniOdaVarMi =
                _context.Odalar.Any(o =>
                    o.AktifMi &&
                    o.ServisId ==
                    model.ServisId &&
                    o.OdaNo ==
                    model.OdaNo
                );

            if (ayniOdaVarMi)
            {
                ModelState.AddModelError(
                    "OdaNo",
                    "Seçilen serviste bu oda numarası zaten kullanılıyor."
                );
            }

            if (!ModelState.IsValid)
            {
                OdaSecenekleriniHazirla(model);

                return View(model);
            }

            model.AktifMi = true;
            model.KayitTarihi = DateTime.Now;

            _context.Odalar.Add(model);
            _context.SaveChanges();

            TempData["Basari"] =
                "Oda başarıyla eklendi.";

            return RedirectToAction(
                "Index",
                new
                {
                    servisId =
                        model.ServisId
                }
            );
        }

        [HttpGet]
        public IActionResult EditOda(
            int id
        )
        {
            IActionResult? kontrol =
                GirisVeyaYetkiKontrolu(true);

            if (kontrol != null)
            {
                return kontrol;
            }

            Oda? oda =
                _context.Odalar
                    .FirstOrDefault(o =>
                        o.OdaId == id &&
                        o.AktifMi
                    );

            if (oda == null)
            {
                TempData["Hata"] =
                    "Oda bulunamadı.";

                return RedirectToAction("Index");
            }

            OdaSecenekleriniHazirla(oda);

            return View(oda);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditOda(
            Oda model
        )
        {
            IActionResult? kontrol =
                GirisVeyaYetkiKontrolu(true);

            if (kontrol != null)
            {
                return kontrol;
            }

            Oda? guncellenecek =
                _context.Odalar
                    .FirstOrDefault(o =>
                        o.OdaId ==
                        model.OdaId &&

                        o.AktifMi
                    );

            if (guncellenecek == null)
            {
                TempData["Hata"] =
                    "Oda bulunamadı.";

                return RedirectToAction("Index");
            }

            model.OdaNo =
                model.OdaNo?.Trim() ??
                string.Empty;

            model.Aciklama =
                BosMetniNullYap(
                    model.Aciklama
                );

            bool servisVarMi =
                _context.Servisler.Any(s =>
                    s.ServisId ==
                    model.ServisId &&
                    s.AktifMi
                );

            if (!servisVarMi)
            {
                ModelState.AddModelError(
                    "ServisId",
                    "Geçerli bir servis seçiniz."
                );
            }

            if (!OdaTipleri.Contains(model.OdaTipi))
            {
                ModelState.AddModelError(
                    "OdaTipi",
                    "Geçerli bir oda tipi seçiniz."
                );
            }

            if (
                !CinsiyetKisitlamalari.Contains(
                    model.CinsiyetKisitlamasi
                )
            )
            {
                ModelState.AddModelError(
                    "CinsiyetKisitlamasi",
                    "Geçerli bir cinsiyet kısıtlaması seçiniz."
                );
            }

            bool ayniOdaVarMi =
                _context.Odalar.Any(o =>
                    o.AktifMi &&
                    o.OdaId !=
                    model.OdaId &&
                    o.ServisId ==
                    model.ServisId &&
                    o.OdaNo ==
                    model.OdaNo
                );

            if (ayniOdaVarMi)
            {
                ModelState.AddModelError(
                    "OdaNo",
                    "Seçilen serviste bu oda numarası kullanılıyor."
                );
            }

            bool odadaYatanHastaVarMi =
                _context.HastaYatislari.Any(y =>
                    y.AktifMi &&
                    y.Durum == "Yatıyor" &&
                    y.Yatak != null &&
                    y.Yatak.OdaId ==
                    model.OdaId
                );

            if (
                odadaYatanHastaVarMi &&
                guncellenecek.ServisId !=
                model.ServisId
            )
            {
                ModelState.AddModelError(
                    "ServisId",
                    "İçinde yatan hasta bulunan oda başka servise taşınamaz."
                );
            }

            if (!ModelState.IsValid)
            {
                OdaSecenekleriniHazirla(model);

                return View(model);
            }

            guncellenecek.ServisId =
                model.ServisId;

            guncellenecek.OdaNo =
                model.OdaNo;

            guncellenecek.OdaTipi =
                model.OdaTipi;

            guncellenecek.CinsiyetKisitlamasi =
                model.CinsiyetKisitlamasi;

            guncellenecek.Aciklama =
                model.Aciklama;

            _context.SaveChanges();

            TempData["Basari"] =
                "Oda başarıyla güncellendi.";

            return RedirectToAction(
                "Index",
                new
                {
                    servisId =
                        model.ServisId
                }
            );
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteOda(
            int id
        )
        {
            IActionResult? kontrol =
                GirisVeyaYetkiKontrolu(true);

            if (kontrol != null)
            {
                return kontrol;
            }

            Oda? oda =
                _context.Odalar
                    .FirstOrDefault(o =>
                        o.OdaId == id &&
                        o.AktifMi
                    );

            if (oda == null)
            {
                TempData["Hata"] =
                    "Oda bulunamadı.";

                return RedirectToAction("Index");
            }

            bool aktifYatakVarMi =
                _context.Yataklar.Any(y =>
                    y.OdaId == id &&
                    y.AktifMi
                );

            if (aktifYatakVarMi)
            {
                TempData["Hata"] =
                    "Aktif yatakları bulunan oda silinemez.";

                return RedirectToAction(
                    "Index",
                    new
                    {
                        servisId =
                            oda.ServisId
                    }
                );
            }

            oda.AktifMi = false;

            _context.SaveChanges();

            TempData["Basari"] =
                "Oda başarıyla silindi.";

            return RedirectToAction(
                "Index",
                new
                {
                    servisId =
                        oda.ServisId
                }
            );
        }

        [HttpGet]
        public IActionResult CreateYatak(
            int? odaId
        )
        {
            IActionResult? kontrol =
                GirisVeyaYetkiKontrolu(true);

            if (kontrol != null)
            {
                return kontrol;
            }

            Yatak model =
                new Yatak
                {
                    OdaId =
                        odaId ?? 0,

                    Durum =
                        "Boş"
                };

            YatakSecenekleriniHazirla(model);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CreateYatak(
            Yatak model
        )
        {
            IActionResult? kontrol =
                GirisVeyaYetkiKontrolu(true);

            if (kontrol != null)
            {
                return kontrol;
            }

            model.YatakNo =
                model.YatakNo?.Trim() ??
                string.Empty;

            model.Ozellik =
                BosMetniNullYap(
                    model.Ozellik
                );

            bool odaVarMi =
                _context.Odalar.Any(o =>
                    o.OdaId ==
                    model.OdaId &&
                    o.AktifMi &&
                    o.Servis != null &&
                    o.Servis.AktifMi
                );

            if (!odaVarMi)
            {
                ModelState.AddModelError(
                    "OdaId",
                    "Geçerli bir oda seçiniz."
                );
            }

            if (!YatakDurumlari.Contains(model.Durum))
            {
                ModelState.AddModelError(
                    "Durum",
                    "Geçerli bir yatak durumu seçiniz."
                );
            }

            if (model.Durum == "Dolu")
            {
                ModelState.AddModelError(
                    "Durum",
                    "Yeni yatak dolu oluşturulamaz."
                );
            }

            bool ayniYatakVarMi =
                _context.Yataklar.Any(y =>
                    y.AktifMi &&
                    y.OdaId ==
                    model.OdaId &&
                    y.YatakNo ==
                    model.YatakNo
                );

            if (ayniYatakVarMi)
            {
                ModelState.AddModelError(
                    "YatakNo",
                    "Seçilen odada bu yatak numarası zaten kullanılıyor."
                );
            }

            if (!ModelState.IsValid)
            {
                YatakSecenekleriniHazirla(model);

                return View(model);
            }

            model.AktifMi = true;
            model.KayitTarihi = DateTime.Now;

            _context.Yataklar.Add(model);
            _context.SaveChanges();

            int servisId =
                _context.Odalar
                    .Where(o =>
                        o.OdaId ==
                        model.OdaId
                    )
                    .Select(o =>
                        o.ServisId
                    )
                    .FirstOrDefault();

            TempData["Basari"] =
                "Yatak başarıyla eklendi.";

            return RedirectToAction(
                "Index",
                new
                {
                    servisId
                }
            );
        }

        [HttpGet]
        public IActionResult EditYatak(
            int id
        )
        {
            IActionResult? kontrol =
                GirisVeyaYetkiKontrolu(true);

            if (kontrol != null)
            {
                return kontrol;
            }

            Yatak? yatak =
                _context.Yataklar
                    .FirstOrDefault(y =>
                        y.YatakId == id &&
                        y.AktifMi
                    );

            if (yatak == null)
            {
                TempData["Hata"] =
                    "Yatak bulunamadı.";

                return RedirectToAction("Index");
            }

            bool aktifYatisVarMi =
                _context.HastaYatislari.Any(y =>
                    y.AktifMi &&
                    y.YatakId == id &&
                    y.Durum == "Yatıyor"
                );

            YatakSecenekleriniHazirla(yatak);

            ViewBag.YatakDoluMu =
                aktifYatisVarMi ||
                yatak.Durum == "Dolu";

            return View(yatak);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditYatak(
            Yatak model
        )
        {
            IActionResult? kontrol =
                GirisVeyaYetkiKontrolu(true);

            if (kontrol != null)
            {
                return kontrol;
            }

            Yatak? guncellenecek =
                _context.Yataklar
                    .FirstOrDefault(y =>
                        y.YatakId ==
                        model.YatakId &&
                        y.AktifMi
                    );

            if (guncellenecek == null)
            {
                TempData["Hata"] =
                    "Yatak bulunamadı.";

                return RedirectToAction("Index");
            }

            model.YatakNo =
                model.YatakNo?.Trim() ??
                string.Empty;

            model.Ozellik =
                BosMetniNullYap(
                    model.Ozellik
                );

            bool aktifYatisVarMi =
                _context.HastaYatislari.Any(y =>
                    y.AktifMi &&
                    y.YatakId ==
                    model.YatakId &&
                    y.Durum == "Yatıyor"
                );

            bool odaVarMi =
                _context.Odalar.Any(o =>
                    o.OdaId ==
                    model.OdaId &&
                    o.AktifMi &&
                    o.Servis != null &&
                    o.Servis.AktifMi
                );

            if (!odaVarMi)
            {
                ModelState.AddModelError(
                    "OdaId",
                    "Geçerli bir oda seçiniz."
                );
            }

            if (!YatakDurumlari.Contains(model.Durum))
            {
                ModelState.AddModelError(
                    "Durum",
                    "Geçerli bir yatak durumu seçiniz."
                );
            }

            if (
                aktifYatisVarMi &&
                (
                    model.Durum != "Dolu" ||
                    model.OdaId !=
                    guncellenecek.OdaId
                )
            )
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Yatan hastası bulunan yatağın durumu veya odası değiştirilemez."
                );
            }

            if (
                !aktifYatisVarMi &&
                model.Durum == "Dolu"
            )
            {
                ModelState.AddModelError(
                    "Durum",
                    "Yatak manuel olarak dolu yapılamaz. Hasta yatışı sırasında otomatik olarak dolu olur."
                );
            }

            bool ayniYatakVarMi =
                _context.Yataklar.Any(y =>
                    y.AktifMi &&
                    y.YatakId !=
                    model.YatakId &&
                    y.OdaId ==
                    model.OdaId &&
                    y.YatakNo ==
                    model.YatakNo
                );

            if (ayniYatakVarMi)
            {
                ModelState.AddModelError(
                    "YatakNo",
                    "Seçilen odada bu yatak numarası kullanılıyor."
                );
            }

            if (!ModelState.IsValid)
            {
                YatakSecenekleriniHazirla(model);

                ViewBag.YatakDoluMu =
                    aktifYatisVarMi;

                return View(model);
            }

            guncellenecek.OdaId =
                model.OdaId;

            guncellenecek.YatakNo =
                model.YatakNo;

            guncellenecek.Durum =
                model.Durum;

            guncellenecek.Ozellik =
                model.Ozellik;

            _context.SaveChanges();

            int servisId =
                _context.Odalar
                    .Where(o =>
                        o.OdaId ==
                        model.OdaId
                    )
                    .Select(o =>
                        o.ServisId
                    )
                    .FirstOrDefault();

            TempData["Basari"] =
                model.Durum == "Boş"
                    ? "Yatak güncellendi ve yeniden kullanıma açıldı."
                    : "Yatak başarıyla güncellendi.";

            return RedirectToAction(
                "Index",
                new
                {
                    servisId
                }
            );
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteYatak(
            int id
        )
        {
            IActionResult? kontrol =
                GirisVeyaYetkiKontrolu(true);

            if (kontrol != null)
            {
                return kontrol;
            }

            Yatak? yatak =
                _context.Yataklar
                    .Include(y => y.Oda)
                    .FirstOrDefault(y =>
                        y.YatakId == id &&
                        y.AktifMi
                    );

            if (yatak == null)
            {
                TempData["Hata"] =
                    "Yatak bulunamadı.";

                return RedirectToAction("Index");
            }

            bool aktifYatisVarMi =
                _context.HastaYatislari.Any(y =>
                    y.AktifMi &&
                    y.YatakId == id &&
                    y.Durum == "Yatıyor"
                );

            if (
                aktifYatisVarMi ||
                yatak.Durum == "Dolu"
            )
            {
                TempData["Hata"] =
                    "Yatan hastası bulunan yatak silinemez.";

                return RedirectToAction(
                    "Index",
                    new
                    {
                        servisId =
                            yatak.Oda?.ServisId
                    }
                );
            }

            yatak.AktifMi = false;

            _context.SaveChanges();

            TempData["Basari"] =
                "Yatak başarıyla silindi.";

            return RedirectToAction(
                "Index",
                new
                {
                    servisId =
                        yatak.Oda?.ServisId
                }
            );
        }
    }
}
