using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations.Schema;

namespace HajurKoCarRental.Models
{
    public class Car
    {
        public Guid Id { get; set; }
        public string ModelName { get; set; }

        [Required]
        [ForeignKey("CarBrand")]
        [DisplayName("BrandName")]
        public Guid BrandId { get; set; }
        public virtual CarBrand CarBrand { get; set; }


        public int ModelYear { get; set; }

        public int TotalSeats { get; set; }

        public string EngineType { get; set; }

        public int Price { get; set; }

        public string CarDescription { get; set; }
        public string PhotoUrl { get; set; }

        [NotMapped]
        public IFormFile Photo { get; set; }


        public bool Available { get; set; }

        public string CarNumber { get; set; }
        public bool? IsCarBooked { get; set; }
        public int? BookedCount { get; set; }

        [DisplayName("Number of Available Cars")]
        public int AvailableCarCount { get; set; } = 1;

        public virtual ICollection<CarBranch> CarBranches { get; set; } = new List<CarBranch>();
    }
}