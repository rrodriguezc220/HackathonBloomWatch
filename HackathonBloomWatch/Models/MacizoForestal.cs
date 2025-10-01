using NetTopologySuite.Geometries;
using System.ComponentModel.DataAnnotations;

namespace HackathonBloomWatch.Models
{
    public class MacizoForestal
    {
        [Key]
        public int IdMacizoForestal { get; set; }

        [StringLength(20)]
        public string Departamento { get; set; }

        [StringLength(35)]
        public string Provincia { get; set; }

        [StringLength(35)]
        public string Distrito { get; set; }

        [StringLength(35)]
        public string Localidad { get; set; }

        public decimal? AreaHectareas { get; set; }

        public decimal? CoordenadaEste { get; set; }

        public decimal? CoordenadaNorte { get; set; }

        public Geometry Geometria { get; set; }

        public virtual ICollection<CampaniaDetalle> CampaniaDetalles { get; set; }
    }
}
