using System.ComponentModel.DataAnnotations;

namespace HajurKoCarRental.Models
{
    public class Branch
    {
        public Guid Id { get; set; }

        [Required]
        public string Name { get; set; }

        public string? Address { get; set; }

        public string Contact { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }

        public virtual ICollection<CarBranch> CarBranches { get; set; } = new List<CarBranch>();
        public virtual ICollection<OfferBranch> OfferBranches { get; set; } = new List<OfferBranch>();
    }
}
