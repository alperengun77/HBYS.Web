using System.ComponentModel.DataAnnotations;

namespace HBYS.Web.Models
{
    public class IlacKullanimSekli
    {
        public int IlacKullanimSekliId { get; set; }

        [Required]
        [StringLength(100)]
        public string KullanimSekliAdi { get; set; } = string.Empty;

        public bool AktifMi { get; set; } = true;
    }
}