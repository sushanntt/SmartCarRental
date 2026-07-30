using HajurKoCarRental.Areas.Identity.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HajurKoCarRental.Models
{
    public class RetentionOffer
    {
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("User")]
        public string UserId { get; set; }
        public virtual HajurKoCarRentalUser User { get; set; }

        [Required]
        public string OfferType { get; set; }

        [Required]
        public string DiscountCode { get; set; }

        public DateTime SentAt { get; set; }

        public DateTime? UsedAt { get; set; }

        [Required]
        public string Email { get; set; }

        public string? CustomMessage { get; set; }

        public int DiscountPercent { get; set; }
    }
}
