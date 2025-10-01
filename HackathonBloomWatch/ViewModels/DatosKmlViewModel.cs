using HackathonBloomWatch.Models;

namespace HackathonBloomWatch.ViewModels
{
    public class DatosKmlViewModel
    {
        public Campania Campania { get; set; }
        public List<EspeciePlantaKml> Especies { get; set; }
        public List<MacizoKml> Macizos { get; set; }
        public List<CampaniaDetalleKml> CampaniaDetalles { get; set; }
    }

    public class EspeciePlantaKml : EspeciePlanta
    {
        public string ClaveEspecie { get; set; }
        public bool ExisteEspecie { get; set; }
    }

    public class MacizoKml : MacizoForestal
    {
        public string ClaveMacizo { get; set; }
        public string GeoJson { get; set; }
        public bool ExisteMacizo { get; set; }
    }

    public class CampaniaDetalleKml : CampaniaDetalle
    {
        public string ClaveEspecie { get; set; }
        public string ClaveMacizo { get; set; }
    }
}
