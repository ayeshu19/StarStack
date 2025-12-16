using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShiftSync.Api.Models
{
    [Table("loads")]
    public class Load
    {
        [Key]
        [Column("load_id")]
        public int LoadId { get; set; }

        [Required]
        [Column("load_ref")]
        public string LoadRef { get; set; } = string.Empty;

        [Required]
        [Column("region")]
        public string Region { get; set; } = string.Empty;

        [Column("stops")]
        public int Stops { get; set; }

        [Column("estimated_hours")]
        public decimal EstimatedHours { get; set; }

        [Column("estimated_distance")]
        public decimal EstimatedDistance { get; set; }

        [Column("priority")]
        public string Priority { get; set; } = "MEDIUM"; // HIGH/MEDIUM/LOW

        [Column("status")]
        public string Status { get; set; } = "PENDING"; // PENDING/ASSIGNED/IN_PROGRESS/COMPLETED

        [Column("assigned_driver_id")]
        public int? AssignedDriverId { get; set; } // Nullable FK

        // If you already have Driver model:
        [ForeignKey("AssignedDriverId")]
        public Driver? AssignedDriver { get; set; }

        [Column("assigned_at")]
        public DateTime? AssignedAt { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
