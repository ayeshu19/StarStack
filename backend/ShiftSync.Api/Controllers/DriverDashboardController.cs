using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ShiftSync.Api.Data;
using ShiftSync.Api.DTOs;
using ShiftSync.Api.Models;

namespace ShiftSync.Api.Controllers
{
    [ApiController]
    [Route("api/driver")]
    [Authorize(Roles = "DRIVER")]
    public class DriverDashboardController : ControllerBase
    {
        private readonly AppDbContext _context;
        public DriverDashboardController(AppDbContext context) => _context = context;

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
                var today = now.Date;

                // Get all attendance and filter in memory
                var allAttendance = await _context.Attendances
                    .Where(a => a.DriverId == driverId)
                    .ToListAsync();

                var existing = allAttendance.FirstOrDefault(a => a.Date.Date == today);

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
                var today = now.Date;

                var allAttendance = await _context.Attendances
                    .Where(a => a.DriverId == driverId)
                    .ToListAsync();

                var attendance = allAttendance.FirstOrDefault(a => a.Date.Date == today);

                if (attendance == null || attendance.CheckInTime == null)
                    return BadRequest(new { message = "No check-in record found for today" });

                if (attendance.CheckOutTime != null)
                    return BadRequest(new { message = "Already checked out today" });

                attendance.CheckOutTime = now;
                var totalHours = (attendance.CheckOutTime.Value - attendance.CheckInTime.Value).TotalHours;
                attendance.TotalHours = Math.Round((decimal)totalHours, 2);

                await _context.SaveChangesAsync();

                return Ok(new { message = "Checked out successfully", totalHours = attendance.TotalHours });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Check-out failed", error = ex.Message });
            }
        }
    }
}
