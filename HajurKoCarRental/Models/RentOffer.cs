using HajurKoCarRental.Areas.Identity.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HajurKoCarRental.Models
{
    public class RentOffer
    {
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("Car")]
        public Guid OfferId { get; set; }
        public virtual Offer Offer { get; set; }

        [Required]
        [ForeignKey("User")]
        public string UserId { get; set; }
        public virtual HajurKoCarRentalUser User { get; set; }

        [Required]
        public string? PaymentMethod { get; set; }

        public double Amount { get; set; }

        public double AdvancePaid { get; set; }

        public double RemainingAmount { get; set; }

        public bool? Verified { get; set; }

        public DateTime RentalDate { get; set; }

        [Required]
        public int Duration { get; set; } = 1;

        public bool CustomerSeen { get; set; } = true;
        public bool AdminSeen { get; set; } = true;

        public string? DiscountCode { get; set; }

        [ForeignKey("PickupBranch")]
        public Guid? PickupBranchId { get; set; }
        public virtual Branch? PickupBranch { get; set; }

        public double? PickupLat { get; set; }
        public double? PickupLng { get; set; }

        public string? Status { get; set; }
        public DateTime RequestDate { get; set; } = DateTime.Now;
    }
}
