using HackathonBloomWatch.Models;

namespace HackathonBloomWatch.ViewModels
{
    public class MacizoPaginadoViewModel
    {
        public List<MacizoForestal> Macizos { get; set; }
        public int PaginaActual { get; set; }
        public int TotalPaginas { get; set; }
        public string? BusquedaProvincia { get; set; }
        public string? BusquedaDistrito { get; set; }
        public string? BusquedaLocalidad { get; set; }
    }
}
