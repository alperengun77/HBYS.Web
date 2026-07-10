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

        public RandevuController(AppDbContext context)
        {
            _context = context;
        }

        private bool GirisYapildiMi()
        {
            string? kullaniciAdi = HttpContext.Session.GetString("KullaniciAdi");
            return !string.IsNullOrEmpty(kullaniciAdi);
        }

        private bool RandevuGormeYetkisiVarMi()
        {
            string? rolAdi = HttpContext.Session.GetString("RolAdi");

            return rolAdi == "Admin" ||
                   rolAdi == "Yonetici" ||
                   rolAdi == "Sekreter" ||
                   rolAdi == "Doktor";
        }

        private bool RandevuIslemiYetkisiVarMi()
        {
            string? rolAdi = HttpContext.Session.GetString("RolAdi");

            return rolAdi == "Admin" ||
                   rolAdi == "Yonetici" ||
                   rolAdi == "Sekreter" ||
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

        private List<string> RandevuSaatleriniGetir()
        {
            List<string> saatler = new List<string>();

            TimeSpan baslangic = new TimeSpan(8, 0, 0);
            TimeSpan bitis = new TimeSpan(17, 45, 0);

            for (TimeSpan saat = baslangic; saat <= bitis; saat = saat.Add(TimeSpan.FromMinutes(15)))
            {
                saatler.Add(saat.ToString(@"hh\:mm"));
            }

            return saatler;
        }

        private void SecimListeleriniHazirla(int? seciliPoliklinikId = null, int? seciliDoktorId = null, int? seciliHastaId = null)
        {
            ViewBag.Hastalar = new SelectList(
                _context.Hastalar
                    .Where(h => h.AktifMi)
                    .OrderBy(h => h.Ad)
                    .ThenBy(h => h.Soyad)
                    .Select(h => new
                    {
                        h.HastaId,
                        AdSoyad = h.Ad + " " + h.Soyad + " - " + h.TcKimlikNo
                    })
                    .ToList(),
                "HastaId",
                "AdSoyad",
                seciliHastaId
            );

            ViewBag.Poliklinikler = new SelectList(
                _context.Poliklinikler
                    .Where(p => p.AktifMi)
                    .OrderBy(p => p.PoliklinikAdi)
                    .ToList(),
                "PoliklinikId",
                "PoliklinikAdi",
                seciliPoliklinikId
            );

            var doktorlarQuery = _context.Doktorlar
                .Include(d => d.Poliklinik)
                .Where(d => d.AktifMi);

            if (seciliPoliklinikId.HasValue && seciliPoliklinikId.Value > 0)
            {
                doktorlarQuery = doktorlarQuery.Where(d => d.PoliklinikId == seciliPoliklinikId.Value);
            }

            if (DoktorMu())
            {
                int? aktifDoktorId = AktifDoktorId();

                if (aktifDoktorId.HasValue)
                {
                    doktorlarQuery = doktorlarQuery.Where(d => d.DoktorId == aktifDoktorId.Value);
                }
            }

            ViewBag.Doktorlar = new SelectList(
                doktorlarQuery
                    .OrderBy(d => d.Ad)
                    .ThenBy(d => d.Soyad)
                    .Select(d => new
                    {
                        d.DoktorId,
                        AdSoyad = d.Ad + " " + d.Soyad + " - " + d.Poliklinik!.PoliklinikAdi
                    })
                    .ToList(),
                "DoktorId",
                "AdSoyad",
                seciliDoktorId
            );

            ViewBag.Durumlar = new SelectList(
                new List<string>
                {
                    "Bekliyor",
                    "Tamamlandi",
                    "Iptal"
                }
            );
        }

        [HttpGet]
        public IActionResult DoktorlariGetir(int poliklinikId)
        {
            if (!GirisYapildiMi())
            {
                return Json(new List<object>());
            }

            var doktorlarQuery = _context.Doktorlar
                .Where(d => d.AktifMi && d.PoliklinikId == poliklinikId);

            if (DoktorMu())
            {
                int? aktifDoktorId = AktifDoktorId();

                if (aktifDoktorId.HasValue)
                {
                    doktorlarQuery = doktorlarQuery.Where(d => d.DoktorId == aktifDoktorId.Value);
                }
                else
                {
                    return Json(new List<object>());
                }
            }

            var doktorlar = doktorlarQuery
                .OrderBy(d => d.Ad)
                .ThenBy(d => d.Soyad)
                .Select(d => new
                {
                    doktorId = d.DoktorId,
                    adSoyad = d.Ad + " " + d.Soyad
                })
                .ToList();

            return Json(doktorlar);
        }

        public IActionResult Index(string? arama, DateTime? baslangicTarihi, DateTime? bitisTarihi, int? doktorId, int? poliklinikId, string? durum)
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction("Login", "Account");
            }

            if (!RandevuGormeYetkisiVarMi())
            {
                TempData["Hata"] = "Randevu ekranına erişim yetkiniz yok.";
                return RedirectToAction("Index", "Dashboard");
            }

            var randevular = _context.Randevular
                .Include(r => r.Hasta)
                .Include(r => r.Doktor)
                .Include(r => r.Poliklinik)
                .Where(r => r.AktifMi)
                .AsQueryable();

            if (DoktorMu())
            {
                int? aktifDoktorId = AktifDoktorId();

                if (aktifDoktorId.HasValue)
                {
                    randevular = randevular.Where(r => r.DoktorId == aktifDoktorId.Value);
                    doktorId = aktifDoktorId.Value;
                }
                else
                {
                    randevular = randevular.Where(r => false);
                    TempData["Hata"] = "Bu doktor kullanıcısı herhangi bir doktor kaydıyla eşleştirilmemiş.";
                }
            }

            if (!string.IsNullOrWhiteSpace(arama))
            {
                randevular = randevular.Where(r =>
                    r.Hasta!.Ad.Contains(arama) ||
                    r.Hasta.Soyad.Contains(arama) ||
                    r.Hasta.TcKimlikNo.Contains(arama) ||
                    r.Doktor!.Ad.Contains(arama) ||
                    r.Doktor.Soyad.Contains(arama));
            }

            if (baslangicTarihi.HasValue)
            {
                randevular = randevular.Where(r => r.RandevuTarihiSaati >= baslangicTarihi.Value.Date);
            }

            if (bitisTarihi.HasValue)
            {
                DateTime bitis = bitisTarihi.Value.Date.AddDays(1);
                randevular = randevular.Where(r => r.RandevuTarihiSaati < bitis);
            }

            if (poliklinikId.HasValue && poliklinikId.Value > 0)
            {
                randevular = randevular.Where(r => r.PoliklinikId == poliklinikId.Value);
            }

            if (doktorId.HasValue && doktorId.Value > 0)
            {
                randevular = randevular.Where(r => r.DoktorId == doktorId.Value);
            }

            if (!string.IsNullOrWhiteSpace(durum))
            {
                randevular = randevular.Where(r => r.Durum == durum);
            }

            SecimListeleriniHazirla(poliklinikId, doktorId);

            ViewBag.Arama = arama;
            ViewBag.BaslangicTarihi = baslangicTarihi?.ToString("yyyy-MM-dd");
            ViewBag.BitisTarihi = bitisTarihi?.ToString("yyyy-MM-dd");
            ViewBag.SeciliPoliklinikId = poliklinikId;
            ViewBag.SeciliDoktorId = doktorId;
            ViewBag.SeciliDurum = durum;
            ViewBag.DoktorMu = DoktorMu();

            List<Randevu> liste = randevular
                .OrderByDescending(r => r.RandevuTarihiSaati)
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

            if (!RandevuIslemiYetkisiVarMi())
            {
                TempData["Hata"] = "Randevu ekleme yetkiniz yok.";
                return RedirectToAction("Index");
            }

            int? aktifDoktorId = AktifDoktorId();
            int? seciliPoliklinikId = null;
            int? seciliDoktorId = null;

            if (DoktorMu() && aktifDoktorId.HasValue)
            {
                Doktor? doktor = _context.Doktorlar.FirstOrDefault(d => d.DoktorId == aktifDoktorId.Value);

                if (doktor != null)
                {
                    seciliDoktorId = doktor.DoktorId;
                    seciliPoliklinikId = doktor.PoliklinikId;
                }
            }

            SecimListeleriniHazirla(seciliPoliklinikId, seciliDoktorId);

            Randevu yeniRandevu = new Randevu
            {
                RandevuTarihiSaati = DateTime.Now.Date.AddHours(9),
                Durum = "Bekliyor"
            };

            ViewBag.RandevuTarihi = DateTime.Now.ToString("yyyy-MM-dd");
            ViewBag.RandevuSaati = "09:00";
            ViewBag.Saatler = RandevuSaatleriniGetir();
            ViewBag.DoktorMu = DoktorMu();

            return View(yeniRandevu);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(Randevu randevu, string randevuTarihi, string randevuSaati)
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction("Login", "Account");
            }

            if (!RandevuIslemiYetkisiVarMi())
            {
                TempData["Hata"] = "Randevu ekleme yetkiniz yok.";
                return RedirectToAction("Index");
            }

            if (DoktorMu())
            {
                int? aktifDoktorId = AktifDoktorId();

                if (aktifDoktorId.HasValue)
                {
                    Doktor? doktor = _context.Doktorlar.FirstOrDefault(d => d.DoktorId == aktifDoktorId.Value);

                    if (doktor != null)
                    {
                        randevu.DoktorId = doktor.DoktorId;
                        randevu.PoliklinikId = doktor.PoliklinikId;
                    }
                }
                else
                {
                    ModelState.AddModelError("", "Bu doktor kullanıcısı herhangi bir doktor kaydıyla eşleştirilmemiş.");
                }
            }

            if (string.IsNullOrWhiteSpace(randevuTarihi))
            {
                ModelState.AddModelError("RandevuTarihiSaati", "Randevu tarihi seçiniz.");
            }

            if (string.IsNullOrWhiteSpace(randevuSaati))
            {
                ModelState.AddModelError("RandevuTarihiSaati", "Randevu saati seçiniz.");
            }

            if (!string.IsNullOrWhiteSpace(randevuTarihi) && !string.IsNullOrWhiteSpace(randevuSaati))
            {
                bool tarihSaatOlustuMu = DateTime.TryParse($"{randevuTarihi} {randevuSaati}", out DateTime olusanTarihSaat);

                if (tarihSaatOlustuMu)
                {
                    randevu.RandevuTarihiSaati = ValidationHelper.SaniyeVeSaliseyiTemizle(olusanTarihSaat);
                }
                else
                {
                    ModelState.AddModelError("RandevuTarihiSaati", "Randevu tarihi veya saati geçersiz.");
                }
            }

            if (randevu.HastaId <= 0)
            {
                ModelState.AddModelError("HastaId", "Hasta seçiniz.");
            }

            if (randevu.PoliklinikId <= 0)
            {
                ModelState.AddModelError("PoliklinikId", "Poliklinik seçiniz.");
            }

            if (randevu.DoktorId <= 0)
            {
                ModelState.AddModelError("DoktorId", "Doktor seçiniz.");
            }

            bool doktorPoliklinikUyumluMu = _context.Doktorlar.Any(d =>
                d.AktifMi &&
                d.DoktorId == randevu.DoktorId &&
                d.PoliklinikId == randevu.PoliklinikId);

            if (!doktorPoliklinikUyumluMu)
            {
                ModelState.AddModelError("DoktorId", "Seçilen doktor bu poliklinikte görevli değil.");
            }

            if (randevu.RandevuTarihiSaati < DateTime.Now)
            {
                ModelState.AddModelError("RandevuTarihiSaati", "Geçmiş tarihe randevu oluşturulamaz.");
            }

            bool randevuCakismasiVarMi = _context.Randevular.Any(r =>
                r.AktifMi &&
                r.DoktorId == randevu.DoktorId &&
                r.RandevuTarihiSaati == randevu.RandevuTarihiSaati &&
                r.Durum != "Iptal");

            if (randevuCakismasiVarMi)
            {
                ModelState.AddModelError("RandevuTarihiSaati", "Bu doktora aynı tarih ve saatte başka bir randevu kayıtlı.");
            }

            if (!ModelState.IsValid)
            {
                SecimListeleriniHazirla(randevu.PoliklinikId, randevu.DoktorId, randevu.HastaId);

                ViewBag.RandevuTarihi = randevuTarihi;
                ViewBag.RandevuSaati = randevuSaati;
                ViewBag.Saatler = RandevuSaatleriniGetir();
                ViewBag.DoktorMu = DoktorMu();

                return View(randevu);
            }

            randevu.AktifMi = true;
            randevu.KayitTarihi = DateTime.Now;

            _context.Randevular.Add(randevu);
            _context.SaveChanges();

            TempData["Basari"] = "Randevu başarıyla eklendi.";

            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult Edit(int id)
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction("Login", "Account");
            }

            if (!RandevuIslemiYetkisiVarMi())
            {
                TempData["Hata"] = "Randevu düzenleme yetkiniz yok.";
                return RedirectToAction("Index");
            }

            Randevu? randevu = _context.Randevular
                .Include(r => r.Doktor)
                .FirstOrDefault(r => r.RandevuId == id && r.AktifMi);

            if (randevu == null)
            {
                TempData["Hata"] = "Randevu bulunamadı.";
                return RedirectToAction("Index");
            }

            if (DoktorMu())
            {
                int? aktifDoktorId = AktifDoktorId();

                if (!aktifDoktorId.HasValue || randevu.DoktorId != aktifDoktorId.Value)
                {
                    TempData["Hata"] = "Başka doktora ait randevuyu düzenleyemezsiniz.";
                    return RedirectToAction("Index");
                }
            }

            SecimListeleriniHazirla(randevu.PoliklinikId, randevu.DoktorId, randevu.HastaId);

            ViewBag.RandevuTarihi = randevu.RandevuTarihiSaati.ToString("yyyy-MM-dd");
            ViewBag.RandevuSaati = randevu.RandevuTarihiSaati.ToString("HH:mm");
            ViewBag.Saatler = RandevuSaatleriniGetir();
            ViewBag.DoktorMu = DoktorMu();

            return View(randevu);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Randevu randevu, string randevuTarihi, string randevuSaati)
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction("Login", "Account");
            }

            if (!RandevuIslemiYetkisiVarMi())
            {
                TempData["Hata"] = "Randevu düzenleme yetkiniz yok.";
                return RedirectToAction("Index");
            }

            Randevu? guncellenecekRandevu = _context.Randevular
                .FirstOrDefault(r => r.RandevuId == randevu.RandevuId && r.AktifMi);

            if (guncellenecekRandevu == null)
            {
                TempData["Hata"] = "Randevu bulunamadı.";
                return RedirectToAction("Index");
            }

            if (DoktorMu())
            {
                int? aktifDoktorId = AktifDoktorId();

                if (!aktifDoktorId.HasValue || guncellenecekRandevu.DoktorId != aktifDoktorId.Value)
                {
                    TempData["Hata"] = "Başka doktora ait randevuyu düzenleyemezsiniz.";
                    return RedirectToAction("Index");
                }

                Doktor? doktor = _context.Doktorlar.FirstOrDefault(d => d.DoktorId == aktifDoktorId.Value);

                if (doktor != null)
                {
                    randevu.DoktorId = doktor.DoktorId;
                    randevu.PoliklinikId = doktor.PoliklinikId;
                }
            }

            if (string.IsNullOrWhiteSpace(randevuTarihi))
            {
                ModelState.AddModelError("RandevuTarihiSaati", "Randevu tarihi seçiniz.");
            }

            if (string.IsNullOrWhiteSpace(randevuSaati))
            {
                ModelState.AddModelError("RandevuTarihiSaati", "Randevu saati seçiniz.");
            }

            if (!string.IsNullOrWhiteSpace(randevuTarihi) && !string.IsNullOrWhiteSpace(randevuSaati))
            {
                bool tarihSaatOlustuMu = DateTime.TryParse($"{randevuTarihi} {randevuSaati}", out DateTime olusanTarihSaat);

                if (tarihSaatOlustuMu)
                {
                    randevu.RandevuTarihiSaati = ValidationHelper.SaniyeVeSaliseyiTemizle(olusanTarihSaat);
                }
                else
                {
                    ModelState.AddModelError("RandevuTarihiSaati", "Randevu tarihi veya saati geçersiz.");
                }
            }

            if (randevu.HastaId <= 0)
            {
                ModelState.AddModelError("HastaId", "Hasta seçiniz.");
            }

            if (randevu.PoliklinikId <= 0)
            {
                ModelState.AddModelError("PoliklinikId", "Poliklinik seçiniz.");
            }

            if (randevu.DoktorId <= 0)
            {
                ModelState.AddModelError("DoktorId", "Doktor seçiniz.");
            }

            bool doktorPoliklinikUyumluMu = _context.Doktorlar.Any(d =>
                d.AktifMi &&
                d.DoktorId == randevu.DoktorId &&
                d.PoliklinikId == randevu.PoliklinikId);

            if (!doktorPoliklinikUyumluMu)
            {
                ModelState.AddModelError("DoktorId", "Seçilen doktor bu poliklinikte görevli değil.");
            }

            bool randevuCakismasiVarMi = _context.Randevular.Any(r =>
                r.AktifMi &&
                r.RandevuId != randevu.RandevuId &&
                r.DoktorId == randevu.DoktorId &&
                r.RandevuTarihiSaati == randevu.RandevuTarihiSaati &&
                r.Durum != "Iptal");

            if (randevuCakismasiVarMi)
            {
                ModelState.AddModelError("RandevuTarihiSaati", "Bu doktora aynı tarih ve saatte başka bir randevu kayıtlı.");
            }

            if (!ModelState.IsValid)
            {
                SecimListeleriniHazirla(randevu.PoliklinikId, randevu.DoktorId, randevu.HastaId);

                ViewBag.RandevuTarihi = randevuTarihi;
                ViewBag.RandevuSaati = randevuSaati;
                ViewBag.Saatler = RandevuSaatleriniGetir();
                ViewBag.DoktorMu = DoktorMu();

                return View(randevu);
            }

            guncellenecekRandevu.HastaId = randevu.HastaId;
            guncellenecekRandevu.DoktorId = randevu.DoktorId;
            guncellenecekRandevu.PoliklinikId = randevu.PoliklinikId;
            guncellenecekRandevu.RandevuTarihiSaati = randevu.RandevuTarihiSaati;
            guncellenecekRandevu.Durum = randevu.Durum;
            guncellenecekRandevu.Aciklama = randevu.Aciklama;

            _context.Randevular.Update(guncellenecekRandevu);
            _context.SaveChanges();

            TempData["Basari"] = "Randevu başarıyla güncellendi.";

            return RedirectToAction("Index");
        }

        public IActionResult Details(int id)
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction("Login", "Account");
            }

            Randevu? randevu = _context.Randevular
                .Include(r => r.Hasta)
                .Include(r => r.Doktor)
                .Include(r => r.Poliklinik)
                .FirstOrDefault(r => r.RandevuId == id && r.AktifMi);

            if (randevu == null)
            {
                TempData["Hata"] = "Randevu bulunamadı.";
                return RedirectToAction("Index");
            }

            if (DoktorMu())
            {
                int? aktifDoktorId = AktifDoktorId();

                if (!aktifDoktorId.HasValue || randevu.DoktorId != aktifDoktorId.Value)
                {
                    TempData["Hata"] = "Başka doktora ait randevu detayını görüntüleyemezsiniz.";
                    return RedirectToAction("Index");
                }
            }

            return View(randevu);
        }

        public IActionResult Delete(int id)
        {
            if (!GirisYapildiMi())
            {
                return RedirectToAction("Login", "Account");
            }

            if (!RandevuIslemiYetkisiVarMi())
            {
                TempData["Hata"] = "Randevu silme yetkiniz yok.";
                return RedirectToAction("Index");
            }

            Randevu? randevu = _context.Randevular.FirstOrDefault(r => r.RandevuId == id && r.AktifMi);

            if (randevu == null)
            {
                TempData["Hata"] = "Randevu bulunamadı.";
                return RedirectToAction("Index");
            }

            if (DoktorMu())
            {
                int? aktifDoktorId = AktifDoktorId();

                if (!aktifDoktorId.HasValue || randevu.DoktorId != aktifDoktorId.Value)
                {
                    TempData["Hata"] = "Başka doktora ait randevuyu silemezsiniz.";
                    return RedirectToAction("Index");
                }
            }

            randevu.AktifMi = false;

            _context.Randevular.Update(randevu);
            _context.SaveChanges();

            TempData["Basari"] = "Randevu başarıyla silindi.";

            return RedirectToAction("Index");
        }
    }
}