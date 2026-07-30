using HajurKoCarRental.Areas.Identity.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HajurKoCarRental.Models
{
    public class ChatMessage
    {
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("Sender")]
        public string SenderId { get; set; }
        public virtual HajurKoCarRentalUser Sender { get; set; }

        [Required]
        [ForeignKey("Receiver")]
        public string ReceiverId { get; set; }
        public virtual HajurKoCarRentalUser Receiver { get; set; }

        [Required]
        public string Message { get; set; }

        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        public bool IsRead { get; set; } = false;
    }
}
