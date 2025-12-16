namespace ShiftSync.Api.DTOs
{
    public class UpdateDriverDto
    {
        public string? Name { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Region { get; set; }
        public string? VehicleType { get; set; }
        public string? WeeklyOff { get; set; }
    }
}