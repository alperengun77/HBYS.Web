using System.ComponentModel.DataAnnotations;

namespace HBYS.Web.Models
{
    public class IlacDozu
    {
        public int IlacDozuId { get; set; }

        [Required]
        [StringLength(100)]
        public string DozAdi { get; set; } = string.Empty;

        public bool AktifMi { get; set; } = true;
    }
}