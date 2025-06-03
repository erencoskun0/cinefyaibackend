using Microsoft.EntityFrameworkCore;
using CinefyAiServer.Entities;
using CinefyAiServer.Entities.DTOs;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Text.Json;

namespace CinefyAiServer.Data
{
    /// <summary>
    /// Uygulamanın ana DbContext'i. Tüm entity'ler ve ilişkiler burada tanımlanır.
    /// </summary>
    public class ApplicationDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        // DbSets
        public DbSet<Cinema> Cinemas { get; set; }
        public DbSet<Hall> Halls { get; set; }
        public DbSet<Seat> Seats { get; set; }
        public DbSet<Session> Sessions { get; set; }
        public DbSet<Booking> Bookings { get; set; }
        public DbSet<Movie> Movies { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Analytics> Analytics { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Table names
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<Cinema>().ToTable("Cinemas");
            modelBuilder.Entity<Hall>().ToTable("Halls");
            modelBuilder.Entity<Seat>().ToTable("Seats");
            modelBuilder.Entity<Session>().ToTable("Sessions");
            modelBuilder.Entity<Booking>().ToTable("Bookings");
            modelBuilder.Entity<Movie>().ToTable("Movies");
            modelBuilder.Entity<Review>().ToTable("Reviews");
            modelBuilder.Entity<Analytics>().ToTable("Analytics");
            modelBuilder.Entity<ChatMessage>().ToTable("ChatMessages");

            // JSON field configurations
            ConfigureJsonFields(modelBuilder);

            // Entity relationships
            ConfigureRelationships(modelBuilder);

            // Indexes for performance
            ConfigureIndexes(modelBuilder);

            // Enum conversions
            ConfigureEnums(modelBuilder);

            // Auto-update timestamps
            ConfigureTimestamps(modelBuilder);
        }

        private void ConfigureJsonFields(ModelBuilder modelBuilder)
        {
            // Cinema JSON fields
            modelBuilder.Entity<Cinema>()
                .Property(e => e.Facilities)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>()
                )
                .HasColumnType("nvarchar(max)");

            modelBuilder.Entity<Cinema>()
                .Property(e => e.Features)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>()
                )
                .HasColumnType("nvarchar(max)");

