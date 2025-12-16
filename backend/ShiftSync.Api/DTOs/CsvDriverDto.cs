namespace ShiftSync.Api.DTOs
{
    public class CsvDriverDto
    {
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string Region { get; set; } = string.Empty;
        public string VehicleType { get; set; } = string.Empty;
        public string WeeklyOff { get; set; } = string.Empty;
    }
}