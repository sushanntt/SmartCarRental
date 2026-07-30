using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;
using HajurKoCarRental.Areas.Identity.Data;

namespace HajurKoCarRental.Models
{
    public class CarDamage
    {
        public Guid Id { get; set; }
        public string DamageDescription { get; set; }
        [Required]
        [ForeignKey("User")]
        public string? UserId { get; set; }
        public virtual HajurKoCarRentalUser User { get; set; }

        [Required]
        [ForeignKey("Car")]
        public Guid? CarId { get; set; }
        public virtual Car Car { get; set; }

        public string? PhotoUrl { get; set; }
        [NotMapped]
        public IFormFile Photo { get; set; }
        public bool DamagePaid { get; set; }
        public double? DamageCharge { get; set; }

        public bool CustomerSeen { get; set; } = true;
        public bool AdminSeen { get; set; } = true;
    }
}
