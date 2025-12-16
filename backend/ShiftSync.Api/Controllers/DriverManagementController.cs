using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShiftSync.Api.Data;
using ShiftSync.Api.DTOs;
using ShiftSync.Api.Models;
using ShiftSync.Api.Utils;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;

namespace ShiftSync.Api.Controllers
{
    [ApiController]
    [Route("api/admin/drivers")]
    //[Authorize] // Add your admin authorization policy here if needed
    public class DriverManagementController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DriverManagementController> _logger;

        public DriverManagementController(AppDbContext context, ILogger<DriverManagementController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ==========================================
        // DRIVER CRUD OPERATIONS
        // ==========================================

        // GET: /api/admin/drivers?status=ACTIVE&region=North&search=john
        [HttpGet]
        public async Task<ActionResult<IEnumerable<DriverProfileDto>>> GetAllDrivers(
            [FromQuery] string? status = null,
            [FromQuery] string? region = null,
            [FromQuery] string? search = null)
        {
            try
            {
                var query = _context.Drivers.AsQueryable();

                // Apply filters
                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(d => d.Status == status);
                }

                if (!string.IsNullOrEmpty(region))
                {
                    query = query.Where(d => d.Region == region);
                }

                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(d => 
                        d.Name.Contains(search) || 
                        d.Phone.Contains(search));
                }

                var drivers = await query
                    .OrderByDescending(d => d.CreatedAt)
                    .Select(d => new DriverProfileDto
                    {
                        DriverId = d.DriverId,
                        Name = d.Name,
                        Phone = d.Phone,
                        Email = d.Email,
                        Region = d.Region,
                        VehicleType = d.VehicleType,
                        WeeklyOff = d.WeeklyOff,
                        Status = d.Status,
                        FatigueScore = d.FatigueScore,
                        LastAssignmentDate = d.LastAssignmentDate,
                        ConsecutiveDays = d.ConsecutiveDays,
                        CreatedAt = d.CreatedAt
                    })
                    .ToListAsync();

                return Ok(drivers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching drivers");
                return StatusCode(500, new { message = "An error occurred while fetching drivers" });
            }
        }

        // GET: /api/admin/drivers/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<DriverProfileDto>> GetDriverById(int id)
        {
            try
            {
                var driver = await _context.Drivers
                    .Where(d => d.DriverId == id)
                    .Select(d => new DriverProfileDto
                    {
                        DriverId = d.DriverId,
                        Name = d.Name,
                        Phone = d.Phone,
                        Email = d.Email,
                        Region = d.Region,
                        VehicleType = d.VehicleType,
                        WeeklyOff = d.WeeklyOff,
                        Status = d.Status,
                        FatigueScore = d.FatigueScore,
                        LastAssignmentDate = d.LastAssignmentDate,
                        ConsecutiveDays = d.ConsecutiveDays,
                        CreatedAt = d.CreatedAt
                    })
                    .FirstOrDefaultAsync();

                if (driver == null)
                {
                    return NotFound(new { message = "Driver not found" });
                }

                return Ok(driver);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching driver {DriverId}", id);
                return StatusCode(500, new { message = "An error occurred while fetching the driver" });
            }
        }

