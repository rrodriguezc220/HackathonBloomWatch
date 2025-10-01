using System.ComponentModel.DataAnnotations;

namespace HackathonBloomWatch.Models
{
    public class EspeciePlanta
    {
        [Key]
        public int IdEspeciePlanta { get; set; }

        [Required(ErrorMessage = "El nombre de la especie es requerido")]
        [StringLength(100)]
        public string NombreEspecie { get; set; }

        [StringLength(100)]
        public string? NombreComun { get; set; }

        public byte[]? ImagenEspecie { get; set; }

        public virtual ICollection<CampaniaDetalle> CampaniaDetalles { get; set; }
    }
}
