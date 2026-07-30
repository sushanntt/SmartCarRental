using System.ComponentModel.DataAnnotations;

namespace HajurKoCarRental.Models
{
    public class Festival
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
        
        [Required]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }
        
        [Required]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }
        
        public decimal PriceMultiplier { get; set; }
        
        public bool IsActive { get; set; } = true;
    }
}
