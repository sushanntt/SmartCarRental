using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel;
using HajurKoCarRental.Areas.Identity.Data;

namespace HajurKoCarRental.Models
{
    public class Payment
    {
        public Guid Id { get; set; }

        public int Amount { get; set; }

        [Required]
        [ForeignKey("Car")]
        [DisplayName("ModelName")]
        public Guid CarId { get; set; }
        public virtual Car Car { get; set; }

        [Required]
        [ForeignKey("User")]
        public string UserId { get; set; }
        public virtual HajurKoCarRentalUser User { get; set; }
    }
}
