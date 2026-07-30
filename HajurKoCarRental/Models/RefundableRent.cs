using HajurKoCarRental.Areas.Identity.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HajurKoCarRental.Models
{
    public class RefundableRent
    {
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("User")]
        public string UserId { get; set; }
        public virtual HajurKoCarRentalUser User { get; set; }

        public int Amount { get; set; } 
        public bool? Status { get; set; }

        public bool CustomerConfirmed { get; set; } = false;

        public bool CustomerSeen { get; set; } = true;
        public bool AdminSeen { get; set; } = true;
    }
}