            modelBuilder.Entity<Cinema>()
                .Property(e => e.OpeningHours)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, string>()
                )
                .HasColumnType("nvarchar(max)");

            // Movie JSON fields
            modelBuilder.Entity<Movie>()
                .Property(e => e.Genre)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>()
                )
                .HasColumnType("nvarchar(max)");

            modelBuilder.Entity<Movie>()
                .Property(e => e.Cast)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>()
                )
                .HasColumnType("nvarchar(max)");

            // Hall JSON fields
            modelBuilder.Entity<Hall>()
                .Property(e => e.SeatLayout)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, object>()
                )
                .HasColumnType("nvarchar(max)");

            modelBuilder.Entity<Hall>()
                .Property(e => e.Features)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>()
                )
                .HasColumnType("nvarchar(max)");

            // Booking JSON fields
            modelBuilder.Entity<Booking>()
                .Property(e => e.SelectedSeats)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<SeatSelection>>(v, (JsonSerializerOptions?)null) ?? new List<SeatSelection>()
                )
                .HasColumnType("nvarchar(max)");

            // Analytics JSON fields
            modelBuilder.Entity<Analytics>()
                .Property(e => e.Metrics)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<Dictionary<string, object>>(v, (JsonSerializerOptions?)null) ?? new Dictionary<string, object>()
                )
                .HasColumnType("nvarchar(max)");
        }

        private void ConfigureRelationships(ModelBuilder modelBuilder)
        {
            // Cinema - User (Owner) relationship
            modelBuilder.Entity<Cinema>()
                .HasOne(c => c.Owner)
                .WithMany(u => u.OwnedCinemas)
                .HasForeignKey(c => c.OwnerId)
                .OnDelete(DeleteBehavior.SetNull);

            // Hall - Cinema relationship
            modelBuilder.Entity<Hall>()
                .HasOne(h => h.Cinema)
                .WithMany(c => c.Halls)
                .HasForeignKey(h => h.CinemaId)
                .OnDelete(DeleteBehavior.Cascade);

            // Session relationships
            modelBuilder.Entity<Session>()
                .HasOne(s => s.Movie)
                .WithMany(m => m.Sessions)
                .HasForeignKey(s => s.MovieId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Session>()
                .HasOne(s => s.Hall)
                .WithMany(h => h.Sessions)
                .HasForeignKey(s => s.HallId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Session>()
                .HasOne(s => s.Cinema)
                .WithMany(c => c.Sessions)
                .HasForeignKey(s => s.CinemaId)
                .OnDelete(DeleteBehavior.Restrict);

            // Booking relationships
            modelBuilder.Entity<Booking>()
                .HasOne(b => b.Session)
                .WithMany(s => s.Bookings)
                .HasForeignKey(b => b.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Booking>()
                .HasOne(b => b.User)
                .WithMany(u => u.Bookings)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Review relationships
            modelBuilder.Entity<Review>()
                .HasOne(r => r.Cinema)
                .WithMany(c => c.Reviews)
                .HasForeignKey(r => r.CinemaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Movie)
                .WithMany(m => m.Reviews)
                .HasForeignKey(r => r.MovieId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.User)
                .WithMany(u => u.Reviews)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            // Analytics relationships
            modelBuilder.Entity<Analytics>()
                .HasOne(a => a.Cinema)
                .WithMany(c => c.Analytics)
                .HasForeignKey(a => a.CinemaId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Analytics>()
                .HasOne(a => a.Session)
                .WithMany(s => s.Analytics)
                .HasForeignKey(a => a.SessionId)
                .OnDelete(DeleteBehavior.Cascade);

            // ChatMessage relationships
            modelBuilder.Entity<ChatMessage>()
                .HasOne(cm => cm.User)
                .WithMany(u => u.ChatMessages)
                .HasForeignKey(cm => cm.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        }

        private void ConfigureIndexes(ModelBuilder modelBuilder)
        {
            // Performance indexes
            modelBuilder.Entity<Cinema>()
                .HasIndex(c => c.City);

            modelBuilder.Entity<Cinema>()
                .HasIndex(c => c.Brand);

            modelBuilder.Entity<Movie>()
                .HasIndex(m => m.ReleaseDate);

            modelBuilder.Entity<Session>()
                .HasIndex(s => new { s.SessionDate, s.SessionTime });

            modelBuilder.Entity<Booking>()
                .HasIndex(b => b.BookingCode)
                .IsUnique();

            modelBuilder.Entity<Analytics>()
                .HasIndex(a => new { a.CinemaId, a.Date });

            modelBuilder.Entity<ChatMessage>()
                .HasIndex(cm => cm.UserId);
        }

        private void ConfigureEnums(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<User>()
                .Property(u => u.UserRole)
                .HasConversion<string>();

            modelBuilder.Entity<Session>()
                .Property(s => s.OccupancyStatus)
                .HasConversion<string>();

            modelBuilder.Entity<Booking>()
                .Property(b => b.PaymentStatus)
                .HasConversion<string>();

            modelBuilder.Entity<Booking>()
                .Property(b => b.Status)
                .HasConversion<string>();
        }

        private void ConfigureTimestamps(ModelBuilder modelBuilder)
        {
            // Auto-update timestamps on save
        }

        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateTimestamps()
        {
            var entries = ChangeTracker
                .Entries()
                .Where(e => e.Entity is Cinema || e.Entity is Movie || e.Entity is Hall ||
                           e.Entity is Session || e.Entity is Booking || e.Entity is User ||
                           e.Entity is Review)
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entityEntry in entries)
            {
                var entity = entityEntry.Entity;

                if (entityEntry.State == EntityState.Added)
                {
                    if (entity.GetType().GetProperty("CreatedAt") != null)
                        entityEntry.Property("CreatedAt").CurrentValue = DateTime.UtcNow;
                }

                if (entity.GetType().GetProperty("UpdatedAt") != null)
                    entityEntry.Property("UpdatedAt").CurrentValue = DateTime.UtcNow;
            }
        }
    }
}