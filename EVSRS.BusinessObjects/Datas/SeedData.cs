using EVSRS.BusinessObjects.Entity;
using EVSRS.BusinessObjects.Enum;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace EVSRS.BusinessObjects.Datas
{
    public static class SeedData
    {
        public static void Seed(ModelBuilder modelBuilder)
        {
            // Seed Users
            SeedUsers(modelBuilder);

            // Seed Car Manufacturers
            SeedCarManufactures(modelBuilder);

            // Seed Amenities
            SeedAmenities(modelBuilder);

            // Seed Depots
            SeedDepots(modelBuilder);

            // Seed Models
            SeedModels(modelBuilder);

            // Seed CarEVs
            SeedCarEVs(modelBuilder);
        }

        private static string GenerateSalt()
        {
            byte[] saltBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }
            return Convert.ToBase64String(saltBytes);
        }

        private static string HashPassword(string password, string salt)
        {
            var saltBytes = Convert.FromBase64String(salt);
            var hashBytes = new Rfc2898DeriveBytes(password, saltBytes, 20000, HashAlgorithmName.SHA256).GetBytes(32);
            return Convert.ToBase64String(hashBytes);
        }

        private static void SeedUsers(ModelBuilder modelBuilder)
        {
            var adminId = Guid.NewGuid().ToString("N");
            var staffId = Guid.NewGuid().ToString("N");
            var userId = Guid.NewGuid().ToString("N");

            var adminSalt = GenerateSalt();
            var staffSalt = GenerateSalt();
            var userSalt = GenerateSalt();

            var users = new List<ApplicationUser>
        {
            new ApplicationUser
            {
                Id = adminId,
                UserName = "admin",
                UserEmail = "admin@evsrs.com",
                HashPassword = HashPassword("Admin123!", adminSalt),
                Salt = adminSalt,
                FullName = "System Administrator",
                PhoneNumber = "0123456789",
                Role = Role.ADMIN,
                IsVerify = true,
                DateOfBirth = "1990-01-01",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new ApplicationUser
            {
                Id = staffId,
                UserName = "staff001",
                UserEmail = "staff@evsrs.com",
                HashPassword = HashPassword("Staff123!", staffSalt),
                Salt = staffSalt,
                FullName = "Staff Member",
                PhoneNumber = "0123456788",
                Role = Role.STAFF,
                IsVerify = true,
                DateOfBirth = "1995-05-15",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new ApplicationUser
            {
                Id = userId,
                UserName = "user001",
                UserEmail = "user@evsrs.com",
                HashPassword = HashPassword("User123!", userSalt),
                Salt = userSalt,
                FullName = "Demo User",
                PhoneNumber = "0123456787",
                Role = Role.USER,
                IsVerify = true,
                DateOfBirth = "1992-12-20",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            }
        };

            modelBuilder.Entity<ApplicationUser>().HasData(users);
        }

        private static void SeedCarManufactures(ModelBuilder modelBuilder)
        {
            var teslaId = Guid.NewGuid().ToString("N");
            var vinfastId = Guid.NewGuid().ToString("N");
            var bmwId = Guid.NewGuid().ToString("N");
            var mercedesId = Guid.NewGuid().ToString("N");
            var audiId = Guid.NewGuid().ToString("N");

            var manufacturers = new List<CarManufacture>
        {
            new CarManufacture
            {
                Id = teslaId,
                Name = "Tesla",
                Logo = "https://logos-world.net/wp-content/uploads/2021/03/Tesla-Logo.png",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new CarManufacture
            {
                Id = vinfastId,
                Name = "VinFast",
                Logo = "https://vinfast.vn/wp-content/uploads/2023/03/vinfast-logo.png",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new CarManufacture
            {
                Id = bmwId,
                Name = "BMW",
                Logo = "https://logos-world.net/wp-content/uploads/2020/04/BMW-Logo.png",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new CarManufacture
            {
                Id = mercedesId,
                Name = "Mercedes-Benz",
                Logo = "https://logos-world.net/wp-content/uploads/2020/04/Mercedes-Benz-Logo.png",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new CarManufacture
            {
                Id = audiId,
                Name = "Audi",
                Logo = "https://logos-world.net/wp-content/uploads/2021/03/Audi-Logo.png",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            }
        };

            modelBuilder.Entity<CarManufacture>().HasData(manufacturers);

            // Store IDs for use in other seed methods
            _teslaId = teslaId;
            _vinfastId = vinfastId;
            _bmwId = bmwId;
        }

        // Static fields to store IDs for relationships
        private static string _teslaId = "";
        private static string _vinfastId = "";
        private static string _bmwId = "";
        private static string _gpsAmenityId = "";
        private static string _bluetoothAmenityId = "";
        private static string _acAmenityId = "";
        private static string _depot1Id = "";
        private static string _depot3Id = "";

        private static void SeedAmenities(ModelBuilder modelBuilder)
        {
            _gpsAmenityId = Guid.NewGuid().ToString("N");
            _bluetoothAmenityId = Guid.NewGuid().ToString("N");
            _acAmenityId = Guid.NewGuid().ToString("N");
            var heatedSeatsId = Guid.NewGuid().ToString("N");
            var backupCameraId = Guid.NewGuid().ToString("N");
            var sunroofId = Guid.NewGuid().ToString("N");

            var amenities = new List<Amenities>
        {
            new Amenities
            {
                Id = _gpsAmenityId,
                Name = "GPS Navigation",
                Icon = "🗺️",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new Amenities
            {
                Id = _bluetoothAmenityId,
                Name = "Bluetooth",
                Icon = "📶",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new Amenities
            {
                Id = _acAmenityId,
                Name = "Air Conditioning",
                Icon = "❄️",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new Amenities
            {
                Id = heatedSeatsId,
                Name = "Heated Seats",
                Icon = "🔥",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new Amenities
            {
                Id = backupCameraId,
                Name = "Backup Camera",
                Icon = "📹",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new Amenities
            {
                Id = sunroofId,
                Name = "Sunroof",
                Icon = "☀️",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            }
        };

            modelBuilder.Entity<Amenities>().HasData(amenities);
        }

        private static void SeedDepots(ModelBuilder modelBuilder)
        {
            _depot1Id = Guid.NewGuid().ToString("N");
            var depot2Id = Guid.NewGuid().ToString("N");
            _depot3Id = Guid.NewGuid().ToString("N");

            var depots = new List<Depot>
        {
            new Depot
            {
                Id = _depot1Id,
                Name = "EVSRS Depot - District 1",
                MapId = "depot_001",
                Province = "Ho Chi Minh City",
                District = "District 1",
                Ward = "Ben Nghe Ward",
                Street = "123 Nguyen Hue Street",
                Lattitude = "10.7769",
                Longitude = "106.7009",
                OpenTime = "06:00",
                CloseTime = "22:00",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new Depot
            {
                Id = depot2Id,
                Name = "EVSRS Depot - District 3",
                MapId = "depot_002",
                Province = "Ho Chi Minh City",
                District = "District 3",
                Ward = "Ward 5",
                Street = "456 Vo Van Tan Street",
                Lattitude = "10.7756",
                Longitude = "106.6887",
                OpenTime = "06:00",
                CloseTime = "22:00",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new Depot
            {
                Id = _depot3Id,
                Name = "EVSRS Depot - Tan Binh",
                MapId = "depot_003",
                Province = "Ho Chi Minh City",
                District = "Tan Binh District",
                Ward = "Ward 2",
                Street = "789 Cong Hoa Street",
                Lattitude = "10.8006",
                Longitude = "106.6534",
                OpenTime = "06:00",
                CloseTime = "22:00",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            }
        };

            modelBuilder.Entity<Depot>().HasData(depots);
        }

        private static void SeedModels(ModelBuilder modelBuilder)
        {
            var model3Id = Guid.NewGuid().ToString("N");
            var modelYId = Guid.NewGuid().ToString("N");
            var vf8Id = Guid.NewGuid().ToString("N");
            var vf9Id = Guid.NewGuid().ToString("N");
            var ix3Id = Guid.NewGuid().ToString("N");

            var models = new List<Model>
        {
            new Model
            {
                Id = model3Id,
                ModelName = "Model 3",
                ManufacturerCarId = _teslaId,
                AmenitiesId = _gpsAmenityId,
                BatteryCapacityKwh = "75",
                RangeKm = "448",
                LimiteDailyKm = "200",
                Seats = "5",
                Price = 50000,
                Sale = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new Model
            {
                Id = modelYId,
                ModelName = "Model Y",
                ManufacturerCarId = _teslaId,
                AmenitiesId = _bluetoothAmenityId,
                BatteryCapacityKwh = "75",
                RangeKm = "533",
                LimiteDailyKm = "200",
                Seats = "7",
                Price = 60000,
                Sale = 5,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new Model
            {
                Id = vf8Id,
                ModelName = "VF 8",
                ManufacturerCarId = _vinfastId,
                AmenitiesId = _acAmenityId,
                BatteryCapacityKwh = "87.7",
                RangeKm = "420",
                LimiteDailyKm = "180",
                Seats = "5",
                Price = 45000,
                Sale = 10,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new Model
            {
                Id = vf9Id,
                ModelName = "VF 9",
                ManufacturerCarId = _vinfastId,
                AmenitiesId = _gpsAmenityId,
                BatteryCapacityKwh = "123",
                RangeKm = "438",
                LimiteDailyKm = "200",
                Seats = "7",
                Price = 65000,
                Sale = 0,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new Model
            {
                Id = ix3Id,
                ModelName = "iX3",
                ManufacturerCarId = _bmwId,
                AmenitiesId = _bluetoothAmenityId,
                BatteryCapacityKwh = "74",
                RangeKm = "460",
                LimiteDailyKm = "190",
                Seats = "5",
                Price = 55000,
                Sale = 8,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            }
        };

            modelBuilder.Entity<Model>().HasData(models);

            // Store model IDs for CarEV seeding
            _model3Id = model3Id;
            _modelYId = modelYId;
            _vf8Id = vf8Id;
        }

        private static string _model3Id = "";
        private static string _modelYId = "";
        private static string _vf8Id = "";

        private static void SeedCarEVs(ModelBuilder modelBuilder)
        {
            var carEVs = new List<CarEV>
        {
            new CarEV
            {
                Id = Guid.NewGuid().ToString("N"),
                ModelId = _model3Id,
                DepotId = _depot1Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new CarEV
            {
                Id = Guid.NewGuid().ToString("N"),
                ModelId = _model3Id,
                DepotId = _depot1Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new CarEV
            {
                Id = Guid.NewGuid().ToString("N"),
                ModelId = _modelYId,
                DepotId = _depot3Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new CarEV
            {
                Id = Guid.NewGuid().ToString("N"),
                ModelId = _vf8Id,
                DepotId = _depot1Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            },
            new CarEV
            {
                Id = Guid.NewGuid().ToString("N"),
                ModelId = _vf8Id,
                DepotId = _depot3Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsDeleted = false
            }
        };

            modelBuilder.Entity<CarEV>().HasData(carEVs);
        }
    }
}

