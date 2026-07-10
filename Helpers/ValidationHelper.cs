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

            return tcKimlikNo.Length == 11 && tcKimlikNo.All(char.IsDigit);
        }

        public static bool TelefonGecerliMi(string? telefon)
        {
            if (string.IsNullOrWhiteSpace(telefon))
            {
                return true;
            }

            bool sadeceRakam = telefon.All(char.IsDigit);
            bool uzunlukUygun = telefon.Length == 10 || telefon.Length == 11;

            return sadeceRakam && uzunlukUygun;
        }

        public static bool RandevuSaatiGecerliMi(DateTime tarih)
        {
            bool saniyeYok = tarih.Second == 0;
            bool saliseYok = tarih.Millisecond == 0;
            bool onBesDakikaAraligi = tarih.Minute % 15 == 0;

            return saniyeYok && saliseYok && onBesDakikaAraligi;
        }

        public static DateTime SaniyeVeSaliseyiTemizle(DateTime tarih)
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