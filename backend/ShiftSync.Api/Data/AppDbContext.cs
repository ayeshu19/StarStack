using Microsoft.EntityFrameworkCore;
using ShiftSync.Api.Models;

namespace ShiftSync.Api.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options)
            : base(options)
        {
        }

        public DbSet<AdminUser> AdminUsers { get; set; } = null!;
        public DbSet<Driver> Drivers { get; set; } = null!;
        public DbSet<Attendance> Attendances { get; set; } = null!;
        public DbSet<ShiftAssignment> ShiftAssignments { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // -------------------------
            // ADMIN USER MAPPING
            // -------------------------
            modelBuilder.Entity<AdminUser>(entity =>
            {
                entity.ToTable("admin_users");

                entity.HasKey(a => a.AdminId).HasName("pk_admin_users");

                entity.Property(a => a.AdminId).HasColumnName("admin_id");
                entity.Property(a => a.Username).HasColumnName("username");
                entity.Property(a => a.Email).HasColumnName("email");
                entity.Property(a => a.PasswordHash).HasColumnName("password_hash");
                entity.Property(a => a.FullName).HasColumnName("full_name");
                entity.Property(a => a.Role).HasColumnName("role");
                entity.Property(a => a.IsActive).HasColumnName("is_active");
                entity.Property(a => a.CreatedAt).HasColumnName("created_at");
                entity.Property(a => a.LastLogin).HasColumnName("last_login");

                entity.HasIndex(a => a.Username).IsUnique();
                entity.HasIndex(a => a.Email).IsUnique();
            });

            // -------------------------
            // DRIVER MAPPING
            // -------------------------
            modelBuilder.Entity<Driver>(entity =>
            {
                entity.ToTable("drivers");

                entity.HasKey(d => d.DriverId).HasName("pk_drivers");

                entity.Property(d => d.DriverId).HasColumnName("driver_id");
                entity.Property(d => d.Name).HasColumnName("name");
                entity.Property(d => d.Phone).HasColumnName("phone");
                entity.Property(d => d.Email).HasColumnName("email");
                entity.Property(d => d.Region).HasColumnName("region");
                entity.Property(d => d.PasswordHash).HasColumnName("password_hash");
                entity.Property(d => d.CreatedAt).HasColumnName("created_at");
                entity.Property(d => d.UpdatedAt).HasColumnName("updated_at");
                entity.Property(d => d.FatigueScore).HasColumnName("fatigue_score");
                entity.Property(d => d.Status).HasColumnName("status");
                entity.Property(d => d.VehicleType).HasColumnName("vehicle_type");
                entity.Property(d => d.WeeklyOff).HasColumnName("weekly_off");

                entity.HasIndex(d => d.Phone).IsUnique();
                entity.HasIndex(d => d.Email).IsUnique();
            });

            // -------------------------
            // ATTENDANCE MAPPING
            // -------------------------
            modelBuilder.Entity<Attendance>(entity =>
            {
                entity.ToTable("attendances");

                entity.HasKey(a => a.AttendanceId).HasName("pk_attendances");

                entity.Property(a => a.AttendanceId).HasColumnName("attendance_id");
                entity.Property(a => a.DriverId).HasColumnName("driver_id");
                entity.Property(a => a.Date).HasColumnName("date");
                entity.Property(a => a.CheckInTime).HasColumnName("check_in_time");
                entity.Property(a => a.CheckOutTime).HasColumnName("check_out_time");
                entity.Property(a => a.TotalHours).HasColumnName("total_hours");
                entity.Property(a => a.IsAbsent).HasColumnName("is_absent");
                entity.Property(a => a.CreatedAt).HasColumnName("created_at");

                entity.HasOne(a => a.Driver)
                      .WithMany()
                      .HasForeignKey(a => a.DriverId);
            });

            // -------------------------
            // SHIFT ASSIGNMENT MAPPING
            // -------------------------
            modelBuilder.Entity<ShiftAssignment>(entity =>
            {
                entity.ToTable("shift_assignments");

                entity.HasKey(s => s.AssignmentId).HasName("pk_shift_assignments");

                entity.Property(s => s.AssignmentId).HasColumnName("assignment_id");
                entity.Property(s => s.DriverId).HasColumnName("driver_id");
                entity.Property(s => s.LoadRef).HasColumnName("load_ref");
                entity.Property(s => s.Status).HasColumnName("status");
                entity.Property(s => s.AssignedDate).HasColumnName("assigned_date");
                entity.Property(s => s.CreatedAt).HasColumnName("created_at");

                entity.HasOne(s => s.Driver)
                      .WithMany()
                      .HasForeignKey(s => s.DriverId);
            });
        }
    }
}
