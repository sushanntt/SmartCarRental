using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HajurKoCarRental.Models
{
    public class CarBranch
    {
        public Guid Id { get; set; }

        [Required]
        [ForeignKey("Car")]
        public Guid CarId { get; set; }
        public virtual Car Car { get; set; }

        [Required]
        [ForeignKey("Branch")]
        public Guid BranchId { get; set; }
        public virtual Branch Branch { get; set; }

        public int AvailableCount { get; set; } = 1;
    }
}