        // POST: /api/admin/drivers
        [HttpPost]
        public async Task<ActionResult<DriverProfileDto>> CreateDriver([FromBody] AdminCreateDriverDto dto)
        {
            try
            {
                // Validate required fields
                if (string.IsNullOrWhiteSpace(dto.Name))
                {
                    return BadRequest(new { message = "Name is required" });
                }

                if (string.IsNullOrWhiteSpace(dto.Phone))
                {
                    return BadRequest(new { message = "Phone is required" });
                }

                if (string.IsNullOrWhiteSpace(dto.Region))
                {
                    return BadRequest(new { message = "Region is required" });
                }

                if (string.IsNullOrWhiteSpace(dto.VehicleType))
                {
                    return BadRequest(new { message = "Vehicle type is required" });
                }

                if (string.IsNullOrWhiteSpace(dto.WeeklyOff))
                {
                    return BadRequest(new { message = "Weekly off is required" });
                }

                // Check for duplicate phone
                if (await _context.Drivers.AnyAsync(d => d.Phone == dto.Phone))
                {
                    return BadRequest(new { message = "Phone number already exists" });
                }

                // Check for duplicate email if provided
                if (!string.IsNullOrEmpty(dto.Email) && 
                    await _context.Drivers.AnyAsync(d => d.Email == dto.Email))
                {
                    return BadRequest(new { message = "Email already exists" });
                }

                var driver = new Driver
                {
                    Name = dto.Name,
                    Phone = dto.Phone,
                    Email = dto.Email,
                    Region = dto.Region,
                    VehicleType = dto.VehicleType,
                    WeeklyOff = dto.WeeklyOff,
                    Status = "ACTIVE",
                    PasswordHash = string.IsNullOrEmpty(dto.Password)
                        ? PasswordHasher.HashPassword("driver123")
                        : PasswordHasher.HashPassword(dto.Password),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Drivers.Add(driver);
                await _context.SaveChangesAsync();

                var result = new DriverProfileDto
                {
                    DriverId = driver.DriverId,
                    Name = driver.Name,
                    Phone = driver.Phone,
                    Email = driver.Email,
                    Region = driver.Region,
                    VehicleType = driver.VehicleType,
                    WeeklyOff = driver.WeeklyOff,
                    Status = driver.Status,
                    FatigueScore = driver.FatigueScore,
                    LastAssignmentDate = driver.LastAssignmentDate,
                    ConsecutiveDays = driver.ConsecutiveDays,
                    CreatedAt = driver.CreatedAt
                };

                return CreatedAtAction(nameof(GetDriverById), new { id = driver.DriverId }, result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating driver");
                return StatusCode(500, new { message = "An error occurred while creating the driver" });
            }
        }

        // PUT: /api/admin/drivers/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<DriverProfileDto>> UpdateDriver(int id, [FromBody] UpdateDriverDto dto)
        {
            try
            {
                var driver = await _context.Drivers.FindAsync(id);
                if (driver == null)
                {
                    return NotFound(new { message = "Driver not found" });
                }

                // Check for duplicate phone if changed
                if (!string.IsNullOrEmpty(dto.Phone) && dto.Phone != driver.Phone)
                {
                    if (await _context.Drivers.AnyAsync(d => d.Phone == dto.Phone))
                    {
                        return BadRequest(new { message = "Phone number already exists" });
                    }
                    driver.Phone = dto.Phone;
                }

                // Check for duplicate email if changed
                if (dto.Email != null && dto.Email != driver.Email)
                {
                    if (!string.IsNullOrEmpty(dto.Email) && 
                        await _context.Drivers.AnyAsync(d => d.Email == dto.Email))
                    {
                        return BadRequest(new { message = "Email already exists" });
                    }
                    driver.Email = dto.Email;
                }

                // Update other fields if provided
                if (!string.IsNullOrEmpty(dto.Name)) driver.Name = dto.Name;
                if (!string.IsNullOrEmpty(dto.Region)) driver.Region = dto.Region;
                if (!string.IsNullOrEmpty(dto.VehicleType)) driver.VehicleType = dto.VehicleType;
                if (!string.IsNullOrEmpty(dto.WeeklyOff)) driver.WeeklyOff = dto.WeeklyOff;
                
                driver.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var result = new DriverProfileDto
                {
                    DriverId = driver.DriverId,
                    Name = driver.Name,
                    Phone = driver.Phone,
                    Email = driver.Email,
                    Region = driver.Region,
                    VehicleType = driver.VehicleType,
                    WeeklyOff = driver.WeeklyOff,
                    Status = driver.Status,
                    FatigueScore = driver.FatigueScore,
                    LastAssignmentDate = driver.LastAssignmentDate,
                    ConsecutiveDays = driver.ConsecutiveDays,
                    CreatedAt = driver.CreatedAt
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating driver {DriverId}", id);
                return StatusCode(500, new { message = "An error occurred while updating the driver" });
            }
        }

        // PATCH: /api/admin/drivers/{id}/status
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateDriverStatus(int id, [FromBody] UpdateStatusDto dto)
        {
            try
            {
                var driver = await _context.Drivers.FindAsync(id);
                if (driver == null)
                {
                    return NotFound(new { message = "Driver not found" });
                }

                // Check for active assignments if deactivating
                if (dto.Status == "INACTIVE")
                {
                    var hasActiveLoads = await _context.ShiftAssignments
                        .AnyAsync(sa => sa.DriverId == id && 
                                       (sa.Status == "ASSIGNED" || sa.Status == "IN_PROGRESS"));
                    
                    if (hasActiveLoads)
                    {
                        return BadRequest(new { 
                            message = "Cannot deactivate driver with active assignments",
                            warning = true 
                        });
                    }
                }

                driver.Status = dto.Status;
                driver.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Driver status updated successfully", status = driver.Status });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating driver status {DriverId}", id);
                return StatusCode(500, new { message = "An error occurred while updating driver status" });
            }
        }

        // ==========================================
        // STATISTICS & METADATA
        // ==========================================

        // GET: /api/admin/drivers/stats
        [HttpGet("stats")]
        public async Task<ActionResult<DriverStatsDto>> GetDriverStats()
        {
            try
            {
                var drivers = await _context.Drivers.ToListAsync();

                var stats = new DriverStatsDto
                {
                    TotalDrivers = drivers.Count,
                    ActiveDrivers = drivers.Count(d => d.Status == "ACTIVE"),
                    InactiveDrivers = drivers.Count(d => d.Status == "INACTIVE"),
                    HighFatigueDrivers = drivers.Count(d => d.FatigueScore > 70),
                    DriversByRegion = drivers
                        .GroupBy(d => d.Region)
                        .ToDictionary(g => g.Key, g => g.Count())
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching driver stats");
                return StatusCode(500, new { message = "An error occurred while fetching driver statistics" });
            }
        }

        // GET: /api/admin/drivers/regions
        [HttpGet("regions")]
        public async Task<ActionResult<IEnumerable<string>>> GetRegions()
        {
            try
            {
                var regions = await _context.Drivers
                    .Select(d => d.Region)
                    .Distinct()
                    .OrderBy(r => r)
                    .ToListAsync();

                return Ok(regions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching regions");
                return StatusCode(500, new { message = "An error occurred while fetching regions" });
            }
        }

        // GET: /api/admin/drivers/vehicle-types
        [HttpGet("vehicle-types")]
        public async Task<ActionResult<IEnumerable<string>>> GetVehicleTypes()
        {
            try
            {
                var vehicleTypes = await _context.Drivers
                    .Select(d => d.VehicleType)
                    .Distinct()
                    .OrderBy(v => v)
                    .ToListAsync();

                return Ok(vehicleTypes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching vehicle types");
                return StatusCode(500, new { message = "An error occurred while fetching vehicle types" });
            }
        }

        // GET: /api/admin/drivers/weekly-offs
        [HttpGet("weekly-offs")]
        public async Task<ActionResult<IEnumerable<string>>> GetWeeklyOffs()
        {
            try
            {
                var weeklyOffs = await _context.Drivers
                    .Select(d => d.WeeklyOff)
                    .Distinct()
                    .OrderBy(w => w)
                    .ToListAsync();

                return Ok(weeklyOffs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching weekly offs");
                return StatusCode(500, new { message = "An error occurred while fetching weekly offs" });
            }
        }

        // ==========================================
        // CSV UPLOAD
        // ==========================================

        // POST: /api/admin/drivers/upload-csv
        [HttpPost("upload-csv")]
public async Task<IActionResult> UploadCsv(IFormFile file)
{
    if (file == null || file.Length == 0)
        return BadRequest(new { message = "No file uploaded" });

    try
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            MissingFieldFound = null,
            HeaderValidated = null,
            BadDataFound = null,
            IgnoreBlankLines = true,
            TrimOptions = TrimOptions.Trim
        };

        using var reader = new StreamReader(file.OpenReadStream());
        using var csv = new CsvReader(reader, config);

        var records = csv.GetRecords<CsvDriverDto>().ToList();

        var driversToAdd = new List<Driver>();

        foreach (var record in records)
        {
            if (string.IsNullOrWhiteSpace(record.Name) ||
                string.IsNullOrWhiteSpace(record.Phone) ||
                string.IsNullOrWhiteSpace(record.Region) ||
                string.IsNullOrWhiteSpace(record.VehicleType) ||
                string.IsNullOrWhiteSpace(record.WeeklyOff))
            {
                continue;
            }

            if (await _context.Drivers.AnyAsync(d => d.Phone == record.Phone))
                continue;

            driversToAdd.Add(new Driver
            {
                Name = record.Name.Trim(),
                Phone = record.Phone.Trim(),
                Email = string.IsNullOrWhiteSpace(record.Email) ? null : record.Email.Trim(),
                Region = record.Region.Trim(),
                VehicleType = record.VehicleType.Trim(),
                WeeklyOff = record.WeeklyOff.Trim(),
                Status = "ACTIVE",
                PasswordHash = PasswordHasher.HashPassword("driver123"),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        if (driversToAdd.Any())
        {
            _context.Drivers.AddRange(driversToAdd);
            await _context.SaveChangesAsync();
        }

        return Ok(new { addedCount = driversToAdd.Count });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "CSV upload failed");
        return StatusCode(500, new { error = ex.Message });
    }
}
    }

    // ==========================================
    // HELPER DTOs
    // ==========================================

    public class UpdateStatusDto
    {
        public string Status { get; set; } = string.Empty;
    }
}