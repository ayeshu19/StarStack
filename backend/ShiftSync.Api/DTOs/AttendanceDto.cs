// DTOs/AttendanceDtos.cs
using System;

namespace ShiftSync.Api.DTOs
{
    public class AttendanceDto
    {
        public int AttendanceId { get; set; }
        public int DriverId { get; set; }
        public string DriverName { get; set; } = "";
        public string Region { get; set; } = "";
        public DateTime Date { get; set; }
        public DateTime? CheckInTime { get; set; }
        public DateTime? CheckOutTime { get; set; }
        public decimal TotalHours { get; set; }
        public bool IsAbsent { get; set; }
        public bool IsOvertime { get; set; }
        public bool? OvertimeApproved { get; set; }
        public string Status { get; set; } = ""; // Present / Absent / Late / Missing Checkout
    }

    public class AttendanceStatsDto
    {
        public int TotalDrivers { get; set; }
        public int PresentCount { get; set; }
        public int AbsentCount { get; set; }
        public int LateCheckIns { get; set; }
        public int MissingCheckOuts { get; set; }
        public int OvertimeCount { get; set; }
        public int PendingOvertimeApprovals { get; set; }
    }
}