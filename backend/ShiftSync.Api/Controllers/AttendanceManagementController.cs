using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShiftSync.Api.Data;
using ShiftSync.Api.DTOs;
using ShiftSync.Api.Models;

namespace ShiftSync.Api.Controllers
{
    [ApiController]
    [Route("api/admin/attendance")]
    public class AttendanceManagementController : ControllerBase
    {
        private readonly AppDbContext _context;

        public AttendanceManagementController(AppDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// [TEST ENDPOINT] Bulk check-in all active drivers for today (for testing auto-assignment)
        /// </summary>
        [HttpPost("bulk-checkin")]
        public async Task<IActionResult> BulkCheckIn()
        {
            try
            {
                var now = DateTime.UtcNow;
                var today = DateTime.SpecifyKind(now.Date, DateTimeKind.Utc);
                var todayEnd = today.AddDays(1);

                // Get all active drivers
                var activeDrivers = await _context.Drivers
                    .Where(d => d.Status == "ACTIVE")
                    .ToListAsync();

                // Get ALL attendance records and filter in memory to avoid DateTime issues
                var allAttendance = await _context.Attendances.ToListAsync();
                var existingAttendance = allAttendance
                    .Where(a => a.Date.Date == today.Date)
                    .ToList();

                var checkedInCount = 0;
                var alreadyCheckedIn = 0;
                var errors = new List<string>();

                foreach (var driver in activeDrivers)
                {
                    try
                    {
                        var existing = existingAttendance.FirstOrDefault(a => a.DriverId == driver.DriverId);

                        if (existing != null && existing.CheckInTime != null)
                        {
                            alreadyCheckedIn++;
                            continue;
                        }

                        if (existing == null)
                        {
                            var attendance = new Attendance
                            {
                                DriverId = driver.DriverId,
                                Date = today,
                                CheckInTime = now,
                                IsAbsent = false,
                                IsOvertime = false,
                                TotalHours = 0,
                                CreatedAt = now
                            };
                            _context.Attendances.Add(attendance);
                            await _context.SaveChangesAsync(); // Save one at a time
                            checkedInCount++;
                        }
                        else
                        {
                            existing.CheckInTime = now;
                            existing.IsAbsent = false;
                            await _context.SaveChangesAsync();
                            checkedInCount++;
                        }
                    }
                    catch (Exception innerEx)
                    {
                        errors.Add($"Driver {driver.DriverId} ({driver.Name}): {innerEx.InnerException?.Message ?? innerEx.Message}");
                    }
                }

                return Ok(new
                {
                    message = "Bulk check-in completed",
                    checkedInCount,
                    alreadyCheckedIn,
                    totalDrivers = activeDrivers.Count,
                    errors = errors.Count > 0 ? errors : null
                });
            }
            catch (Exception ex)
            {
                var innerMessage = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { error = innerMessage, details = ex.ToString() });
            }
        }

        // GET: api/admin/attendance
        // Returns ALL attendance rows (no date/region/status filter for now)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AttendanceDto>>> GetAttendance()
        {
            var query =
                from a in _context.Attendances
                join d in _context.Drivers on a.DriverId equals d.DriverId
                select new { a, d };

            var list = await query
                .Select(x => new AttendanceDto
                {
                    AttendanceId = x.a.AttendanceId,
                    DriverId = x.d.DriverId,
                    DriverName = x.d.Name,
                    Region = x.d.Region,
                    Date = x.a.Date,
                    CheckInTime = x.a.CheckInTime,
                    CheckOutTime = x.a.CheckOutTime,
                    TotalHours = x.a.TotalHours,
                    IsAbsent = x.a.IsAbsent,
                    IsOvertime = x.a.IsOvertime,
                    OvertimeApproved = x.a.OvertimeApproved,
                    Status =
                        x.a.IsAbsent
                            ? "Absent"
                            : x.a.CheckInTime == null
                                ? "Absent"
                                : x.a.CheckOutTime == null
                                    ? "Missing Checkout"
                                    : x.a.CheckInTime.Value.TimeOfDay > new TimeSpan(9, 0, 0)
                                        ? "Late"
                                        : "Present"
                })
                .ToListAsync();

            return Ok(list);
        }

        // GET: api/admin/attendance/stats
        // Computes stats from ALL attendance rows (no date filter for now)
        [HttpGet("stats")]
        public async Task<ActionResult<AttendanceStatsDto>> GetStats()
        {
            var attendances = await _context.Attendances.ToListAsync();
            var totalDrivers = await _context.Drivers.CountAsync();

            var stats = new AttendanceStatsDto
            {
                TotalDrivers = totalDrivers,
                PresentCount = attendances.Count(a => !a.IsAbsent && a.CheckInTime != null),
                AbsentCount = totalDrivers - attendances.Count(a => !a.IsAbsent),
                LateCheckIns = attendances.Count(a =>
                    !a.IsAbsent &&
                    a.CheckInTime != null &&
                    a.CheckInTime.Value.TimeOfDay > new TimeSpan(9, 0, 0)),
                MissingCheckOuts = attendances.Count(a =>
                    !a.IsAbsent &&
                    a.CheckInTime != null &&
                    a.CheckOutTime == null),
                OvertimeCount = attendances.Count(a => a.IsOvertime),
                PendingOvertimeApprovals = attendances.Count(a =>
                    a.IsOvertime && a.OvertimeApproved == null)
            };

            return Ok(stats);
        }
    }
}