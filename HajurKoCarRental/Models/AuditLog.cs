using HajurKoCarRental.Areas.Identity.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HajurKoCarRental.Models
{
    public class AuditLog
    {
        public Guid Id { get; set; }

        public string? UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual HajurKoCarRentalUser? User { get; set; }

        [Required]
        public string Action { get; set; }

        [Required]
        public string EntityType { get; set; }

        public string EntityId { get; set; }

        public string Details { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.Now;
    }
}
