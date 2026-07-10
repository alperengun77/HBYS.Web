using System.ComponentModel.DataAnnotations;

namespace HBYS.Web.Models
{
    public class IlacKullanimSuresi
    {
        public int IlacKullanimSuresiId { get; set; }

        [Required]
        [StringLength(100)]
        public string KullanimSuresiAdi { get; set; } = string.Empty;

        public bool AktifMi { get; set; } = true;
    }
}