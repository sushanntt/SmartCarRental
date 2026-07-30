using HajurKoCarRental.Areas.Identity.Data;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace HajurKoCarRental.Models
{
    public class CarTracking
    {
        public int Id { get; set; }

        [Required]
        public Guid RentId { get; set; }

        [Required]
        [ForeignKey("User")]
        public string UserId { get; set; }
        public virtual HajurKoCarRentalUser User { get; set; }

        public bool IsActive { get; set; } = false;
        public bool StopRequested { get; set; } = false;
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
    }

    public class TrackingPoint
    {
        public int Id { get; set; }

        [Required]
        public int CarTrackingId { get; set; }
        public virtual CarTracking CarTracking { get; set; }

        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
