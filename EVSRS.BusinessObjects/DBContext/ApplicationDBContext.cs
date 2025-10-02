using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bogus;
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
        public DbSet<ApplicationRole> Roles { get; set; }
        public DbSet<Model> CarEVs { get; set; }
        public DbSet<CarEV> CarEVDetails { get; set; }
        public DbSet<CarManufacture> CarManufactures { get; set; }
        public DbSet<Depot> Depots { get; set; }
        public DbSet<Feedback> Feedbacks { get; set; }
        public DbSet<HandoverInspection> HandoverInspections { get; set; }
        public DbSet<IdentifyDocument> IdentifyDocuments { get; set; }
        public DbSet<InspectionDamageItem> InspectionDamageItems { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<OTP> OTPs { get; set; }
        public DbSet<ReturnSettlement> ReturnSettlements { get; set; }
        public DbSet<Transaction> Transactions { get; set; }
        public DbSet<VoucherBatch> VoucherBatch { get; set; }
        public DbSet<VoucherDiscount> VoucherDiscount { get; set; }



        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            #region Confiure Table Names
            modelBuilder.Entity<ApplicationUser>().ToTable("User");
            modelBuilder.Entity<OrderBooking>().ToTable("OrderBooking");
            modelBuilder.Entity<Amenities>().ToTable("Amenities");
            modelBuilder.Entity<ApplicationRole>().ToTable("Role");
            modelBuilder.Entity<Model>().ToTable("Model");
            modelBuilder.Entity<CarEV>().ToTable("CarEV");
            modelBuilder.Entity<CarManufacture>().ToTable("CarManufacture");
            modelBuilder.Entity<Depot>().ToTable("Depot");
            modelBuilder.Entity<Feedback>().ToTable("Feedback");
            modelBuilder.Entity<HandoverInspection>().ToTable("HandoverInspection");
            modelBuilder.Entity<IdentifyDocument>().ToTable("IdentifyDocument");
            modelBuilder.Entity<InspectionDamageItem>().ToTable("InspectionDamageItem");
            modelBuilder.Entity<Notification>().ToTable("Notification");
            modelBuilder.Entity<OTP>().ToTable("OTP");
            modelBuilder.Entity<ReturnSettlement>().ToTable("ReturnSettlement");
            modelBuilder.Entity<SettlementItem>().ToTable("SettlementItem");
            modelBuilder.Entity<Transaction>().ToTable("Transaction");
            modelBuilder.Entity<VoucherBatch>().ToTable("VoucherBatch");
            modelBuilder.Entity<VoucherDiscount>().ToTable("VoucherDiscount");
            #endregion

            #region Configure Fluent Api

            modelBuilder.Entity<ApplicationUser>(options =>
            {
                options.HasOne(u => u.Role)
                    .WithMany(r => r.Users)
                    .HasForeignKey(u => u.Id)
                    .OnDelete(DeleteBehavior.NoAction);

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

                
                options.HasMany(u => u.VoucherDiscounts)
                    .WithOne(m => m.User)
                    .HasForeignKey(m => m.UserId)
                    .OnDelete(DeleteBehavior.NoAction);

                options.HasMany(u => u.IdentifyDocuments)
                    .WithOne(l => l.User)
                    .HasForeignKey(l => l.UserId)
                    .OnDelete(DeleteBehavior.NoAction);


            });

            modelBuilder.Entity<OrderBooking>(options => {
                options.HasOne()
            });

        }
    }
}
