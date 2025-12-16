using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ShiftSync.Api.Data;
using ShiftSync.Api.DTOs;
using ShiftSync.Api.Models;
using ShiftSync.Api.Services;

namespace ShiftSync.Api.Controllers
{
    [ApiController]
    [Route("api/driver")]
    [Authorize(Roles = "DRIVER")]
    public class DriverDashboardController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly FatigueService _fatigueService;

        public DriverDashboardController(AppDbContext context, FatigueService fatigueService)
        {
            _context = context;
            _fatigueService = fatigueService;
        }

        private int GetDriverIdFromToken()
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (idClaim == null) throw new UnauthorizedAccessException("Invalid token");
            return int.Parse(idClaim.Value);
        }

        // GET /api/driver/me - Get driver profile
        [HttpGet("me")]
        public async Task<IActionResult> GetMe()
        {
            try
            {
                int driverId = GetDriverIdFromToken();
                var driver = await _context.Drivers
                    .Where(x => x.DriverId == driverId)
                    .Select(x => new DriverProfileDto
                    {
                        DriverId = x.DriverId,
                        Name = x.Name,
                        Phone = x.Phone,
                        Email = x.Email ?? "",
                        Region = x.Region,
                        VehicleType = x.VehicleType,
                        WeeklyOff = x.WeeklyOff,
                        Status = x.Status,
                        FatigueScore = (int)x.FatigueScore
                    }).FirstOrDefaultAsync();

                if (driver == null) return NotFound(new { message = "Driver not found" });
                return Ok(driver);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to load profile", error = ex.Message });
            }
        }

        // GET /api/driver/dashboard - Get full dashboard data
        [HttpGet("dashboard")]
        public async Task<IActionResult> GetDashboard()
        {
            try
            {
                int driverId = GetDriverIdFromToken();

                var driver = await _context.Drivers
                    .Where(d => d.DriverId == driverId)
                    .FirstOrDefaultAsync();

                if (driver == null) return NotFound(new { message = "Driver not found" });

                // Get all attendance records for this driver and filter in memory
                var allAttendance = await _context.Attendances
                    .Where(a => a.DriverId == driverId)
                    .ToListAsync();

                var today = DateTime.UtcNow.Date;
                var attendance = allAttendance.FirstOrDefault(a => a.Date.Date == today);

                // Get all assignments for this driver and filter in memory
                var allAssignments = await _context.ShiftAssignments
                    .Where(s => s.DriverId == driverId)
                    .ToListAsync();

                var assignments = allAssignments
                    .Where(s => s.AssignedDate.Date == today)
                    .Select(s => new AssignmentDto
                    {
                        AssignmentId = s.AssignmentId,
                        LoadRef = s.LoadRef,
                        Status = s.Status
                    }).ToList();

                var dto = new DriverDashboardDto
                {
                    DriverId = driver.DriverId,
                    Name = driver.Name,
                    Phone = driver.Phone,
                    Email = driver.Email ?? "",
                    Region = driver.Region,
                    VehicleType = driver.VehicleType,
                    WeeklyOff = driver.WeeklyOff,
                    Status = driver.Status,
                    FatigueScore = (int)driver.FatigueScore,
                    TodayAttendance = attendance == null ? null : new AttendanceSummaryDto
                    {
                        AttendanceId = attendance.AttendanceId,
                        CheckInTime = attendance.CheckInTime,
                        CheckOutTime = attendance.CheckOutTime,
                        TotalHours = attendance.TotalHours,
                        IsAbsent = attendance.IsAbsent
                    },
                    Assignments = assignments
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to load dashboard", error = ex.Message });
            }
        }

        // GET /api/driver/attendance/today - Get today's attendance
        [HttpGet("attendance/today")]
        public async Task<IActionResult> GetTodayAttendance()
        {
            try
            {
                int driverId = GetDriverIdFromToken();

                var allAttendance = await _context.Attendances
                    .Where(a => a.DriverId == driverId)
                    .ToListAsync();

                var today = DateTime.UtcNow.Date;
                var attendance = allAttendance.FirstOrDefault(a => a.Date.Date == today);

                if (attendance == null)
                {
                    return Ok(new { message = "No attendance record for today", attendance = (object?)null });
                }

                return Ok(new AttendanceSummaryDto
                {
                    AttendanceId = attendance.AttendanceId,
                    CheckInTime = attendance.CheckInTime,
                    CheckOutTime = attendance.CheckOutTime,
                    TotalHours = attendance.TotalHours,
                    IsAbsent = attendance.IsAbsent
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to load attendance", error = ex.Message });
            }
        }

        // POST /api/driver/checkin - Check in
        [HttpPost("checkin")]
        public async Task<IActionResult> CheckIn()
        {
            try
            {
                int driverId = GetDriverIdFromToken();
                var now = DateTime.UtcNow;
                // Use UTC date for PostgreSQL compatibility
                var today = DateTime.SpecifyKind(now.Date, DateTimeKind.Utc);

                // Get all attendance and filter in memory
                var allAttendance = await _context.Attendances
                    .Where(a => a.DriverId == driverId)
                    .ToListAsync();

                var existing = allAttendance.FirstOrDefault(a => a.Date.Date == today.Date);

                if (existing != null && existing.CheckInTime != null)
                    return BadRequest(new { message = "Already checked in today" });

                if (existing == null)
                {
                    existing = new Attendance
                    {
                        DriverId = driverId,
                        Date = today,
                        CheckInTime = now,
                        IsAbsent = false,
                        CreatedAt = now
                    };
                    _context.Attendances.Add(existing);
                }
                else
                {
                    existing.CheckInTime = now;
                    existing.IsAbsent = false;
                }

                await _context.SaveChangesAsync();

                // Update consecutive days after check-in
                await _fatigueService.UpdateConsecutiveDays(driverId);

                return Ok(new { message = "Checked in successfully", checkInTime = existing.CheckInTime });
            }
            catch (DbUpdateException)
            {
                return BadRequest(new { message = "Already checked in today" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Check-in failed", error = ex.Message });
            }
        }

        // POST /api/driver/checkout - Check out
        [HttpPost("checkout")]
        public async Task<IActionResult> CheckOut()
        {
            try
            {
                int driverId = GetDriverIdFromToken();
                var now = DateTime.UtcNow;
                // Use UTC date for PostgreSQL compatibility
                var today = DateTime.SpecifyKind(now.Date, DateTimeKind.Utc);

                var allAttendance = await _context.Attendances
                    .Where(a => a.DriverId == driverId)
                    .ToListAsync();

                var attendance = allAttendance.FirstOrDefault(a => a.Date.Date == today.Date);

                if (attendance == null || attendance.CheckInTime == null)
                    return BadRequest(new { message = "No check-in record found for today" });

                if (attendance.CheckOutTime != null)
                    return BadRequest(new { message = "Already checked out today" });

                attendance.CheckOutTime = now;
                var totalHours = (attendance.CheckOutTime.Value - attendance.CheckInTime.Value).TotalHours;
                attendance.TotalHours = Math.Round((decimal)totalHours, 2);

                // Set overtime flag if worked more than 8 hours
                attendance.IsOvertime = attendance.TotalHours > 8;

                await _context.SaveChangesAsync();

                // Update fatigue score after checkout
                var newFatigueScore = await _fatigueService.UpdateDriverFatigueScore(driverId);

                return Ok(new
                {
                    message = "Checked out successfully",
                    totalHours = attendance.TotalHours,
                    isOvertime = attendance.IsOvertime,
                    fatigueScore = newFatigueScore
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Check-out failed", error = ex.Message });
            }
        }

        // GET /api/driver/fatigue - Get driver's fatigue breakdown
        [HttpGet("fatigue")]
        public async Task<IActionResult> GetFatigueBreakdown()
        {
            try
            {
                int driverId = GetDriverIdFromToken();
                var breakdown = await _fatigueService.CalculateFatigueScore(driverId);
                return Ok(breakdown);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to load fatigue data", error = ex.Message });
            }
        }

        // GET /api/driver/workload - Get driver's current workload for today
        [HttpGet("workload")]
        public async Task<IActionResult> GetTodayWorkload()
        {
            try
            {
                int driverId = GetDriverIdFromToken();
                // Use UTC date range for PostgreSQL compatibility
                var todayStart = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
                var todayEnd = todayStart.AddDays(1);

                var assignments = await _context.ShiftAssignments
                    .Include(s => s.Load)
                    .Where(s => s.DriverId == driverId &&
                               s.AssignedDate >= todayStart && s.AssignedDate < todayEnd &&
                               s.Status != "COMPLETED")
                    .ToListAsync();

                var totalStops = assignments.Sum(a => a.Load?.Stops ?? 0);
                var totalHours = assignments.Sum(a => a.Load?.EstimatedHours ?? 0);
                var totalDistance = assignments.Sum(a => a.Load?.EstimatedDistance ?? 0);

                // Calculate overload indicator
                decimal stopsNorm = Math.Min(1m, totalStops / 60m);
                decimal hoursNorm = Math.Min(1m, totalHours / 10m);
                decimal distanceNorm = Math.Min(1m, totalDistance / 200m);
                decimal overloadScore = 0.50m * stopsNorm + 0.30m * hoursNorm + 0.20m * distanceNorm;

                string overloadStatus = overloadScore < 0.75m ? "SAFE" :
                                       overloadScore < 0.90m ? "WARNING" : "UNSAFE";

                return Ok(new
                {
                    activeLoadsCount = assignments.Count,
                    totalStops,
                    totalEstimatedHours = Math.Round(totalHours, 2),
                    totalEstimatedDistance = Math.Round(totalDistance, 2),
                    overloadScore = Math.Round(overloadScore, 4),
                    overloadStatus,
                    loads = assignments.Select(a => new
                    {
                        a.AssignmentId,
                        a.LoadRef,
                        a.Status,
                        Stops = a.Load?.Stops ?? 0,
                        EstimatedHours = a.Load?.EstimatedHours ?? 0,
                        Region = a.Load?.Region ?? ""
                    })
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Failed to load workload data", error = ex.Message });
            }
        }
    }
}
