using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShiftSync.Api.Data;

namespace ShiftSync.Api.Controllers
{
    [ApiController]
    [Route("api/admin/reports")]
    // [Authorize(Roles = "ADMIN")] // Commented out for development - uncomment for production
    public class ReportsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public ReportsController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get attendance report for date range
        /// </summary>
        [HttpGet("attendance")]
        public async Task<IActionResult> GetAttendanceReport(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] string? region)
        {
            try
            {
                // Use UTC dates for PostgreSQL compatibility
                var start = DateTime.SpecifyKind(startDate?.Date ?? DateTime.UtcNow.AddDays(-7).Date, DateTimeKind.Utc);
                var end = DateTime.SpecifyKind(endDate?.Date ?? DateTime.UtcNow.Date, DateTimeKind.Utc).AddDays(1);

                var driversQuery = _context.Drivers.Where(d => d.Status == "ACTIVE");
                if (!string.IsNullOrEmpty(region))
                    driversQuery = driversQuery.Where(d => d.Region == region);

                var drivers = await driversQuery.ToListAsync();
                var driverIds = drivers.Select(d => d.DriverId).ToList();

                var attendances = await _context.Attendances
                    .Where(a => driverIds.Contains(a.DriverId) && a.Date >= start && a.Date < end)
                    .ToListAsync();

                var report = drivers.Select(driver =>
                {
                    var driverAttendances = attendances.Where(a => a.DriverId == driver.DriverId).ToList();
                    return new
                    {
                        driver.DriverId,
                        driver.Name,
                        driver.Region,
                        DaysPresent = driverAttendances.Count(a => !a.IsAbsent),
                        DaysAbsent = driverAttendances.Count(a => a.IsAbsent),
                        DaysLate = driverAttendances.Count(a => a.CheckInTime.HasValue && a.CheckInTime.Value.Hour >= 9),
                        TotalHoursWorked = Math.Round(driverAttendances.Sum(a => a.TotalHours), 2),
                        OvertimeDays = driverAttendances.Count(a => a.TotalHours > 8),
                        AverageHoursPerDay = driverAttendances.Any(a => !a.IsAbsent)
                            ? Math.Round(driverAttendances.Where(a => !a.IsAbsent).Average(a => a.TotalHours), 2)
                            : 0
                    };
                }).OrderBy(r => r.Name).ToList();

                return Ok(new
                {
                    startDate = start,
                    endDate = end,
                    region = region ?? "All",
                    totalDrivers = report.Count,
                    report
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get workload/load history report
        /// </summary>
        [HttpGet("workload")]
        public async Task<IActionResult> GetWorkloadReport(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] string? region)
        {
            try
            {
                // Use UTC dates for PostgreSQL compatibility
                var start = DateTime.SpecifyKind(startDate?.Date ?? DateTime.UtcNow.AddDays(-7).Date, DateTimeKind.Utc);
                var end = DateTime.SpecifyKind(endDate?.Date ?? DateTime.UtcNow.Date, DateTimeKind.Utc).AddDays(1);

                var driversQuery = _context.Drivers.Where(d => d.Status == "ACTIVE");
                if (!string.IsNullOrEmpty(region))
                    driversQuery = driversQuery.Where(d => d.Region == region);

                var drivers = await driversQuery.ToListAsync();
                var driverIds = drivers.Select(d => d.DriverId).ToList();

                var assignments = await _context.ShiftAssignments
                    .Include(s => s.Load)
                    .Where(s => driverIds.Contains(s.DriverId) && s.AssignedDate >= start && s.AssignedDate < end)
                    .ToListAsync();

                var report = drivers.Select(driver =>
                {
                    var driverAssignments = assignments.Where(a => a.DriverId == driver.DriverId).ToList();
                    return new
                    {
                        driver.DriverId,
                        driver.Name,
                        driver.Region,
                        TotalLoadsAssigned = driverAssignments.Count,
                        CompletedLoads = driverAssignments.Count(a => a.Status == "COMPLETED"),
                        InProgressLoads = driverAssignments.Count(a => a.Status == "IN_PROGRESS"),
                        TotalStops = driverAssignments.Sum(a => a.Load?.Stops ?? 0),
                        TotalDistance = Math.Round(driverAssignments.Sum(a => a.Load?.EstimatedDistance ?? 0), 2),
                        TotalEstimatedHours = Math.Round(driverAssignments.Sum(a => a.Load?.EstimatedHours ?? 0), 2),
                        AverageSuitabilityScore = driverAssignments.Any()
                            ? Math.Round(driverAssignments.Average(a => a.SuitabilityScore), 2)
                            : 0
                    };
                }).OrderByDescending(r => r.TotalLoadsAssigned).ToList();

                return Ok(new
                {
                    startDate = start,
                    endDate = end,
                    region = region ?? "All",
                    totalDrivers = report.Count,
                    report
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get fatigue trends report
        /// </summary>
        [HttpGet("fatigue-trends")]
        public async Task<IActionResult> GetFatigueTrendsReport(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            try
            {
                // Use UTC dates for PostgreSQL compatibility
                var start = DateTime.SpecifyKind(startDate?.Date ?? DateTime.UtcNow.AddDays(-7).Date, DateTimeKind.Utc);
                var end = DateTime.SpecifyKind(endDate?.Date ?? DateTime.UtcNow.Date, DateTimeKind.Utc).AddDays(1);

                var drivers = await _context.Drivers
                    .Where(d => d.Status == "ACTIVE")
                    .OrderByDescending(d => d.FatigueScore)
                    .Select(d => new
                    {
                        d.DriverId,
                        d.Name,
                        d.Region,
                        d.FatigueScore,
                        d.ConsecutiveDays,
                        d.LastAssignmentDate,
                        FatigueLevel = d.FatigueScore <= 40 ? "LOW" :
                                      d.FatigueScore <= 70 ? "MEDIUM" : "HIGH",
                        NeedsRest = d.FatigueScore > 85
                    })
                    .ToListAsync();

                var attendances = await _context.Attendances
                    .Where(a => a.Date >= start && a.Date < end)
                    .ToListAsync();

                var fatigueDistribution = new
                {
                    LowFatigue = drivers.Count(d => d.FatigueLevel == "LOW"),
                    MediumFatigue = drivers.Count(d => d.FatigueLevel == "MEDIUM"),
                    HighFatigue = drivers.Count(d => d.FatigueLevel == "HIGH"),
                    NeedingRest = drivers.Count(d => d.NeedsRest)
                };

                var overtimeStats = new
                {
                    TotalOvertimeDays = attendances.Count(a => a.TotalHours > 8),
                    DriversWithOvertime = attendances.Where(a => a.TotalHours > 8).Select(a => a.DriverId).Distinct().Count(),
                    AverageOvertimeHours = attendances.Where(a => a.TotalHours > 8).Any()
                        ? Math.Round(attendances.Where(a => a.TotalHours > 8).Average(a => a.TotalHours - 8), 2)
                        : 0
                };

                return Ok(new
                {
                    startDate = start,
                    endDate = end,
                    fatigueDistribution,
                    overtimeStats,
                    averageFatigueScore = drivers.Any() ? Math.Round(drivers.Average(d => d.FatigueScore), 2) : 0,
                    drivers
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get work hours report
        /// </summary>
        [HttpGet("work-hours")]
        public async Task<IActionResult> GetWorkHoursReport(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] string? region)
        {
            try
            {
                // Use UTC dates for PostgreSQL compatibility
                var start = DateTime.SpecifyKind(startDate?.Date ?? DateTime.UtcNow.AddDays(-7).Date, DateTimeKind.Utc);
                var end = DateTime.SpecifyKind(endDate?.Date ?? DateTime.UtcNow.Date, DateTimeKind.Utc).AddDays(1);

                var driversQuery = _context.Drivers.Where(d => d.Status == "ACTIVE");
                if (!string.IsNullOrEmpty(region))
                    driversQuery = driversQuery.Where(d => d.Region == region);

                var drivers = await driversQuery.ToListAsync();
                var driverIds = drivers.Select(d => d.DriverId).ToList();

                var attendances = await _context.Attendances
                    .Where(a => driverIds.Contains(a.DriverId) && a.Date >= start && a.Date < end && !a.IsAbsent)
                    .ToListAsync();

                var report = drivers.Select(driver =>
                {
                    var driverAttendances = attendances.Where(a => a.DriverId == driver.DriverId).ToList();
                    var totalHours = driverAttendances.Sum(a => a.TotalHours);
                    var daysWorked = driverAttendances.Count;

                    return new
                    {
                        driver.DriverId,
                        driver.Name,
                        driver.Region,
                        DaysWorked = daysWorked,
                        TotalHours = Math.Round(totalHours, 2),
                        AverageHoursPerDay = daysWorked > 0 ? Math.Round(totalHours / daysWorked, 2) : 0,
                        OvertimeHours = Math.Round(driverAttendances.Where(a => a.TotalHours > 8).Sum(a => a.TotalHours - 8), 2),
                        RegularHours = Math.Round(driverAttendances.Sum(a => Math.Min(a.TotalHours, 8)), 2),
                        EarliestCheckIn = driverAttendances.Any()
                            ? driverAttendances.Where(a => a.CheckInTime.HasValue).Min(a => a.CheckInTime)?.ToString("HH:mm")
                            : "-",
                        LatestCheckOut = driverAttendances.Any()
                            ? driverAttendances.Where(a => a.CheckOutTime.HasValue).Max(a => a.CheckOutTime)?.ToString("HH:mm")
                            : "-"
                    };
                }).OrderByDescending(r => r.TotalHours).ToList();

                var summary = new
                {
                    TotalDrivers = report.Count,
                    TotalHoursWorked = Math.Round(report.Sum(r => r.TotalHours), 2),
                    TotalOvertimeHours = Math.Round(report.Sum(r => r.OvertimeHours), 2),
                    AverageHoursPerDriver = report.Any() ? Math.Round(report.Average(r => r.TotalHours), 2) : 0
                };

                return Ok(new
                {
                    startDate = start,
                    endDate = end,
                    region = region ?? "All",
                    summary,
                    report
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Get individual driver report
        /// </summary>
        [HttpGet("driver/{driverId}")]
        public async Task<IActionResult> GetDriverReport(
            int driverId,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            try
            {
                var driver = await _context.Drivers.FindAsync(driverId);
                if (driver == null)
                    return NotFound(new { error = "Driver not found" });

                // Use UTC dates for PostgreSQL compatibility
                var start = DateTime.SpecifyKind(startDate?.Date ?? DateTime.UtcNow.AddDays(-7).Date, DateTimeKind.Utc);
                var end = DateTime.SpecifyKind(endDate?.Date ?? DateTime.UtcNow.Date, DateTimeKind.Utc).AddDays(1);

                var attendances = await _context.Attendances
                    .Where(a => a.DriverId == driverId && a.Date >= start && a.Date < end)
                    .OrderByDescending(a => a.Date)
                    .ToListAsync();

                var assignments = await _context.ShiftAssignments
                    .Include(s => s.Load)
                    .Where(s => s.DriverId == driverId && s.AssignedDate >= start && s.AssignedDate < end)
                    .OrderByDescending(s => s.AssignedDate)
                    .ToListAsync();

                var dailyDetails = attendances.Select(a =>
                {
                    var dayAssignments = assignments.Where(s => s.AssignedDate.Date == a.Date.Date).ToList();
                    return new
                    {
                        Date = a.Date.ToString("yyyy-MM-dd"),
                        DayOfWeek = a.Date.DayOfWeek.ToString(),
                        CheckIn = a.CheckInTime?.ToString("HH:mm") ?? "-",
                        CheckOut = a.CheckOutTime?.ToString("HH:mm") ?? "-",
                        TotalHours = Math.Round(a.TotalHours, 2),
                        IsOvertime = a.TotalHours > 8,
                        IsAbsent = a.IsAbsent,
                        LoadsAssigned = dayAssignments.Count,
                        TotalStops = dayAssignments.Sum(s => s.Load?.Stops ?? 0),
                        Status = a.IsAbsent ? "ABSENT" : (a.CheckOutTime == null ? "WORKING" : "COMPLETED")
                    };
                }).ToList();

                return Ok(new
                {
                    driver = new
                    {
                        driver.DriverId,
                        driver.Name,
                        driver.Phone,
                        driver.Region,
                        driver.VehicleType,
                        driver.WeeklyOff,
                        driver.FatigueScore,
                        driver.ConsecutiveDays
                    },
                    period = new { startDate = start, endDate = end },
                    summary = new
                    {
                        TotalDays = dailyDetails.Count,
                        DaysPresent = dailyDetails.Count(d => !d.IsAbsent),
                        DaysAbsent = dailyDetails.Count(d => d.IsAbsent),
                        TotalHoursWorked = Math.Round(dailyDetails.Sum(d => d.TotalHours), 2),
                        OvertimeDays = dailyDetails.Count(d => d.IsOvertime),
                        TotalLoads = assignments.Count,
                        TotalStops = assignments.Sum(a => a.Load?.Stops ?? 0)
                    },
                    dailyDetails
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
