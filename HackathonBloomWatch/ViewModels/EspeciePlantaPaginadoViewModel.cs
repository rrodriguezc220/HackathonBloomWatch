using HackathonBloomWatch.Models;

namespace HackathonBloomWatch.ViewModels
{
    public class EspeciePlantaPaginadoViewModel
    {
        public List<EspeciePlanta> EspeciePlantas { get; set; }
        public int PaginaActual { get; set; }
        public int TotalPaginas { get; set; }
        public string? BusquedaNombreEspecie { get; set; }
    }
}
