using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ShiftSync.Api.Data;
using ShiftSync.Api.DTOs;
using ShiftSync.Api.Models;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;

namespace ShiftSync.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LoadsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public LoadsController(AppDbContext db)
        {
            _db = db;
        }

        // =========================================================
        // GET: /api/loads?region=Tambaram&status=PENDING&date=2025-12-14
        // =========================================================
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] string? region,
            [FromQuery] string? status,
            [FromQuery] DateOnly? date
        )
        {
            var q = _db.Loads.Include(l => l.AssignedDriver).AsQueryable();

            if (!string.IsNullOrWhiteSpace(region))
                q = q.Where(l => l.Region == region);

            if (!string.IsNullOrWhiteSpace(status))
                q = q.Where(l => l.Status == status);

            // Optional date filter (CreatedAt day range in UTC)
            if (date.HasValue)
            {
                var startUtc = DateTime.SpecifyKind(
                    date.Value.ToDateTime(TimeOnly.MinValue),
                    DateTimeKind.Utc);

                var endUtc = startUtc.AddDays(1);

                q = q.Where(l => l.CreatedAt >= startUtc && l.CreatedAt < endUtc);
            }

            var data = await q.OrderByDescending(l => l.CreatedAt)
                .Select(l => new LoadResponseDto
                {
                    LoadId = l.LoadId,
                    LoadRef = l.LoadRef,
                    Region = l.Region,
                    Stops = l.Stops,
                    EstimatedHours = l.EstimatedHours,
                    EstimatedDistance = l.EstimatedDistance,
                    Priority = l.Priority,
                    Status = l.Status,
                    AssignedDriverId = l.AssignedDriverId,
                    AssignedDriverName = l.AssignedDriver != null ? l.AssignedDriver.Name : null,
                    AssignedAt = l.AssignedAt,
                    CreatedAt = l.CreatedAt
                })
                .ToListAsync();

            return Ok(data);
        }

        // =========================================================
        // GET: /api/loads/stats?region=Tambaram&status=PENDING&date=2025-12-14
        // ✅ Filter-aware stats (Option A)
        // =========================================================
        [HttpGet("stats")]
        public async Task<IActionResult> Stats(
            [FromQuery] string? region,
            [FromQuery] string? status,
            [FromQuery] DateOnly? date
        )
        {
            // ✅ Debug: confirm what values API is receiving
            Console.WriteLine($"[LoadsController.Stats] region={region}, status={status}, date={date}");

            var q = _db.Loads.AsQueryable();

            if (!string.IsNullOrWhiteSpace(region))
                q = q.Where(l => l.Region == region);

            if (!string.IsNullOrWhiteSpace(status))
                q = q.Where(l => l.Status == status);

            if (date.HasValue)
            {
                var startUtc = DateTime.SpecifyKind(
                    date.Value.ToDateTime(TimeOnly.MinValue),
                    DateTimeKind.Utc);

                var endUtc = startUtc.AddDays(1);

                q = q.Where(l => l.CreatedAt >= startUtc && l.CreatedAt < endUtc);
            }

            var total = await q.CountAsync();
            var pending = await q.CountAsync(x => x.Status == "PENDING");
            var assigned = await q.CountAsync(x => x.Status == "ASSIGNED");
            var completed = await q.CountAsync(x => x.Status == "COMPLETED");
            var highPending = await q.CountAsync(x => x.Status == "PENDING" && x.Priority == "HIGH");

            return Ok(new LoadStatsDto
            {
                TotalLoads = total,
                PendingLoads = pending,
                AssignedLoads = assigned,
                CompletedLoads = completed,
                HighPriorityPending = highPending
            });
        }

        // =========================================================
        // POST: /api/loads  (create single)
        // =========================================================
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateLoadDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Region))
                return BadRequest("Region is required");

            if (dto.Stops <= 0) return BadRequest("Stops must be > 0");
            if (dto.EstimatedDistance <= 0) return BadRequest("EstimatedDistance must be > 0");

            // Auto-calc hours if missing/0 (Chennai avg speed 20 km/h)
            var hours = dto.EstimatedHours;
            if (hours <= 0)
                hours = Math.Round(dto.EstimatedDistance / 20m, 1);

            var load = new Load
            {
                LoadRef = await GenerateLoadRefAsync(),
                Region = dto.Region.Trim(),
                Stops = dto.Stops,
                EstimatedDistance = dto.EstimatedDistance,
                EstimatedHours = hours,
                Priority = NormalizePriority(dto.Priority),
                Status = "PENDING",
                CreatedAt = DateTime.UtcNow
            };

            _db.Loads.Add(load);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Load created", loadRef = load.LoadRef, loadId = load.LoadId });
        }

        // =========================================================
        // PUT: /api/loads/{id}  (only PENDING)
        // =========================================================
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateLoadDto dto)
        {
            var load = await _db.Loads.FirstOrDefaultAsync(x => x.LoadId == id);
            if (load == null) return NotFound("Load not found");

            if (load.Status != "PENDING")
                return BadRequest("Only PENDING loads can be edited.");

            if (string.IsNullOrWhiteSpace(dto.Region))
                return BadRequest("Region is required");

            if (dto.Stops <= 0) return BadRequest("Stops must be > 0");
            if (dto.EstimatedDistance <= 0) return BadRequest("EstimatedDistance must be > 0");

            // Auto-calc hours if missing/0 (Chennai avg speed 20 km/h)
            var hours = dto.EstimatedHours;
            if (hours <= 0)
                hours = Math.Round(dto.EstimatedDistance / 20m, 1);

            load.Region = dto.Region.Trim();
            load.Stops = dto.Stops;
            load.EstimatedDistance = dto.EstimatedDistance;
            load.EstimatedHours = hours;
            load.Priority = NormalizePriority(dto.Priority);
            load.Status = "PENDING";

            await _db.SaveChangesAsync();

            return Ok(new { message = "Load updated", loadRef = load.LoadRef, loadId = load.LoadId });
        }

        // =========================================================
        // DELETE: /api/loads/{id}  (only PENDING)
        // =========================================================
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var load = await _db.Loads.FirstOrDefaultAsync(x => x.LoadId == id);
            if (load == null) return NotFound("Load not found");

            if (load.Status != "PENDING")
                return BadRequest("Only PENDING loads can be deleted.");

            _db.Loads.Remove(load);
            await _db.SaveChangesAsync();

            return Ok(new { message = "Load deleted" });
        }

        // =========================================================
        // ✅ BULK CSV UPLOAD
        // POST: /api/loads/bulk-upload
        // =========================================================
        [HttpPost("bulk-upload")]
        public async Task<IActionResult> BulkUploadCsv([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("CSV file is required.");

            if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Only .csv files are allowed.");

            int created = 0;
            int failed = 0;
            var errors = new List<string>();

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                PrepareHeaderForMatch = args => args.Header.Trim(),
                MissingFieldFound = null,
                HeaderValidated = null,
                BadDataFound = null
            };

            using var stream = file.OpenReadStream();
            using var reader = new StreamReader(stream);
            using var csv = new CsvReader(reader, config);

            try
            {
                var rows = csv.GetRecords<BulkLoadCsvRow>().ToList();

                var (prefix, seq) = await GetLoadRefSeedAsync();

                foreach (var r in rows)
                {
                    try
                    {
                        if (string.IsNullOrWhiteSpace(r.Region)) throw new Exception("Region is required");
                        if (r.Stops <= 0) throw new Exception("Stops must be > 0");
                        if (r.EstimatedDistance <= 0) throw new Exception("EstimatedDistance must be > 0");

                        var hours = Math.Round(r.EstimatedDistance / 20m, 1);

                        var load = new Load
                        {
                            LoadRef = $"{prefix}{seq:000}",
                            Region = r.Region.Trim(),
                            Stops = r.Stops,
                            EstimatedDistance = r.EstimatedDistance,
                            EstimatedHours = hours,
                            Priority = NormalizePriority(r.Priority),
                            Status = "PENDING",
                            CreatedAt = DateTime.UtcNow
                        };

                        seq++;

                        _db.Loads.Add(load);
                        created++;
                    }
                    catch (Exception exRow)
                    {
                        failed++;
                        errors.Add($"Row failed ({r.Region}): {exRow.Message}");
                    }
                }

                await _db.SaveChangesAsync();

                return Ok(new { message = "Bulk upload completed", created, failed, errors });
            }
            catch (Exception ex)
            {
                return BadRequest($"CSV parsing failed: {ex.Message}");
            }
        }

        // =========================================================
        // OPTIONAL: BULK JSON
        // POST: /api/loads/bulk
        // =========================================================
        [HttpPost("bulk")]
        public async Task<IActionResult> BulkCreateJson([FromBody] List<CreateLoadDto> dtos)
        {
            if (dtos == null || dtos.Count == 0)
                return BadRequest("No loads provided.");

            int created = 0;

            var (prefix, seq) = await GetLoadRefSeedAsync();

            foreach (var dto in dtos)
            {
                if (string.IsNullOrWhiteSpace(dto.Region)) continue;
                if (dto.Stops <= 0) continue;
                if (dto.EstimatedDistance <= 0) continue;

                var hours = dto.EstimatedHours;
                if (hours <= 0)
                    hours = Math.Round(dto.EstimatedDistance / 20m, 1);

                var load = new Load
                {
                    LoadRef = $"{prefix}{seq:000}",
                    Region = dto.Region.Trim(),
                    Stops = dto.Stops,
                    EstimatedDistance = dto.EstimatedDistance,
                    EstimatedHours = hours,
                    Priority = NormalizePriority(dto.Priority),
                    Status = "PENDING",
                    CreatedAt = DateTime.UtcNow
                };

                seq++;

                _db.Loads.Add(load);
                created++;
            }

            await _db.SaveChangesAsync();
            return Ok(new { message = "Bulk JSON upload completed", created });
        }

        // =========================================================
        // Helpers
        // =========================================================
        private static string NormalizePriority(string p)
        {
            if (string.IsNullOrWhiteSpace(p)) return "MEDIUM";
            p = p.Trim().ToUpper();
            return p is "HIGH" or "MEDIUM" or "LOW" ? p : "MEDIUM";
        }

        private async Task<string> GenerateLoadRefAsync()
        {
            var today = DateTime.UtcNow.ToString("yyyyMMdd");
            var prefix = $"LD-{today}-";

            var last = await _db.Loads
                .Where(l => l.LoadRef.StartsWith(prefix))
                .OrderByDescending(l => l.LoadRef)
                .Select(l => l.LoadRef)
                .FirstOrDefaultAsync();

            var seq = 1;
            if (!string.IsNullOrWhiteSpace(last))
            {
                var parts = last.Split('-');
                if (parts.Length == 3 && int.TryParse(parts[2], out var n))
                    seq = n + 1;
            }

            return $"{prefix}{seq:000}";
        }

        private async Task<(string prefix, int startSeq)> GetLoadRefSeedAsync()
        {
            var today = DateTime.UtcNow.ToString("yyyyMMdd");
            var prefix = $"LD-{today}-";

            var last = await _db.Loads
                .Where(l => l.LoadRef.StartsWith(prefix))
                .OrderByDescending(l => l.LoadRef)
                .Select(l => l.LoadRef)
                .FirstOrDefaultAsync();

            var seq = 1;
            if (!string.IsNullOrWhiteSpace(last))
            {
                var parts = last.Split('-');
                if (parts.Length == 3 && int.TryParse(parts[2], out var n))
                    seq = n + 1;
            }

            return (prefix, seq);
        }
    }

    public class BulkLoadCsvRow
    {
        public string Region { get; set; } = "";
        public int Stops { get; set; }
        public decimal EstimatedDistance { get; set; }
        public string Priority { get; set; } = "MEDIUM";
    }
}
