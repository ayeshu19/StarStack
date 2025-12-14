namespace ShiftSync.Api.DTOs
{
    public class DriverDashboardDto
    {
        public int DriverId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;
        public string WeeklyOff { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int FatigueScore { get; set; }
        public AttendanceSummaryDto? TodayAttendance { get; set; }
        public List<AssignmentDto> Assignments { get; set; } = new();
    }

    public class AttendanceSummaryDto
    {
        public int AttendanceId { get; set; }
        public DateTime? CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public decimal TotalHours { get; set; }
        public bool IsAbsent { get; set; }
    }

    public class AssignmentDto
    {
        public int AssignmentId { get; set; }
        public string LoadRef { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }
}
