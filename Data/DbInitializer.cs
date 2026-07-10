using HBYS.Web.Models;
using Microsoft.EntityFrameworkCore;

namespace HBYS.Web.Data
{
    public static class DbInitializer
    {
        public static void Initialize(AppDbContext context)
        {
            context.Database.EnsureCreated();

            LogTablosunuGuncelle(context);
            IlacTablolariniOlustur(context);

            RolleriOlustur(context);
            PoliklinikleriOlustur(context);
            IlacVerileriniOlustur(context);
            AdminKullanicisiniOlustur(context);
        }

        private static bool KolonVarMi(AppDbContext context, string tabloAdi, string kolonAdi)
        {
            var connection = context.Database.GetDbConnection();

            if (connection.State != System.Data.ConnectionState.Open)
            {
                connection.Open();
            }

            using var command = connection.CreateCommand();
            command.CommandText = $"PRAGMA table_info({tabloAdi});";

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                string? mevcutKolonAdi = reader["name"]?.ToString();

                if (mevcutKolonAdi == kolonAdi)
                {
                    return true;
                }
            }

            return false;
        }

        private static void LogTablosunuGuncelle(AppDbContext context)
        {
            context.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS LogKayitlari (
                    LogKaydiId INTEGER NOT NULL CONSTRAINT PK_LogKayitlari PRIMARY KEY AUTOINCREMENT,
                    KullaniciAdi TEXT NOT NULL,
                    Modul TEXT NOT NULL DEFAULT 'Sistem',
                    IslemTipi TEXT NOT NULL DEFAULT 'Bilgi',
                    Islem TEXT NOT NULL,
                    Aciklama TEXT NULL,
                    IpAdresi TEXT NULL,
                    Tarih TEXT NOT NULL
                );
            ");

            if (!KolonVarMi(context, "LogKayitlari", "Modul"))
            {
                context.Database.ExecuteSqlRaw("ALTER TABLE LogKayitlari ADD COLUMN Modul TEXT NOT NULL DEFAULT 'Sistem';");
            }

            if (!KolonVarMi(context, "LogKayitlari", "IslemTipi"))
            {
                context.Database.ExecuteSqlRaw("ALTER TABLE LogKayitlari ADD COLUMN IslemTipi TEXT NOT NULL DEFAULT 'Bilgi';");
            }

            context.Database.ExecuteSqlRaw(@"
                CREATE INDEX IF NOT EXISTS IX_LogKayitlari_Modul 
                ON LogKayitlari (Modul);
            ");

            context.Database.ExecuteSqlRaw(@"
                CREATE INDEX IF NOT EXISTS IX_LogKayitlari_IslemTipi 
                ON LogKayitlari (IslemTipi);
            ");

            context.Database.ExecuteSqlRaw(@"
                CREATE INDEX IF NOT EXISTS IX_LogKayitlari_Tarih 
                ON LogKayitlari (Tarih);
            ");
        }

        private static void IlacTablolariniOlustur(AppDbContext context)
        {
            context.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS Ilaclar (
                    IlacId INTEGER NOT NULL CONSTRAINT PK_Ilaclar PRIMARY KEY AUTOINCREMENT,
                    IlacAdi TEXT NOT NULL,
                    Aciklama TEXT NULL,
                    AktifMi INTEGER NOT NULL
                );
            ");

            context.Database.ExecuteSqlRaw(@"
                CREATE UNIQUE INDEX IF NOT EXISTS IX_Ilaclar_IlacAdi 
                ON Ilaclar (IlacAdi);
            ");

            context.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS IlacDozlari (
                    IlacDozuId INTEGER NOT NULL CONSTRAINT PK_IlacDozlari PRIMARY KEY AUTOINCREMENT,
                    DozAdi TEXT NOT NULL,
                    AktifMi INTEGER NOT NULL
                );
            ");

            context.Database.ExecuteSqlRaw(@"
                CREATE UNIQUE INDEX IF NOT EXISTS IX_IlacDozlari_DozAdi 
                ON IlacDozlari (DozAdi);
            ");

            context.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS IlacKullanimSekilleri (
                    IlacKullanimSekliId INTEGER NOT NULL CONSTRAINT PK_IlacKullanimSekilleri PRIMARY KEY AUTOINCREMENT,
                    KullanimSekliAdi TEXT NOT NULL,
                    AktifMi INTEGER NOT NULL
                );
            ");

            context.Database.ExecuteSqlRaw(@"
                CREATE UNIQUE INDEX IF NOT EXISTS IX_IlacKullanimSekilleri_KullanimSekliAdi 
                ON IlacKullanimSekilleri (KullanimSekliAdi);
            ");

            context.Database.ExecuteSqlRaw(@"
                CREATE TABLE IF NOT EXISTS IlacKullanimSureleri (
                    IlacKullanimSuresiId INTEGER NOT NULL CONSTRAINT PK_IlacKullanimSureleri PRIMARY KEY AUTOINCREMENT,
                    KullanimSuresiAdi TEXT NOT NULL,
                    AktifMi INTEGER NOT NULL
                );
            ");

            context.Database.ExecuteSqlRaw(@"
                CREATE UNIQUE INDEX IF NOT EXISTS IX_IlacKullanimSureleri_KullanimSuresiAdi 
                ON IlacKullanimSureleri (KullanimSuresiAdi);
            ");
        }

        private static void RolleriOlustur(AppDbContext context)
        {
            List<Rol> roller = new List<Rol>
            {
                new Rol { RolAdi = "Admin", Aciklama = "Sistem yöneticisi", AktifMi = true },
                new Rol { RolAdi = "Doktor", Aciklama = "Doktor kullanıcısı", AktifMi = true },
                new Rol { RolAdi = "Sekreter", Aciklama = "Sekreter kullanıcısı", AktifMi = true },
                new Rol { RolAdi = "Laborant", Aciklama = "Laboratuvar kullanıcısı", AktifMi = true },
                new Rol { RolAdi = "Eczane", Aciklama = "Eczane kullanıcısı", AktifMi = true },
                new Rol { RolAdi = "Yonetici", Aciklama = "Yönetici kullanıcısı", AktifMi = true }
            };

            foreach (Rol rol in roller)
            {
                bool rolVarMi = context.Roller.Any(r => r.RolAdi == rol.RolAdi);

                if (!rolVarMi)
                {
                    context.Roller.Add(rol);
                }
            }

            context.SaveChanges();
        }

        private static void PoliklinikleriOlustur(AppDbContext context)
        {
            List<string> poliklinikler = new List<string>
            {
                "Acil Servis",
                "Dahiliye",
                "Kardiyoloji",
                "Ortopedi",
                "Göz Hastalıkları",
                "Kulak Burun Boğaz",
                "Nöroloji",
                "Genel Cerrahi",
                "Çocuk Sağlığı ve Hastalıkları",
                "Kadın Hastalıkları ve Doğum",
                "Üroloji",
                "Dermatoloji",
                "Psikiyatri",
                "Fizik Tedavi ve Rehabilitasyon",
                "Radyoloji",
                "Enfeksiyon Hastalıkları",
                "Göğüs Hastalıkları",
                "Endokrinoloji",
                "Gastroenteroloji",
                "Beyin ve Sinir Cerrahisi",
                "Kalp ve Damar Cerrahisi",
                "Plastik Cerrahi",
                "Anestezi ve Reanimasyon",
                "Beslenme ve Diyetetik",
                "Ağız ve Diş Sağlığı",
                "Onkoloji",
                "Hematoloji",
                "Nefroloji",
                "Romatoloji",
                "Alerji ve İmmünoloji"
            };

            foreach (string poliklinikAdi in poliklinikler)
            {
                bool poliklinikVarMi = context.Poliklinikler
                    .Any(p => p.PoliklinikAdi == poliklinikAdi);

                if (!poliklinikVarMi)
                {
                    context.Poliklinikler.Add(new Poliklinik
                    {
                        PoliklinikAdi = poliklinikAdi,
                        Aciklama = poliklinikAdi + " bölümü",
                        AktifMi = true
                    });
                }
            }

            context.SaveChanges();
        }

        private static void IlacVerileriniOlustur(AppDbContext context)
        {
            List<string> ilaclar = new List<string>
            {
                "Parol 500 mg",
                "Aferin Forte",
                "Augmentin 1000 mg",
                "Majezik",
                "Dolorex",
                "Arveles",
                "Nurofen",
                "Minoset",
                "Katarin",
                "Vermidon",
                "Apranax",
                "Cipro",
                "Amoklavin",
                "Ventolin",
                "Benical"
            };

            foreach (string ilacAdi in ilaclar)
            {
                bool varMi = context.Ilaclar.Any(i => i.IlacAdi == ilacAdi);

                if (!varMi)
                {
                    context.Ilaclar.Add(new Ilac
                    {
                        IlacAdi = ilacAdi,
                        Aciklama = ilacAdi + " ilacı",
                        AktifMi = true
                    });
                }
            }

            List<string> dozlar = new List<string>
            {
                "Yarım tablet",
                "1 tablet",
                "2 tablet",
                "1 kapsül",
                "2 kapsül",
                "1 ölçek",
                "2 ölçek",
                "5 ml",
                "10 ml",
                "1 ampul",
                "1 damla",
                "2 damla"
            };

            foreach (string dozAdi in dozlar)
            {
                bool varMi = context.IlacDozlari.Any(d => d.DozAdi == dozAdi);

                if (!varMi)
                {
                    context.IlacDozlari.Add(new IlacDozu
                    {
                        DozAdi = dozAdi,
                        AktifMi = true
                    });
                }
            }

            List<string> kullanimSekilleri = new List<string>
            {
                "Günde 1 kez",
                "Günde 2 kez",
                "Günde 3 kez",
                "Sabah",
                "Öğle",
                "Akşam",
                "Sabah-akşam",
                "Tok karnına",
                "Aç karnına",
                "Yatmadan önce",
                "Ağrı oldukça",
                "Doktor önerisine göre"
            };

            foreach (string kullanimSekliAdi in kullanimSekilleri)
            {
                bool varMi = context.IlacKullanimSekilleri.Any(k => k.KullanimSekliAdi == kullanimSekliAdi);

                if (!varMi)
                {
                    context.IlacKullanimSekilleri.Add(new IlacKullanimSekli
                    {
                        KullanimSekliAdi = kullanimSekliAdi,
                        AktifMi = true
                    });
                }
            }

            List<string> kullanimSureleri = new List<string>
            {
                "1 gün",
                "3 gün",
                "5 gün",
                "7 gün",
                "10 gün",
                "14 gün",
                "15 gün",
                "1 ay",
                "2 ay",
                "3 ay",
                "Kontrole kadar",
                "Doktor önerisine göre"
            };

            foreach (string kullanimSuresiAdi in kullanimSureleri)
            {
                bool varMi = context.IlacKullanimSureleri.Any(s => s.KullanimSuresiAdi == kullanimSuresiAdi);

                if (!varMi)
                {
                    context.IlacKullanimSureleri.Add(new IlacKullanimSuresi
                    {
                        KullanimSuresiAdi = kullanimSuresiAdi,
                        AktifMi = true
                    });
                }
            }

            context.SaveChanges();
        }

        private static void AdminKullanicisiniOlustur(AppDbContext context)
        {
            bool adminVarMi = context.Kullanicilar.Any(k => k.KullaniciAdi == "admin");

            if (adminVarMi)
            {
                return;
            }

            Rol? adminRol = context.Roller.FirstOrDefault(r => r.RolAdi == "Admin");

            if (adminRol == null)
            {
                return;
            }

            Kullanici admin = new Kullanici
            {
                KullaniciAdi = "admin",
                Sifre = "1234",
                AdSoyad = "Sistem Yöneticisi",
                Eposta = "admin@hbys.local",
                RolId = adminRol.RolId,
                AktifMi = true,
                KayitTarihi = DateTime.Now
            };

            context.Kullanicilar.Add(admin);
            context.SaveChanges();
        }
    }
}