using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bogus;
using EVSRS.BusinessObjects.Datas;
using EVSRS.BusinessObjects.Entity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;


namespace EVSRS.BusinessObjects.DBContext
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext()
        {
        }
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<ApplicationUser> Users { get; set; }
        public DbSet<OrderBooking> Bookings { get; set; }
        public DbSet<Amenities> Amenities { get; set; }
        public DbSet<Model> Models { get; set; }
        public DbSet<CarEV> CarEVs { get; set; }
        public DbSet<CarManufacture> CarManufactures { get; set; }
        public DbSet<Depot> Depots { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }
        public DbSet<HandoverInspection> HandoverInspections { get; set; }
        public DbSet<IdentifyDocument> IdentifyDocuments { get; set; }
        // public DbSet<InspectionDamageItem> InspectionDamageItems { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<OTP> OTPs { get; set; }
        public DbSet<ReturnSettlement> ReturnSettlements { get; set; }
        public DbSet<SettlementItem> SettlementItems { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<Contract> Contracts { get; set; }
        public DbSet<ApplicationUserToken> ApplicationUserTokens { get; set; }
        public DbSet<SystemConfig> SystemConfigs { get; set; }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            #region Confiure Table Names
            modelBuilder.Entity<ApplicationUser>().ToTable("User");
            modelBuilder.Entity<OrderBooking>().ToTable("OrderBooking");
            modelBuilder.Entity<Amenities>().ToTable("Amenities");
            modelBuilder.Entity<Model>().ToTable("Model");
            modelBuilder.Entity<CarEV>().ToTable("CarEV");
            modelBuilder.Entity<CarManufacture>().ToTable("CarManufacture");
            modelBuilder.Entity<Depot>().ToTable("Depot");
            modelBuilder.Entity<Feedback>().ToTable("Feedback");
            modelBuilder.Entity<HandoverInspection>().ToTable("HandoverInspection");
            modelBuilder.Entity<IdentifyDocument>().ToTable("IdentifyDocument");
            modelBuilder.Entity<Notification>().ToTable("Notification");
            modelBuilder.Entity<OTP>().ToTable("OTP");
            modelBuilder.Entity<ReturnSettlement>().ToTable("ReturnSettlement");
            modelBuilder.Entity<SettlementItem>().ToTable("SettlementItem");
            modelBuilder.Entity<Transaction>().ToTable("Transaction");
            modelBuilder.Entity<Contract>().ToTable("Contract");
            modelBuilder.Entity<ApplicationUserToken>().ToTable("ApplicationUserToken");
            modelBuilder.Entity<SystemConfig>().ToTable("SystemConfig");
            #endregion

            #region Configure Fluent Api

            modelBuilder.Entity<ApplicationUser>(options =>
            {
                options.HasMany(u => u.Notifications)
                    .WithOne(a => a.Receiver)
                    .HasForeignKey(u => u.UserId)
                    .OnDelete(DeleteBehavior.NoAction);

                options.HasMany(u => u.Feedbacks)
                    .WithOne(f => f.User)
                    .HasForeignKey(f => f.UserId)
                    .OnDelete(DeleteBehavior.NoAction);

                options.HasMany(u => u.OTPs)
                    .WithOne(f => f.User)
                    .HasForeignKey(f => f.UserId)
                    .OnDelete(DeleteBehavior.NoAction);

                options.HasMany(u => u.OrderBookings)
                    .WithOne(n => n.User)
                    .HasForeignKey(n => n.UserId)
                    .OnDelete(DeleteBehavior.NoAction);

                options.HasMany(u => u.IdentifyDocuments)
                    .WithOne(l => l.User)
                    .HasForeignKey(l => l.UserId)
                    .OnDelete(DeleteBehavior.NoAction);

                options.HasMany(u => u.UserTokens)
                    .WithOne(t => t.User)
                    .HasForeignKey(t => t.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                options.HasMany(u => u.Transactions)
                    .WithOne(t => t.User)
                    .HasForeignKey(t => t.UserId)
                    .OnDelete(DeleteBehavior.NoAction);

                options.HasMany(u => u.Contracts)
                    .WithOne(c => c.Users)
                    .HasForeignKey(c => c.UserId)
                    .OnDelete(DeleteBehavior.NoAction);

                // Configure relationship between ApplicationUser and Depot
                // One Depot has many ApplicationUsers (staff members)
                options.HasOne(u => u.Depot)
                    .WithMany(d => d.ApplicationUsers)
                    .HasForeignKey(u => u.DepotId)
                    .OnDelete(DeleteBehavior.SetNull); // Set null when depot is deleted

            });

            modelBuilder.Entity<OrderBooking>(options =>
            {
                options.HasMany(o => o.Feedbacks)
                    .WithOne(f => f.OrderBooking)
                    .HasForeignKey(f => f.OrderBookingId)
                    .OnDelete(DeleteBehavior.NoAction);

                options.HasMany(o => o.HandoverInspections)
                    .WithOne(h => h.OrderBooking)
                    .HasForeignKey(h => h.OrderBookingId)
                    .OnDelete(DeleteBehavior.NoAction);

                options.HasMany(o => o.Transactions)
                    .WithOne(t => t.OrderBooking)
                    .HasForeignKey(t => t.OrderBookingId)
                    .OnDelete(DeleteBehavior.NoAction);

                options.HasOne(o => o.ReturnSettlement)
                    .WithOne(r => r.OrderBooking)
                    .HasForeignKey<ReturnSettlement>(r => r.OrderBookingId)
                    .OnDelete(DeleteBehavior.NoAction);

                options.HasOne(o => o.CarEvs)
                    .WithMany(c => c.OrderBookings)
                    .HasForeignKey(o => o.CarEVDetailId)
                    .OnDelete(DeleteBehavior.NoAction);

                options.HasOne(o => o.Depot)
                    .WithMany(d => d.OrderBookings)
                    .HasForeignKey(o => o.DepotId)
                    .OnDelete(DeleteBehavior.NoAction);
                options.HasMany(o => o.Contracts)
                    .WithOne(c => c.OrderBooking)
                    .HasForeignKey(c => c.OrderBookingId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<CarEV>(options =>
            {
                options.HasOne(c => c.Model)
                    .WithMany(m => m.CarEVDetails)
                    .HasForeignKey(c => c.ModelId)
                    .OnDelete(DeleteBehavior.NoAction);

                options.HasOne(c => c.Depot)
                    .WithMany(d => d.Details)
                    .HasForeignKey(c => c.DepotId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<Model>(options =>
            {
                options.HasOne(m => m.CarManufacture)
                    .WithMany(cm => cm.Models)
                    .HasForeignKey(m => m.ManufacturerCarId)
                    .OnDelete(DeleteBehavior.NoAction);

                options.HasOne(m => m.Amenities)
                    .WithMany(a => a.Models)
                    .HasForeignKey(m => m.AmenitiesId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<Transaction>(options =>
            {
                options.HasOne(t => t.User)
                    .WithMany()
                    .HasForeignKey(t => t.UserId)
                    .OnDelete(DeleteBehavior.NoAction);
            });

            modelBuilder.Entity<ReturnSettlement>(options =>
            {
                options.HasMany(r => r.SettlementItems)
                    .WithOne(s => s.ReturnSettlement)
                    .HasForeignKey(s => s.ReturnSettlementId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<SettlementItem>(options =>
            {
                // No need for composite key, using BaseEntity Id
            });

            // Configure many-to-many relationship between Depot and OrderBooking
            // Removed as OrderBooking now has direct foreign key to Depot
            #endregion
            
            // Seed initial data
            SeedData.Seed(modelBuilder);

        }

    }
}