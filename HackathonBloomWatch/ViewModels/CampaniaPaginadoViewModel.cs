using HackathonBloomWatch.Models;

namespace HackathonBloomWatch.ViewModels
{
    public class CampaniaPaginadoViewModel
    {
        public List<Campania> Campanias { get; set; }
        public int PaginaActual { get; set; }
        public int TotalPaginas { get; set; }
        public string? BusquedaAnio { get; set; }
    }
}
