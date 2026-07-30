using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HajurKoCarRental.Models
{
    public class OfferBranch
    {
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("Offer")]
        public Guid OfferId { get; set; }
        public virtual Offer Offer { get; set; }

        [Required]
        [ForeignKey("Branch")]
        public Guid BranchId { get; set; }
        public virtual Branch Branch { get; set; }

        public int AvailableCount { get; set; } = 1;
    }
}
