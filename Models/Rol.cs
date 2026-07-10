using System.ComponentModel.DataAnnotations;

namespace HBYS.Web.Models
{
    public class Rol
    {
        public int RolId { get; set; }

        [Required]
        [StringLength(50)]
        public string RolAdi { get; set; } = string.Empty;

        [StringLength(250)]
        public string? Aciklama { get; set; }

        public bool AktifMi { get; set; } = true;

        public List<Kullanici> Kullanicilar { get; set; } = new List<Kullanici>();
    }
}