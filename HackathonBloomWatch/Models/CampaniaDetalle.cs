using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HackathonBloomWatch.Models
{
    public class CampaniaDetalle
    {
        [Key]
        public int IdCampaniaDetalle { get; set; }

        [Required(ErrorMessage = "La campania es requerida")]
        public int IdCampania { get; set; }
        [ForeignKey("IdCampania")]
        public virtual Campania Campania { get; set; }

        [Required(ErrorMessage = "El tipo de actividad es requerido")]
        [StringLength(15)]
        public string TipoActividad { get; set; } // "Forestación" o "Reforestación"

        [Required(ErrorMessage = "El estado de actividad es requerido")]
        [StringLength(15)]
        public string EstadoActividad { get; set; } // "Hoyada" o "Plantación"

        [DataType(DataType.Date)]
        public DateTime? FechaActividad { get; set; } // Puede ser Fecha de Plantación o Fecha de Hoyación, según el Estado

        [Required(ErrorMessage = "El macizo es requerido")]
        public int IdMacizoForestal { get; set; }
        [ForeignKey("IdMacizoForestal")]
        public virtual MacizoForestal MacizoForestal { get; set; }

        [Required(ErrorMessage = "La especie de la planta es requerida")]
        public int IdEspeciePlanta { get; set; }
        [ForeignKey("IdEspeciePlanta")]
        public virtual EspeciePlanta EspeciePlanta { get; set; }

        public int? CantidadElementos { get; set; } // Puede ser Cantidad de Plantas o Cantidad de Hoyos, según el Tipo

        public int? MortandadPlantas { get; set; }

        public decimal? ValorMacizoForestal { get; set; }

        public decimal? Agroforestal { get; set; }
    }
}
