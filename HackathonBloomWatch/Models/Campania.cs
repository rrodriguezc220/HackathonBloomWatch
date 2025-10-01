using System.ComponentModel.DataAnnotations;

namespace HackathonBloomWatch.Models
{
    public class Campania
    {
        [Key]
        public int IdCampania { get; set; }

        [Required(ErrorMessage = "El nombre de la camapaña es requerido")]
        [StringLength(50)]
        public string NombreCampania { get; set; }

        [Required(ErrorMessage = "El año de la campaña es requerido")]
        [StringLength(15)]
        public string AnioCampania { get; set; }

        [Required(ErrorMessage = "La fecha de proceso de la campaña es requerido")]
        [DataType(DataType.Date)]
        public DateTime FechaProceso { get; set; }

        public virtual ICollection<CampaniaDetalle> CampaniaDetalles { get; set; }
    }
}
