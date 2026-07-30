using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace HajurKoCarRental.Models
{
    public class Offer
    {
        public Guid Id { get; set; }
        public string OfferTitle { get; set; }

        public string OfferDescription { get; set;}

        public string CarName { get; set; }

        [System.ComponentModel.DataAnnotations.Range(0, 100, ErrorMessage = "Discount percentage must be between 0 and 100.")]
        public int DiscountPercentage { get; set;}

        public int Price { get; set; }

        public DateTime ValidityPeroid { get; set; }

        public string PhotoUrl { get; set; }

        [NotMapped]
        public IFormFile Photo { get; set; }

        public bool Available { get; set; }

        public bool IsBooked { get; set; }

        public bool ApprovedStatus { get; set; }

        public virtual ICollection<OfferBranch> OfferBranches { get; set; } = new List<OfferBranch>();
    }
}
