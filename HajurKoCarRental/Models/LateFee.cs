using HajurKoCarRental.Areas.Identity.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HajurKoCarRental.Models
{
    public class LateFee
    {
        public Guid Id { get; set; }

        public Guid RentId { get; set; }

        public Guid? CarId { get; set; }
        [ForeignKey("CarId")]
        public virtual Car? Car { get; set; }

        [Required]
        [ForeignKey("User")]
        public string UserId { get; set; }
        public virtual HajurKoCarRentalUser User { get; set; }

        public double Amount { get; set; }
        public double HoursLate { get; set; }
        public string Status { get; set; } = "Unpaid";
        public bool CustomerConfirmed { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? PaidAt { get; set; }
    }
}
