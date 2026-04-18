using Microsoft.EntityFrameworkCore;
using St10439541_PROG7311_P2.Models;

namespace St10439541_PROG7311_P2.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Client> Clients { get; set; }
        public DbSet<Contract> Contracts { get; set; }
        public DbSet<ServiceRequest> ServiceRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships
            modelBuilder.Entity<Client>()
                .HasMany(c => c.Contracts)
                .WithOne(c => c.Client)
                .HasForeignKey(c => c.ClientId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Contract>()
                .HasMany(c => c.ServiceRequests)
                .WithOne(s => s.Contract)
                .HasForeignKey(s => s.ContractId)
                .OnDelete(DeleteBehavior.Cascade);

            // Configure decimal precision
            modelBuilder.Entity<ServiceRequest>()
                .Property(s => s.AmountUSD)
                .HasPrecision(18, 2);

            modelBuilder.Entity<ServiceRequest>()
                .Property(s => s.AmountZAR)
                .HasPrecision(18, 2);

            modelBuilder.Entity<ServiceRequest>()
                .Property(s => s.ExchangeRateUsed)
                .HasPrecision(18, 4);

            // Seed some sample data
            modelBuilder.Entity<Client>().HasData(
                new Client { ClientId = 1, Name = "TechMove Solutions", ContactEmail = "contact@techmove.com", ContactPhone = "+27 11 123 4567", Address = "123 Main St, Johannesburg", Region = "Gauteng" },
                new Client { ClientId = 2, Name = "Global Logistics Inc", ContactEmail = "info@globallogistics.com", ContactPhone = "+27 21 987 6543", Address = "45 Beach Rd, Cape Town", Region = "Western Cape" }
            );

            modelBuilder.Entity<Contract>().HasData(
                new Contract { ContractId = 1, ClientId = 1, StartDate = new DateTime(2024, 1, 1), EndDate = new DateTime(2024, 12, 31), Status = ContractStatus.Active, ServiceLevel = ServiceLevel.Premium },
                new Contract { ContractId = 2, ClientId = 2, StartDate = new DateTime(2023, 6, 1), EndDate = new DateTime(2024, 5, 31), Status = ContractStatus.Active, ServiceLevel = ServiceLevel.Standard },
                new Contract { ContractId = 3, ClientId = 1, StartDate = new DateTime(2023, 1, 1), EndDate = new DateTime(2023, 12, 31), Status = ContractStatus.Expired, ServiceLevel = ServiceLevel.Enterprise }
            );
        }
    }
}