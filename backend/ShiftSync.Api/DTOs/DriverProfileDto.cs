namespace ShiftSync.Api.DTOs
{
    public class DriverProfileDto
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
    }
}
