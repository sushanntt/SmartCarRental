using HajurKoCarRental.Areas.Identity.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HajurKoCarRental.Models
{
    public class RentalHistory
    {
        public Guid Id { get; set; }

        [ForeignKey("User")]
        public string? UserId { get; set; }
        public virtual HajurKoCarRentalUser? User { get; set; }

        [Required]
        [ForeignKey("Car")]
        public Guid carId { get; set; }
        public virtual Car Car { get; set; }

        public string? AuthorizedBy { get; set; }  
        
        public Boolean Status { get; set; } 
        public string? ActionType { get; set; }
        public DateTime? RentedDate { get; set; } 

        public int? Duration { get; set; }

        public int Hours { get; set; } = 0;

        public bool Seen { get; set; } = false;

        public bool AdminSeen { get; set; } = true;

        public double Amount { get; set; }

        public double AdvancePaid { get; set; }

        public double RemainingAmount { get; set; }

        public string? PaymentMethod { get; set; }

        [ForeignKey("PickupBranch")]
        public Guid? PickupBranchId { get; set; }
        public virtual Branch? PickupBranch { get; set; }
    }
}
