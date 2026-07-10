using System.ComponentModel.DataAnnotations;

namespace HBYS.Web.Models
{
    public class Ilac
    {
        public int IlacId { get; set; }

        [Required]
        [StringLength(150)]
        public string IlacAdi { get; set; } = string.Empty;

        [StringLength(250)]
        public string? Aciklama { get; set; }

        public bool AktifMi { get; set; } = true;
    }
}
