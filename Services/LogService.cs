using HBYS.Web.Data;
using HBYS.Web.Models;

namespace HBYS.Web.Services
{
    public class LogService
    {
        private readonly AppDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public LogService(AppDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public void Logla(
            string modul,
            string islemTipi,
            string islem,
            string? aciklama = null,
            string? kullaniciAdi = null)
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;

                string? sessionKullaniciAdi = httpContext?.Session.GetString("KullaniciAdi");

                LogKaydi log = new LogKaydi
                {
                    KullaniciAdi = kullaniciAdi ?? sessionKullaniciAdi ?? "Sistem",
                    Modul = modul,
                    IslemTipi = islemTipi,
                    Islem = islem,
                    Aciklama = aciklama,
                    IpAdresi = httpContext?.Connection.RemoteIpAddress?.ToString(),
                    Tarih = DateTime.Now
                };

                _context.LogKayitlari.Add(log);
                _context.SaveChanges();
            }
            catch
            {
                // Loglama hata verirse ana işlem bozulmasın diye boş bırakıldı.
            }
        }
    }
}