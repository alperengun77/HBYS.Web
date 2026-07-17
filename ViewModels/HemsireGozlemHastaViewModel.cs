using HBYS.Web.Models;

namespace HBYS.Web.ViewModels
{
    public class HemsireGozlemHastaViewModel
    {
        public HastaYatis HastaYatis { get; set; } =
            new HastaYatis();

        public HemsireGozlem? SonGozlem { get; set; }

        public int SeciliGunGozlemSayisi { get; set; }
    }
}