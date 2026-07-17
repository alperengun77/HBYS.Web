namespace HBYS.Web.Helpers
{
    public static class ValidationHelper
    {
        public static bool SadeceRakamMi(string? deger)
        {
            if (string.IsNullOrWhiteSpace(deger))
            {
                return false;
            }

            return deger.All(char.IsDigit);
        }

        public static bool TcKimlikNoGecerliMi(string? tcKimlikNo)
        {
            if (string.IsNullOrWhiteSpace(tcKimlikNo))
            {
                return false;
            }

            tcKimlikNo = tcKimlikNo.Trim();

            // TC kimlik numarası 11 haneli olmalıdır.
            if (tcKimlikNo.Length != 11)
            {
                return false;
            }

            // Bütün karakterler rakam olmalıdır.
            if (!tcKimlikNo.All(char.IsDigit))
            {
                return false;
            }

            int[] rakamlar = tcKimlikNo
                .Select(karakter => karakter - '0')
                .ToArray();

            // TC kimlik numarasının ilk hanesi 0 olamaz.
            if (rakamlar[0] == 0)
            {
                return false;
            }

            /*
                1, 3, 5, 7 ve 9. hanelerin toplamı.

                Diziler 0'dan başladığı için:
                1. hane = rakamlar[0]
                3. hane = rakamlar[2]
                5. hane = rakamlar[4]
                7. hane = rakamlar[6]
                9. hane = rakamlar[8]
            */
            int tekHanelerToplami =
                rakamlar[0] +
                rakamlar[2] +
                rakamlar[4] +
                rakamlar[6] +
                rakamlar[8];

            /*
                2, 4, 6 ve 8. hanelerin toplamı.
            */
            int ciftHanelerToplami =
                rakamlar[1] +
                rakamlar[3] +
                rakamlar[5] +
                rakamlar[7];

            /*
                10. hane hesaplanır.

                C# dilinde negatif bir sayının % işlemi
                negatif sonuç verebildiği için değer
                tekrar pozitif Mod 10 aralığına alınır.
            */
            int hesaplananOnuncuHane =
                (
                    (
                        tekHanelerToplami * 7 -
                        ciftHanelerToplami
                    ) % 10 + 10
                ) % 10;

            if (rakamlar[9] != hesaplananOnuncuHane)
            {
                return false;
            }

            /*
                İlk 10 hanenin toplamının
                10'a bölümünden kalan 11. haneyi verir.
            */
            int ilkOnHaneToplami = 0;

            for (int i = 0; i < 10; i++)
            {
                ilkOnHaneToplami += rakamlar[i];
            }

            int hesaplananOnBirinciHane =
                ilkOnHaneToplami % 10;

            if (rakamlar[10] != hesaplananOnBirinciHane)
            {
                return false;
            }

            return true;
        }

        public static bool TelefonGecerliMi(string? telefon)
        {
            if (string.IsNullOrWhiteSpace(telefon))
            {
                return true;
            }

            bool sadeceRakam =
                telefon.All(char.IsDigit);

            bool uzunlukUygun =
                telefon.Length == 10 ||
                telefon.Length == 11;

            return sadeceRakam && uzunlukUygun;
        }

        public static bool DogumTarihiGecerliMi(
            DateTime dogumTarihi
        )
        {
            if (dogumTarihi == default)
            {
                return false;
            }

            return dogumTarihi.Date <= DateTime.Today;
        }

        public static bool RandevuSaatiGecerliMi(
            DateTime tarih
        )
        {
            bool saniyeYok =
                tarih.Second == 0;

            bool saliseYok =
                tarih.Millisecond == 0;

            bool onBesDakikaAraligi =
                tarih.Minute % 15 == 0;

            return saniyeYok &&
                   saliseYok &&
                   onBesDakikaAraligi;
        }

        public static DateTime SaniyeVeSaliseyiTemizle(
            DateTime tarih
        )
        {
            return new DateTime(
                tarih.Year,
                tarih.Month,
                tarih.Day,
                tarih.Hour,
                tarih.Minute,
                0
            );
        }
    }
}