using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ShiftSync.Api.Models
{
    [Table("attendances")]
    public class Attendance
    {
        [Key]
        [Column("attendance_id")]
        public int AttendanceId { get; set; }

        [Column("driver_id")]
        public int DriverId { get; set; }

        [ForeignKey("DriverId")]
        public Driver? Driver { get; set; }

        [Column("date")]
        public DateTime Date { get; set; }

        [Column("check_in_time")]
        public DateTime? CheckInTime { get; set; }

        [Column("check_out_time")]
        public DateTime? CheckOutTime { get; set; }

        [Column("total_hours")]
        public decimal TotalHours { get; set; } = 0;

        [Column("is_absent")]
        public bool IsAbsent { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
