using HajurKoCarRental.Areas.Identity.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HajurKoCarRental.Models
{
    public class Rent
    {
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("Car")]
        public Guid CarId { get; set; }
        public virtual Car Car { get; set; }

        [Required]
        [ForeignKey("User")]
        public string UserId { get; set; }
        public virtual HajurKoCarRentalUser User { get; set; }

        [Required]
        public string? PaymentMethod { get; set; }

        public double Amount { get; set; }

        public double AdvancePaid { get; set; }

        public double RemainingAmount { get; set; }

        [Required]
        public bool Verified { get; set; }
        public string? Status { get; set; } = "Pending";
        public DateTime RequestDate { get; set; }
        public DateTime RentalDate { get; set; } 

        [Required]
        public int Duration { get; set; } = 1;

        public int Hours { get; set; } = 0;

        public bool CustomerSeen { get; set; } = true;
        public bool AdminSeen { get; set; } = true;
        public bool ReturnSeen { get; set; } = true;
        public string? DiscountCode { get; set; }

        [ForeignKey("PickupBranch")]
        public Guid? PickupBranchId { get; set; }
        public virtual Branch? PickupBranch { get; set; }

        [ForeignKey("DropOffBranch")]
        public Guid? DropOffBranchId { get; set; }
        public virtual Branch? DropOffBranch { get; set; }

        public double? PickupLat { get; set; }
        public double? PickupLng { get; set; }
    }
}