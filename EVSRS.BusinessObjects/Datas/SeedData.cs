using EVSRS.BusinessObjects.Entity;
using EVSRS.BusinessObjects.Enum;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace EVSRS.BusinessObjects.Datas
{
    public static class SeedData
    {
        public static void Seed(ModelBuilder modelBuilder)
        {
            // Seed MembershipConfigs first (no FK dependencies)
            SeedMembershipConfigs(modelBuilder);

            // Seed SystemConfigs
            SeedSystemConfig(modelBuilder);

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
            modelBuilder.Entity<ApplicationUser>().HasData(new ApplicationUser
                {
                    Id = "f0f59b2db7044564a06c8528d34059cd",
                    UserName = "admin",
                    FullName = "Admin User",
                    UserEmail = "admin@evsrs.com",
                    PhoneNumber = "0123456789",
                    Role = Role.ADMIN,
                    HashPassword = "0/55m5D78ID6aDW3ILaAIJIsrN4NPcmz0/sMHGjUtwU=",
                    Salt = "+RpeKZpefqvzh9xPeparBlx3tFEb6uzJO6Zr27+Tlpg=",
                    IsVerify = true,
                    CreatedBy = "System",
                    UpdatedBy = "System",
                    CreatedAt = new DateTime(2025, 9, 2, 6, 28, 11, 442, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 9, 2, 6, 28, 11, 442, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new ApplicationUser
                {
                    Id = "19b2df0e909049aa87baf829782c5535",
                    UserName = "user",
                    FullName = "Regular User",
                    UserEmail = "user@evsrs.com",
                    PhoneNumber = "0987654321",
                    Role = Role.USER,
                    HashPassword = "FVDdFAj2L27v8bcDdbWoppGuZHlQv449dJ71M0qHetc=",
                    Salt = "B2QFgJToQP84qc2YL6cW7fpE6J1RzfcXmGTnV02mLRY=",
                    IsVerify = true,
                    CreatedBy = "System",
                    UpdatedBy = "System",
                    CreatedAt = new DateTime(2025, 9, 2, 6, 28, 11, 442, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 9, 2, 6, 28, 11, 442, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new ApplicationUser
                {
                    Id = "01a61fae-5bd3-4c9e-8103-251cd9c6e580",
                    UserName = "staff002",
                    FullName = "Staff 2",
                    UserEmail = "staff2@evsrs.com",
                    PhoneNumber = "0987654321",
                    Role = Role.STAFF,
                    HashPassword = "n/pNVwOrtkUWECIFROo7YxGv8TnuiCQTR8CTi4G1dy8=",
                    Salt = "2fqlWU3s47q+hb9tVG7ULmR6PhvDiLzaEV9x+lELThg==",
                    DepotId = "8664ebff6e944c38a801ddc357f9dac0",
                    IsVerify = true,
                    CreatedBy = "System",
                    UpdatedBy = "System",
                    CreatedAt = new DateTime(2025, 9, 2, 6, 28, 11, 442, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 9, 2, 6, 28, 11, 442, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new ApplicationUser
                {
                    Id = "97de3ca5c503497da8cf658945f70214",
                    UserName = "staffHCMTanBinh",
                    FullName = "Staff HCM TanBinh",
                    UserEmail = "staff@evsrs.com",
                    PhoneNumber = "0987654321",
                    Role = Role.STAFF,
                    HashPassword = "SfZ7xDUh0p5nOY18GXHU6nWJYixYs1Fzk5jdPxhRr1Y=",
                    Salt = "WUtN6KXtIRJYE4shDQQPuPLCHjAuCGUWIpUZOPpI3Ts=",
                    DepotId = "1ca81b37db0042c5b74092575026fcc9",
                    IsVerify = true,
                    CreatedBy = "System",
                    UpdatedBy = "System",
                    CreatedAt = new DateTime(2025, 9, 2, 6, 28, 11, 442, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 9, 2, 6, 28, 11, 442, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new ApplicationUser
                {
                    Id = "ba5baf1a345845be85c8e5212e31e913",
                    FullName = "Staff HCM Q1",
                    UserName = "staffHCMQ1",
                    UserEmail = "staffhcmq1@evsrs.com",
                    PhoneNumber = "0987654321",
                    Role = Role.STAFF,
                    HashPassword = "VvNu3hUF0sL8zOMCk27C2hRchPBqf3kDLTTcoY5hZoY=",
                    Salt = "8/GW0z/NNLSI8VaWjDKZrS9xUjKndOniLuWlxGauCJI=",
                    DepotId = "86114896990e4fab8b341a581e2ca551",
                    IsVerify = true,
                    CreatedBy = "System",
                    UpdatedBy = "System",
                    CreatedAt = new DateTime(2025, 9, 2, 6, 28, 11, 442, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 9, 2, 6, 28, 11, 442, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new ApplicationUser
                {
                    Id = "e3c14310-36a4-496c-83cf-2e3919841aac",
                    FullName = "Staff HCM Q1",
                    UserName = "staff003",
                    UserEmail = "staff3@evsrs.com",
                    PhoneNumber = "0987654321",
                    Role = Role.STAFF,
                    HashPassword = "FPrBvhJ9dplQIuf57MgUJq1y6s8N+hsXctngWjFxP1k=",
                    Salt = "TkD48+y8lIrgXjBQEgL9+nClk2nzRdersi37QVkGwb0=",
                    DepotId = "ad971561785446d1a018d5881a307d56",
                    IsVerify = true,
                    CreatedBy = "System",
                    UpdatedBy = "System",
                    CreatedAt = new DateTime(2025, 9, 2, 6, 28, 11, 442, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 9, 2, 6, 28, 11, 442, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new ApplicationUser
                {
                    Id = "80b25fb0d63d404787ba42543679b0f3",
                    FullName = "staffHNBaDinh",
                    UserName = "staffHNBaDinh",
                    UserEmail = "staffhnbadinh@evsrs.com",
                    PhoneNumber = "0987654321",
                    Role = Role.STAFF,
                    HashPassword = "RTIhSYuwI8Y0YcxfewST00b+qEugqwoLjD9Y0EzeRCY=",
                    Salt = "98S7iffGHg33P3MpY0ix5DnptTbNuFz3wQf1yRU0dy0=",
                    DepotId = "1393d516aeb84e2bace0eed3f18f8df9",
                    IsVerify = true,
                    CreatedBy = "System",
                    UpdatedBy = "System",
                    CreatedAt = new DateTime(2025, 9, 2, 6, 28, 11, 442, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 9, 2, 6, 28, 11, 442, DateTimeKind.Utc),
                    IsDeleted = false
                });
        }

        private static void SeedCarManufactures(ModelBuilder modelBuilder)
        {
            _teslaId = "0a4acfce3361489d96c3d25233e8bf1c";
            _vinfastId = "4246a9ff652849c3bed8a7a53adb705f";
            _bmwId = "a9b90b1a38374a4395fdfbadeaa3213b";
            var audiId = "31091b7a15ad461db50f3bdf51493716";
            var mercedesId = "c226f721f3b64c11b186aee836692b33";

            modelBuilder.Entity<CarManufacture>().HasData(
                new CarManufacture
                {
                    Id = audiId,
                    Name = "Audi",
                    Logo = "https://logos-world.net/wp-content/uploads/2021/03/Audi-Logo.png",
                    CreatedAt = new DateTime(2025, 10, 17, 0, 49, 43, 620, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 17, 0, 49, 43, 620, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new CarManufacture
                {
                    Id = _bmwId,
                    Name = "BMW",
                    Logo = "https://logos-world.net/wp-content/uploads/2020/04/BMW-Logo.png",
                    CreatedAt = new DateTime(2025, 10, 17, 0, 49, 43, 620, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 17, 0, 49, 43, 620, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new CarManufacture
                {
                    Id = _teslaId,
                    Name = "Tesla",
                    Logo = "https://logos-world.net/wp-content/uploads/2020/10/Tesla-Logo-700x394.png",
                    CreatedBy = "System Administrator",
                    CreatedAt = new DateTime(2025, 10, 17, 0, 49, 43, 620, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 17, 16, 18, 6, 366, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new CarManufacture
                {
                    Id = _vinfastId,
                    Name = "VinFast",
                    Logo = "https://logos-world.net/wp-content/uploads/2021/03/VinFast-Logo-Vietnam.png",
                    CreatedBy = "System Administrator",
                    CreatedAt = new DateTime(2025, 10, 17, 0, 49, 43, 620, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 19, 1, 37, 12, 274, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new CarManufacture
                {
                    Id = mercedesId,
                    Name = "Mercedes-Benz",
                    Logo = "https://logos-world.net/wp-content/uploads/2020/05/Mercedes-Benz-Logo-700x394.png",
                    CreatedBy = "System Administrator",
                    CreatedAt = new DateTime(2025, 10, 17, 0, 49, 43, 620, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 19, 1, 37, 39, 187, DateTimeKind.Utc),
                    IsDeleted = false
                });
        }

        // Static fields to store IDs for relationships
        private static string _teslaId = "";
        private static string _vinfastId = "";
        private static string _bmwId = "";
        private static string _audiId = "";
        private static string _mercedesId = "";
        private static string _gpsAmenityId = "";
        private static string _bluetoothAmenityId = "";
        private static string _acAmenityId = "";
        private static string _depot1Id = "";
        private static string _depot3Id = "";
        private static string _model3Id = "";
        private static string _modelYId = "";
        private static string _vf8Id = "";

        private static void SeedAmenities(ModelBuilder modelBuilder)
        {
            _gpsAmenityId = "ad2006c7044a4bb5aecd2171dd5907b7";
            _bluetoothAmenityId = "907d206161c64754a7d65bf43bd0b937";
            _acAmenityId = "4f2d05a1d41644a4b0915434f1c72775";
            var backupCameraId = "b503033aa1624279aae590d93de41525";
            var sunroofId = "caf8759dc8ef4237a8fd7a6291f5012b";
            var heatedSeatsId = "e828dec49c464eecbe6674c888ab0be8";
            var androidAutoId = "7e245af3742f459d98eab212ce8df96e";

            modelBuilder.Entity<Amenities>().HasData(
                new Amenities
                {
                    Id = _acAmenityId,
                    Name = "Air Conditioning",
                    Icon = "❄️",
                    CreatedAt = new DateTime(2025, 10, 17, 0, 49, 43, 620, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 17, 0, 49, 43, 620, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new Amenities
                {
                    Id = _bluetoothAmenityId,
                    Name = "Bluetooth",
                    Icon = "📶",
                    CreatedAt = new DateTime(2025, 10, 17, 0, 49, 43, 620, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 17, 0, 49, 43, 620, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new Amenities
                {
                    Id = _gpsAmenityId,
                    Name = "GPS Navigation",
                    Icon = "🗺️",
                    CreatedAt = new DateTime(2025, 10, 17, 0, 49, 43, 620, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 17, 0, 49, 43, 620, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new Amenities
                {
                    Id = backupCameraId,
                    Name = "Backup Camera",
                    Icon = "📹",
                    CreatedAt = new DateTime(2025, 10, 17, 0, 49, 43, 620, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 17, 0, 49, 43, 620, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new Amenities
                {
                    Id = sunroofId,
                    Name = "Sunroof",
                    Icon = "☀️",
                    CreatedAt = new DateTime(2025, 10, 17, 0, 49, 43, 620, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 17, 0, 49, 43, 620, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new Amenities
                {
                    Id = heatedSeatsId,
                    Name = "Heated Seats",
                    Icon = "🔥",
                    CreatedAt = new DateTime(2025, 10, 17, 0, 49, 43, 620, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 17, 0, 49, 43, 620, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new Amenities
                {
                    Id = androidAutoId,
                    Name = "Android Auto",
                    Icon = "📱",
                    CreatedBy = "System Administrator",
                    UpdatedBy = "System",
                    CreatedAt = new DateTime(2025, 10, 20, 16, 50, 38, 68, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 25, 22, 19, 36, 853, DateTimeKind.Utc),
                    IsDeleted = false
                });
        }

        private static void SeedDepots(ModelBuilder modelBuilder)
        {
            _depot1Id = "86114896990e4fab8b341a581e2ca551";
            _depot3Id = "1ca81b37db0042c5b74092575026fcc9";

            modelBuilder.Entity<Depot>().HasData(
                new Depot
                {
                    Id = "0ffe5e744c7d4a47a2614633aee37254",
                    Name = "HN Depot - Dong Da",
                    MapId = "depot_004",
                    Province = "Hà Nội",
                    District = "Đống Đa",
                    Ward = "Ward 1",
                    Street = "11 Cát Linh",
                    Lattitude = "10.8006",
                    Longitude = "106.6534",
                    OpenTime = "6:00",
                    CloseTime = "22:00",
                    CreatedBy = "System",
                    CreatedAt = new DateTime(2025, 10, 19, 1, 16, 28, 863, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 19, 1, 16, 28, 862, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new Depot
                {
                    Id = "1393d516aeb84e2bace0eed3f18f8df9",
                    Name = "HN Depot - Ba Dinh",
                    MapId = "depot_005",
                    Province = "Hà Nội",
                    District = "Ba Đình",
                    Ward = "Ward 7",
                    Street = "123 Văn An",
                    Lattitude = "10.8006",
                    Longitude = "106.6534",
                    OpenTime = "6:00",
                    CloseTime = "22:00",
                    CreatedBy = "System",
                    CreatedAt = new DateTime(2025, 10, 19, 1, 18, 50, 340, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 19, 1, 18, 50, 340, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new Depot
                {
                    Id = "ad971561785446d1a018d5881a307d56",
                    Name = "HN Depot - Hoan Kiem",
                    MapId = "depot_006",
                    Province = "Hà Nội",
                    District = "Hoàn Kiếm",
                    Ward = "Ward 10",
                    Street = "123 Đồng Xuân",
                    Lattitude = "10.8006",
                    Longitude = "106.6534",
                    OpenTime = "6:00",
                    CloseTime = "22:00",
                    CreatedBy = "System",
                    CreatedAt = new DateTime(2025, 10, 19, 1, 19, 41, 882, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 19, 1, 19, 41, 882, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new Depot
                {
                    Id = "8664ebff6e944c38a801ddc357f9dac0",
                    Name = "CT Depot - Ninh Kieu",
                    MapId = "depot_007",
                    Province = "Cần Thơ",
                    District = "Ninh Kiều",
                    Ward = "Ward 11",
                    Street = "123 Trần Phú",
                    Lattitude = "10.8006",
                    Longitude = "106.6534",
                    OpenTime = "6:00",
                    CloseTime = "22:00",
                    CreatedBy = "System",
                    CreatedAt = new DateTime(2025, 10, 19, 1, 21, 51, 48, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 19, 1, 21, 51, 48, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new Depot
                {
                    Id = "0782b3f130cb42ff9af24487234b251c",
                    Name = "CT Depot - Binh Thuy",
                    MapId = "depot_008",
                    Province = "Cần Thơ",
                    District = "Bình Thủy",
                    Ward = "Ward 4",
                    Street = "222 Võ Văn Kiệt",
                    Lattitude = "10.8006",
                    Longitude = "106.6534",
                    OpenTime = "6:00",
                    CloseTime = "22:00",
                    CreatedBy = "System",
                    CreatedAt = new DateTime(2025, 10, 19, 1, 23, 2, 157, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 19, 1, 23, 2, 157, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new Depot
                {
                    Id = _depot3Id,
                    Name = "HCM Depot - Tan Binh",
                    MapId = "depot_003",
                    Province = "TP. Hồ Chí Minh",
                    District = "Tân Bình",
                    Ward = "Ward 2",
                    Street = "789 Cộng Hòa",
                    Lattitude = "10.8006",
                    Longitude = "106.6534",
                    OpenTime = "06:00",
                    CloseTime = "22:00",
                    CreatedAt = new DateTime(2025, 10, 17, 0, 49, 43, 620, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 17, 0, 49, 43, 620, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new Depot
                {
                    Id = _depot1Id,
                    Name = "HCM Depot - Quan 1",
                    MapId = "depot_001",
                    Province = "TP. Hồ Chí Minh",
                    District = "Quận 1",
                    Ward = "Ben Nghe Ward",
                    Street = "123 Nguyễn Huệ",
                    Lattitude = "10.7769",
                    Longitude = "106.7009",
                    OpenTime = "06:00",
                    CloseTime = "22:00",
                    CreatedAt = new DateTime(2025, 10, 17, 0, 49, 43, 620, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 17, 0, 49, 43, 620, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new Depot
                {
                    Id = "b084f39355914a87a535ce632da23596",
                    Name = "HCM Depot - Quan 3",
                    MapId = "depot_002",
                    Province = "TP. Hồ Chí Minh",
                    District = "Quận 3",
                    Ward = "Ward 5",
                    Street = "456 Võ Văn Tần",
                    Lattitude = "10.7756",
                    Longitude = "106.6887",
                    OpenTime = "06:00",
                    CloseTime = "22:00",
                    CreatedAt = new DateTime(2025, 10, 17, 0, 49, 43, 620, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 17, 0, 49, 43, 620, DateTimeKind.Utc),
                    IsDeleted = false
                });
        }

        private static void SeedModels(ModelBuilder modelBuilder)
        {
            _model3Id = "337b3ed012974c85861edd4071fd1a65";
            _modelYId = "7472da3ea3d3408c9e15da574005677c";
            _vf8Id = "b402f2e2ab194ef4aa822928ecdabccf";
            var vf9Id = "1fecbc408acd4e17a5eca29f07e3686b";
            var vf3Id = "407ff9fbeac442eb95d699e93d1df032";
            var mercedesGlaId = "f967d78e0cb04b12afd5581ad3a13d64";
            var bmwIx3Id = "47344916183044178a471bb048d67469";

            modelBuilder.Entity<Model>().HasData(
                new Model
                {
                    Id = vf9Id,
                    ManufacturerCarId = _vinfastId,
                    AmenitiesId = _gpsAmenityId,
                    ModelName = "Vinfast VF9",
                    BatteryCapacityKwh = "92",
                    RangeKm = "400",
                    LimiteDailyKm = "200",
                    Seats = "7",
                    Image = "https://drive.gianhangvn.com/image/vf-9-2684881j29551.jpg",
                    Price = 65000.0,
                    Sale = 0,
                    CreatedAt = new DateTime(2025, 10, 17, 0, 49, 43, 620, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 17, 0, 49, 43, 620, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new Model
                {
                    Id = vf3Id,
                    ManufacturerCarId = _vinfastId,
                    AmenitiesId = _acAmenityId,
                    ModelName = "Vinfast VF3",
                    BatteryCapacityKwh = "30",
                    RangeKm = "300",
                    LimiteDailyKm = "200",
                    Seats = "4",
                    Image = "https://vinfastotohaiphong.com.vn/images/Upload/images/vinfast-vf3(1).jpg",
                    Price = 30000.0,
                    Sale = 10,
                    CreatedBy = "System",
                    CreatedAt = new DateTime(2025, 10, 17, 0, 49, 43, 620, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 17, 0, 49, 43, 620, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new Model
                {
                    Id = mercedesGlaId,
                    ManufacturerCarId = "c226f721f3b64c11b186aee836692b33", // Mercedes-Benz ID
                    AmenitiesId = "7e245af3742f459d98eab212ce8df96e", // Android Auto ID
                    ModelName = "Mercedes Benz GLA",
                    BatteryCapacityKwh = "16",
                    RangeKm = "400",
                    LimiteDailyKm = "200",
                    Seats = "4",
                    Image =
                        "https://hips.hearstapps.com/hmg-prod/images/2025-mercedes-benz-gla-class-101-67212f16b8417.jpg?crop=0.671xw:0.565xh;0.244xw,0.435xh&resize=2048:*",
                    Price = 55000.0,
                    Sale = 12,
                    CreatedBy = "System",
                    CreatedAt = new DateTime(2025, 10, 17, 0, 49, 43, 620, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 17, 0, 49, 43, 620, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new Model
                {
                    Id = _model3Id,
                    ManufacturerCarId = _teslaId,
                    AmenitiesId = _gpsAmenityId,
                    ModelName = "Tesla Model 3",
                    BatteryCapacityKwh = "75",
                    RangeKm = "350",
                    LimiteDailyKm = "200",
                    Seats = "4",
                    Image =
                        "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcQX7EL3fLZcWdBdVRfcTXsW2o2OFklPTRN7Hg&s",
                    Price = 50000.0,
                    Sale = 0,
                    CreatedAt = new DateTime(2025, 10, 17, 0, 49, 43, 620, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 17, 0, 49, 43, 620, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new Model
                {
                    Id = bmwIx3Id,
                    ManufacturerCarId = _bmwId,
                    AmenitiesId = _bluetoothAmenityId,
                    ModelName = "BMW IX3",
                    BatteryCapacityKwh = "80",
                    RangeKm = "400",
                    LimiteDailyKm = "190",
                    Seats = "5",
                    Image =
                        "https://encrypted-tbn0.gstatic.com/images?q=tbn:ANd9GcT7RG2d648UTyaXXMo36cRGy2rOA2Xms1hCdQ&s",
                    Price = 55000.0,
                    Sale = 8,
                    CreatedAt = new DateTime(2025, 10, 17, 0, 49, 43, 620, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 17, 0, 49, 43, 620, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new Model
                {
                    Id = _modelYId,
                    ManufacturerCarId = _teslaId,
                    AmenitiesId = _bluetoothAmenityId,
                    ModelName = "Tesla Model Y",
                    BatteryCapacityKwh = "75",
                    RangeKm = "350",
                    LimiteDailyKm = "200",
                    Seats = "7",
                    Image =
                        "https://hips.hearstapps.com/hmg-prod/images/2026-tesla-model-y-long-range-awd-121-688bc237a2711.jpg?crop=0.615xw:0.519xh;0.0865xw,0.365xh&resize=1200:*",
                    Price = 60000.0,
                    Sale = 5,
                    CreatedAt = new DateTime(2025, 10, 17, 0, 49, 43, 620, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 17, 0, 49, 43, 620, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new Model
                {
                    Id = _vf8Id,
                    ManufacturerCarId = _vinfastId,
                    AmenitiesId = _acAmenityId,
                    ModelName = "Vinfast VF8",
                    BatteryCapacityKwh = "82",
                    RangeKm = "350",
                    LimiteDailyKm = "180",
                    Seats = "5",
                    Image =
                        "https://hips.hearstapps.com/hmg-prod/images/2023-vinfast-vf8-9283-64638ba8c149b.jpg?crop=0.641xw:0.543xh;0.114xw,0.346xh&resize=2048:*",
                    Price = 45000.0,
                    Sale = 10,
                    CreatedAt = new DateTime(2025, 10, 17, 0, 49, 43, 620, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 17, 0, 49, 43, 620, DateTimeKind.Utc),
                    IsDeleted = false
                });
        }


        private static void SeedCarEVs(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CarEV>().HasData(
                // VinFast VF9 cars
                new CarEV
                {
                    Id = "9b63670dc9d64dcc92d15b7abc8899c8",
                    ModelId = "1fecbc408acd4e17a5eca29f07e3686b", // VF9
                    DepotId = "8664ebff6e944c38a801ddc357f9dac0", // CT Depot - Ninh Kieu
                    LicensePlate = "62X-87956",
                    BatteryHealthPercentage = "97",
                    Status = CarEvStatus.AVAILABLE,
                    CreatedBy = "System",
                    CreatedAt = new DateTime(2025, 10, 19, 15, 28, 4, 363, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 19, 15, 28, 4, 363, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new CarEV
                {
                    Id = "0f1609ba52134282800fa5cbd7e5f028",
                    ModelId = "1fecbc408acd4e17a5eca29f07e3686b", // VF9
                    DepotId = "8664ebff6e944c38a801ddc357f9dac0", // CT Depot - Ninh Kieu
                    LicensePlate = "62X-87942",
                    BatteryHealthPercentage = "97",
                    Status = CarEvStatus.AVAILABLE,
                    CreatedBy = "System",
                    CreatedAt = new DateTime(2025, 10, 19, 15, 28, 24, 266, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 19, 15, 28, 24, 266, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new CarEV
                {
                    Id = "fbb136c379fb485382ec6d7fe9643950",
                    ModelId = "1fecbc408acd4e17a5eca29f07e3686b", // VF9
                    DepotId = "0782b3f130cb42ff9af24487234b251c", // CT Depot - Binh Thuy
                    LicensePlate = "72X-87943",
                    BatteryHealthPercentage = "97",
                    Status = CarEvStatus.AVAILABLE,
                    CreatedBy = "System",
                    CreatedAt = new DateTime(2025, 10, 19, 15, 28, 54, 674, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 19, 15, 28, 54, 674, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new CarEV
                {
                    Id = "8af0318d69cd47ab890cc4adc9612f96",
                    ModelId = "1fecbc408acd4e17a5eca29f07e3686b", // VF9
                    DepotId = "1ca81b37db0042c5b74092575026fcc9", // HCM Depot - Tan Binh
                    LicensePlate = "10X-00004",
                    BatteryHealthPercentage = "100",
                    Status = CarEvStatus.AVAILABLE,
                    CreatedBy = "System",
                    CreatedAt = new DateTime(2025, 10, 19, 1, 9, 17, 455, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 19, 1, 9, 17, 455, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new CarEV
                {
                    Id = "b5299d8d105d47448954155aeead5d45",
                    ModelId = "1fecbc408acd4e17a5eca29f07e3686b", // VF9
                    DepotId = "1ca81b37db0042c5b74092575026fcc9", // HCM Depot - Tan Binh
                    LicensePlate = "10X-00002",
                    BatteryHealthPercentage = "100",
                    Status = CarEvStatus.AVAILABLE,
                    CreatedBy = "System",
                    CreatedAt = new DateTime(2025, 10, 19, 1, 4, 54, 641, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 19, 1, 4, 54, 641, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new CarEV
                {
                    Id = "f546472e6f2e456085deb1fa3546082a",
                    ModelId = "1fecbc408acd4e17a5eca29f07e3686b", // VF9
                    DepotId = "1ca81b37db0042c5b74092575026fcc9", // HCM Depot - Tan Binh
                    LicensePlate = "10X-00001",
                    BatteryHealthPercentage = "95",
                    Status = CarEvStatus.AVAILABLE,
                    CreatedBy = "System",
                    CreatedAt = new DateTime(2025, 10, 19, 1, 2, 24, 513, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 19, 1, 2, 24, 512, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new CarEV
                {
                    Id = "49f3bd2c57fc4f45ac8c63fd71bb0eac",
                    ModelId = "1fecbc408acd4e17a5eca29f07e3686b", // VF9
                    DepotId = "1ca81b37db0042c5b74092575026fcc9", // HCM Depot - Tan Binh
                    LicensePlate = "10X-00003",
                    BatteryHealthPercentage = "100",
                    Status = CarEvStatus.AVAILABLE,
                    CreatedBy = "System",
                    UpdatedBy = "Staff HCM TanBinh",
                    CreatedAt = new DateTime(2025, 10, 19, 1, 9, 8, 31, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 25, 20, 39, 53, 449, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new CarEV
                {
                    Id = "ce27c13c42b545cf87131d9f672c8d0b",
                    ModelId = "1fecbc408acd4e17a5eca29f07e3686b", // VF9
                    DepotId = "86114896990e4fab8b341a581e2ca551", // HCM Depot - Quan 1
                    LicensePlate = "81X-56843",
                    BatteryHealthPercentage = "97",
                    Status = CarEvStatus.AVAILABLE,
                    CreatedBy = "System",
                    CreatedAt = new DateTime(2025, 10, 19, 15, 29, 50, 875, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 19, 15, 29, 50, 875, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new CarEV
                {
                    Id = "e28309f391764961998c2fd742ea35c9",
                    ModelId = "1fecbc408acd4e17a5eca29f07e3686b", // VF9
                    DepotId = "86114896990e4fab8b341a581e2ca551", // HCM Depot - Quan 1
                    LicensePlate = "81X-57946",
                    BatteryHealthPercentage = "97",
                    Status = CarEvStatus.IN_USE,
                    CreatedBy = "System",
                    UpdatedBy = "Demo User",
                    CreatedAt = new DateTime(2025, 10, 19, 15, 29, 37, 833, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 24, 22, 45, 45, 349, DateTimeKind.Utc),
                    IsDeleted = false
                },

                // Tesla Model 3 cars
                new CarEV
                {
                    Id = "9082a04c20b046dca5f42d334468bca8",
                    ModelId = _model3Id, // Tesla Model 3
                    DepotId = "86114896990e4fab8b341a581e2ca551", // HCM Depot - Quan 1
                    LicensePlate = "52C-58123",
                    BatteryHealthPercentage = "95",
                    Status = CarEvStatus.IN_USE,
                    UpdatedBy = "Demo User",
                    CreatedAt = new DateTime(2025, 10, 17, 0, 49, 43, 620, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 26, 22, 43, 14, 985, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new CarEV
                {
                    Id = "1f5b664ac619425eb935dc94dcedbaab",
                    ModelId = _model3Id, // Tesla Model 3
                    DepotId = "1ca81b37db0042c5b74092575026fcc9", // HCM Depot - Tan Binh
                    LicensePlate = "20X-00001",
                    BatteryHealthPercentage = "100",
                    Status = CarEvStatus.AVAILABLE,
                    CreatedBy = "System",
                    CreatedAt = new DateTime(2025, 10, 19, 1, 10, 45, 288, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 19, 1, 10, 45, 288, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new CarEV
                {
                    Id = "e1377c2e977f4d9fa03674e56cfd2a5a",
                    ModelId = _model3Id, // Tesla Model 3
                    DepotId = "1ca81b37db0042c5b74092575026fcc9", // HCM Depot - Tan Binh
                    LicensePlate = "20X-00002",
                    BatteryHealthPercentage = "100",
                    Status = CarEvStatus.AVAILABLE,
                    CreatedBy = "System",
                    CreatedAt = new DateTime(2025, 10, 19, 1, 10, 50, 732, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 19, 1, 10, 50, 732, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new CarEV
                {
                    Id = "97a5e122fc6c4a4db729d209d6e52b30",
                    ModelId = _model3Id, // Tesla Model 3
                    DepotId = "1ca81b37db0042c5b74092575026fcc9", // HCM Depot - Tan Binh
                    LicensePlate = "20X-00003",
                    BatteryHealthPercentage = "100",
                    Status = CarEvStatus.AVAILABLE,
                    CreatedBy = "System",
                    CreatedAt = new DateTime(2025, 10, 19, 1, 10, 56, 428, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 19, 1, 10, 56, 428, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new CarEV
                {
                    Id = "32de98d50b984c4d97f17493bc512c38",
                    ModelId = _model3Id, // Tesla Model 3
                    DepotId = "1ca81b37db0042c5b74092575026fcc9", // HCM Depot - Tan Binh
                    LicensePlate = "20X-00004",
                    BatteryHealthPercentage = "100",
                    Status = CarEvStatus.AVAILABLE,
                    CreatedBy = "System",
                    CreatedAt = new DateTime(2025, 10, 19, 1, 11, 1, 213, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 19, 1, 11, 1, 213, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new CarEV
                {
                    Id = "a64bdf38e7ed47b987870fd26ded9a10",
                    ModelId = _model3Id, // Tesla Model 3
                    DepotId = "1ca81b37db0042c5b74092575026fcc9", // HCM Depot - Tan Binh
                    LicensePlate = "20X-00005",
                    BatteryHealthPercentage = "100",
                    Status = CarEvStatus.AVAILABLE,
                    CreatedBy = "System",
                    CreatedAt = new DateTime(2025, 10, 19, 1, 11, 5, 224, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 19, 1, 11, 5, 224, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new CarEV
                {
                    Id = "28b6ebf061bf417692d0bd71af2ee560",
                    ModelId = _model3Id, // Tesla Model 3
                    DepotId = "1ca81b37db0042c5b74092575026fcc9", // HCM Depot - Tan Binh
                    LicensePlate = "20X-00006",
                    BatteryHealthPercentage = "100",
                    Status = CarEvStatus.AVAILABLE,
                    CreatedBy = "System",
                    CreatedAt = new DateTime(2025, 10, 19, 1, 11, 10, 539, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 19, 1, 11, 10, 539, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new CarEV
                {
                    Id = "3cce2f7167764b03b5ab9640f2dfa3ec",
                    ModelId = _model3Id, // Tesla Model 3
                    DepotId = "86114896990e4fab8b341a581e2ca551", // HCM Depot - Quan 1
                    LicensePlate = "55X-78223",
                    BatteryHealthPercentage = "85",
                    Status = CarEvStatus.AVAILABLE,
                    UpdatedBy = "Staff Member",
                    CreatedAt = new DateTime(2025, 10, 17, 0, 49, 43, 620, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 17, 11, 12, 43, 471, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new CarEV
                {
                    Id = "e03c43aff4014f3db050fdb0c10d23ff",
                    ModelId = _model3Id, // Tesla Model 3
                    DepotId = "0ffe5e744c7d4a47a2614633aee37254", // HN Depot - Dong Da
                    LicensePlate = "52X-28310",
                    BatteryHealthPercentage = "93",
                    Status = CarEvStatus.AVAILABLE,
                    CreatedBy = "System",
                    CreatedAt = new DateTime(2025, 10, 21, 7, 26, 56, 432, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 21, 7, 26, 56, 432, DateTimeKind.Utc),
                    IsDeleted = false
                },

                // Tesla Model Y cars  
                new CarEV
                {
                    Id = "ce19dc1e5d5a4585a95feddee74427b1",
                    ModelId = _modelYId, // Tesla Model Y
                    DepotId = "1ca81b37db0042c5b74092575026fcc9", // HCM Depot - Tan Binh
                    LicensePlate = "20X-18223",
                    BatteryHealthPercentage = "90",
                    Status = CarEvStatus.IN_USE,
                    UpdatedBy = "Demo User",
                    CreatedAt = new DateTime(2025, 10, 17, 0, 49, 43, 620, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 25, 16, 12, 6, 452, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new CarEV
                {
                    Id = "567fdab171d9427eb91ad3472aed6cf6",
                    ModelId = _modelYId, // Tesla Model Y
                    DepotId = "b084f39355914a87a535ce632da23596", // HCM Depot - Quan 3
                    LicensePlate = "21X-46239",
                    BatteryHealthPercentage = "95",
                    Status = CarEvStatus.IN_USE,
                    CreatedBy = "System",
                    UpdatedBy = "Demo User",
                    CreatedAt = new DateTime(2025, 10, 19, 15, 31, 13, 600, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 25, 15, 26, 31, 57, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new CarEV
                {
                    Id = "4c500910ced5404489d8a7794ce4e3c6",
                    ModelId = _modelYId, // Tesla Model Y
                    DepotId = "b084f39355914a87a535ce632da23596", // HCM Depot - Quan 3
                    LicensePlate = "21X-46639",
                    BatteryHealthPercentage = "95",
                    Status = CarEvStatus.AVAILABLE,
                    CreatedBy = "System",
                    CreatedAt = new DateTime(2025, 10, 19, 15, 31, 16, 413, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 19, 15, 31, 16, 413, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new CarEV
                {
                    Id = "44e7e66863c34b419333d1a9bae8f2a2",
                    ModelId = _modelYId, // Tesla Model Y
                    DepotId = "b084f39355914a87a535ce632da23596", // HCM Depot - Quan 3
                    LicensePlate = "21X-46679",
                    BatteryHealthPercentage = "95",
                    Status = CarEvStatus.AVAILABLE,
                    CreatedBy = "System",
                    CreatedAt = new DateTime(2025, 10, 19, 15, 31, 19, 125, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 19, 15, 31, 19, 125, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new CarEV
                {
                    Id = "82c026679ca74cbaa24fc27d2c2c6b24",
                    ModelId = _modelYId, // Tesla Model Y
                    DepotId = "b084f39355914a87a535ce632da23596", // HCM Depot - Quan 3
                    LicensePlate = "21X-46678",
                    BatteryHealthPercentage = "95",
                    Status = CarEvStatus.AVAILABLE,
                    CreatedBy = "System",
                    CreatedAt = new DateTime(2025, 10, 19, 15, 31, 21, 476, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 19, 15, 31, 21, 476, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new CarEV
                {
                    Id = "e731a53c376446e29af3d9e95c2f187b",
                    ModelId = _modelYId, // Tesla Model Y
                    DepotId = "b084f39355914a87a535ce632da23596", // HCM Depot - Quan 3
                    LicensePlate = "21X-44678",
                    BatteryHealthPercentage = "95",
                    Status = CarEvStatus.AVAILABLE,
                    CreatedBy = "System",
                    CreatedAt = new DateTime(2025, 10, 19, 15, 31, 26, 203, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 19, 15, 31, 26, 203, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new CarEV
                {
                    Id = "eb083153cc7d467cba26df62f58cd981",
                    ModelId = _modelYId, // Tesla Model Y
                    DepotId = "b084f39355914a87a535ce632da23596", // HCM Depot - Quan 3
                    LicensePlate = "31X-44678",
                    BatteryHealthPercentage = "90",
                    Status = CarEvStatus.AVAILABLE,
                    CreatedBy = "System",
                    UpdatedBy = "System",
                    CreatedAt = new DateTime(2025, 10, 19, 15, 31, 39, 235, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 20, 23, 44, 26, 521, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new CarEV
                {
                    Id = "e63463e5355446f886df1d64918e5680",
                    ModelId = _modelYId, // Tesla Model Y
                    DepotId = "b084f39355914a87a535ce632da23596", // HCM Depot - Quan 3
                    LicensePlate = "21X-45239",
                    BatteryHealthPercentage = "95",
                    Status = CarEvStatus.IN_USE,
                    CreatedBy = "System",
                    UpdatedBy = "Demo User",
                    CreatedAt = new DateTime(2025, 10, 19, 15, 31, 9, 800, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 24, 22, 13, 59, 66, DateTimeKind.Utc),
                    IsDeleted = false
                },

                // VinFast VF8 cars
                new CarEV
                {
                    Id = "9b347101c513423dbe842ebef5c023af",
                    ModelId = _vf8Id, // VinFast VF8
                    DepotId = "0ffe5e744c7d4a47a2614633aee37254", // HN Depot - Dong Da
                    LicensePlate = "36X-78551",
                    BatteryHealthPercentage = "100",
                    Status = CarEvStatus.AVAILABLE,
                    CreatedBy = "System",
                    CreatedAt = new DateTime(2025, 10, 19, 15, 24, 6, 342, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 19, 15, 24, 6, 342, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new CarEV
                {
                    Id = "f5ba94a3d13f41b8b2a46d7c0563686c",
                    ModelId = _vf8Id, // VinFast VF8
                    DepotId = "0ffe5e744c7d4a47a2614633aee37254", // HN Depot - Dong Da
                    LicensePlate = "36X-77551",
                    BatteryHealthPercentage = "100",
                    Status = CarEvStatus.AVAILABLE,
                    CreatedBy = "System",
                    CreatedAt = new DateTime(2025, 10, 19, 15, 24, 21, 959, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 19, 15, 24, 21, 959, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new CarEV
                {
                    Id = "861db525fdae4073a536bb42f003870e",
                    ModelId = _vf8Id, // VinFast VF8
                    DepotId = "1ca81b37db0042c5b74092575026fcc9", // HCM Depot - Tan Binh
                    LicensePlate = "50X-58223",
                    BatteryHealthPercentage = "95",
                    Status = CarEvStatus.AVAILABLE,
                    UpdatedBy = "Staff HCM TanBinh",
                    CreatedAt = new DateTime(2025, 10, 17, 0, 49, 43, 620, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 21, 7, 41, 1, 32, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new CarEV
                {
                    Id = "3e6f13f6db1d494bbd723f87c9040811",
                    ModelId = _vf8Id, // VinFast VF8
                    DepotId = "86114896990e4fab8b341a581e2ca551", // HCM Depot - Quan 1
                    LicensePlate = "43X-58551",
                    BatteryHealthPercentage = "100",
                    Status = CarEvStatus.AVAILABLE,
                    UpdatedBy = "Staff HCM TanBinh",
                    CreatedAt = new DateTime(2025, 10, 17, 0, 49, 43, 620, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 25, 16, 22, 5, 158, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new CarEV
                {
                    Id = "84a9bdbe8b1a4564a96147c7a2485397",
                    ModelId = _vf8Id, // VinFast VF8
                    DepotId = "0ffe5e744c7d4a47a2614633aee37254", // HN Depot - Dong Da
                    LicensePlate = "22X-73411",
                    BatteryHealthPercentage = "93",
                    Status = CarEvStatus.AVAILABLE,
                    CreatedBy = "System",
                    CreatedAt = new DateTime(2025, 10, 21, 7, 27, 59, 710, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 21, 7, 27, 59, 710, DateTimeKind.Utc),
                    IsDeleted = false
                },

                // BMW IX3 cars
                new CarEV
                {
                    Id = "fd278c2075c246039fe73d5c4cc918b2",
                    ModelId = "47344916183044178a471bb048d67469", // BMW IX3
                    DepotId = "1393d516aeb84e2bace0eed3f18f8df9", // HN Depot - Ba Dinh
                    LicensePlate = "42X-87555",
                    BatteryHealthPercentage = "99",
                    Status = CarEvStatus.AVAILABLE,
                    CreatedBy = "System",
                    CreatedAt = new DateTime(2025, 10, 19, 15, 26, 26, 199, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 19, 15, 26, 26, 199, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new CarEV
                {
                    Id = "a02ac062e8534809a778273d1517c315",
                    ModelId = "47344916183044178a471bb048d67469", // BMW IX3
                    DepotId = "1393d516aeb84e2bace0eed3f18f8df9", // HN Depot - Ba Dinh
                    LicensePlate = "42X-87554",
                    BatteryHealthPercentage = "99",
                    Status = CarEvStatus.AVAILABLE,
                    CreatedBy = "System",
                    CreatedAt = new DateTime(2025, 10, 19, 15, 26, 30, 134, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 19, 15, 26, 30, 134, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new CarEV
                {
                    Id = "2b09d05bb17446c68964c6db403b0262",
                    ModelId = "47344916183044178a471bb048d67469", // BMW IX3
                    DepotId = "1393d516aeb84e2bace0eed3f18f8df9", // HN Depot - Ba Dinh
                    LicensePlate = "42X-87553",
                    BatteryHealthPercentage = "99",
                    Status = CarEvStatus.AVAILABLE,
                    CreatedBy = "System",
                    CreatedAt = new DateTime(2025, 10, 19, 15, 26, 32, 980, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 19, 15, 26, 32, 980, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new CarEV
                {
                    Id = "b4a7fa87d6554f668d3747545ae20deb",
                    ModelId = "47344916183044178a471bb048d67469", // BMW IX3
                    DepotId = "8664ebff6e944c38a801ddc357f9dac0", // CT Depot - Ninh Kieu
                    LicensePlate = "60X-28338",
                    BatteryHealthPercentage = "91",
                    Status = CarEvStatus.AVAILABLE,
                    CreatedBy = "System",
                    CreatedAt = new DateTime(2025, 10, 21, 7, 25, 28, 955, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 21, 7, 25, 28, 954, DateTimeKind.Utc),
                    IsDeleted = false
                },

                // Mercedes GLA cars
                new CarEV
                {
                    Id = "993e9fbdaa954e66888042110bb435e5",
                    ModelId = "f967d78e0cb04b12afd5581ad3a13d64", // Mercedes GLA
                    DepotId = "ad971561785446d1a018d5881a307d56", // HN Depot - Hoan Kiem
                    LicensePlate = "32X-87952",
                    BatteryHealthPercentage = "98",
                    Status = CarEvStatus.AVAILABLE,
                    CreatedBy = "System",
                    CreatedAt = new DateTime(2025, 10, 19, 15, 27, 8, 498, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 19, 15, 27, 8, 498, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new CarEV
                {
                    Id = "6ec06c8516774988b9108f328901d8cd",
                    ModelId = "f967d78e0cb04b12afd5581ad3a13d64", // Mercedes GLA
                    DepotId = "1393d516aeb84e2bace0eed3f18f8df9", // HN Depot - Ba Dinh
                    LicensePlate = "42X-87556",
                    BatteryHealthPercentage = "99",
                    Status = CarEvStatus.AVAILABLE,
                    CreatedBy = "System",
                    CreatedAt = new DateTime(2025, 10, 19, 15, 26, 23, 403, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 19, 15, 26, 23, 403, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new CarEV
                {
                    Id = "594129df1fc34ce28fa6023b83ec3f14",
                    ModelId = "f967d78e0cb04b12afd5581ad3a13d64", // Mercedes GLA
                    DepotId = "1393d516aeb84e2bace0eed3f18f8df9", // HN Depot - Ba Dinh
                    LicensePlate = "42X-87557",
                    BatteryHealthPercentage = "99",
                    Status = CarEvStatus.AVAILABLE,
                    CreatedBy = "System",
                    UpdatedBy = "Staff HN Ba Đình",
                    CreatedAt = new DateTime(2025, 10, 19, 15, 26, 18, 808, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 21, 7, 17, 59, 789, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new CarEV
                {
                    Id = "08411178f9f54ff9bc2b513bab1b0efb",
                    ModelId = "f967d78e0cb04b12afd5581ad3a13d64", // Mercedes GLA
                    DepotId = "b084f39355914a87a535ce632da23596", // HCM Depot - Quan 3
                    LicensePlate = "91X-55239",
                    BatteryHealthPercentage = "96",
                    Status = CarEvStatus.AVAILABLE,
                    CreatedBy = "System",
                    CreatedAt = new DateTime(2025, 10, 19, 15, 30, 32, 635, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 19, 15, 30, 32, 635, DateTimeKind.Utc),
                    IsDeleted = false
                },

                // VinFast VF3 car
                new CarEV
                {
                    Id = "53afcdc2c4f54063a5c9d26802912e9a",
                    ModelId = "407ff9fbeac442eb95d699e93d1df032", // VinFast VF3
                    DepotId = "86114896990e4fab8b341a581e2ca551", // HCM Depot - Quan 1
                    LicensePlate = "59A-36237",
                    BatteryHealthPercentage = "97",
                    Status = CarEvStatus.AVAILABLE,
                    CreatedBy = "System",
                    UpdatedBy = "System",
                    CreatedAt = new DateTime(2025, 10, 25, 16, 30, 37, 178, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 25, 16, 51, 9, 986, DateTimeKind.Utc),
                    IsDeleted = false
                },

                // Additional cars to complete the data set (selecting key examples)
                new CarEV
                {
                    Id = "0755638b8be44f3a994584689bba0ea6",
                    ModelId = "1fecbc408acd4e17a5eca29f07e3686b", // VF9
                    DepotId = "8664ebff6e944c38a801ddc357f9dac0", // CT Depot - Ninh Kieu
                    LicensePlate = "62X-87945",
                    BatteryHealthPercentage = "97",
                    Status = CarEvStatus.AVAILABLE,
                    CreatedBy = "System",
                    CreatedAt = new DateTime(2025, 10, 19, 15, 28, 10, 277, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 19, 15, 28, 10, 277, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new CarEV
                {
                    Id = "2e2c7e60eb4240eb9cc1c5c87cb07053",
                    ModelId = "1fecbc408acd4e17a5eca29f07e3686b", // VF9
                    DepotId = "86114896990e4fab8b341a581e2ca551", // HCM Depot - Quan 1
                    LicensePlate = "81X-56946",
                    BatteryHealthPercentage = "97",
                    Status = CarEvStatus.AVAILABLE,
                    CreatedBy = "System",
                    CreatedAt = new DateTime(2025, 10, 19, 15, 29, 40, 944, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 19, 15, 29, 40, 944, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new CarEV
                {
                    Id = "3a170121f3744232ae382d13ae43a765",
                    ModelId = "1fecbc408acd4e17a5eca29f07e3686b", // VF9
                    DepotId = "b084f39355914a87a535ce632da23596", // HCM Depot - Quan 3
                    LicensePlate = "91X-55231",
                    BatteryHealthPercentage = "96",
                    Status = CarEvStatus.AVAILABLE,
                    CreatedBy = "System",
                    CreatedAt = new DateTime(2025, 10, 19, 15, 30, 23, 589, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 19, 15, 30, 23, 589, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new CarEV
                {
                    Id = "48f0157a7a8d4fb1af1a1230ed8985eb",
                    ModelId = _modelYId, // Tesla Model Y
                    DepotId = "ad971561785446d1a018d5881a307d56", // HN Depot - Hoan Kiem
                    LicensePlate = "22X-53410",
                    BatteryHealthPercentage = "93",
                    Status = CarEvStatus.AVAILABLE,
                    CreatedBy = "System",
                    CreatedAt = new DateTime(2025, 10, 21, 7, 28, 37, 503, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 21, 7, 28, 37, 503, DateTimeKind.Utc),
                    IsDeleted = false
                }
            );
        }

        private static void SeedSystemConfig(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<SystemConfig>().HasData(
                new SystemConfig
                {
                    Id = "5595fc17323c48a89f2583de3de9c51a",
                    Key = "DEPOSIT_FEE_PERCENTAGE",
                    Value = "30",
                    ConfigType = ConfigType.General,
                    CreatedBy = "System",
                    UpdatedBy = "System",
                    CreatedAt = new DateTime(2025, 10, 21, 7, 28, 37, 503, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 10, 21, 7, 28, 37, 503, DateTimeKind.Utc),   
                    IsDeleted = false
                },
                new SystemConfig
                {
                    Id = "booking001buffertime00000000000",
                    Key = "BOOKING_BUFFER_TIME_MINUTES",
                    Value = "60",
                    ConfigType = ConfigType.General,
                    CreatedBy = "System",
                    UpdatedBy = "System",
                    CreatedAt = new DateTime(2025, 11, 10, 0, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 11, 10, 0, 0, 0, 0, DateTimeKind.Utc),   
                    IsDeleted = false
                }
            );
        }

        private static void SeedMembershipConfigs(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<MembershipConfig>().HasData(
                new MembershipConfig
                {
                    Id = "mc001noneconfig000000000000000000",
                    Level = MembershipLevel.None,
                    DiscountPercent = 0m,
                    RequiredAmount = 0m,
                    CreatedBy = "System",
                    UpdatedBy = "System",
                    CreatedAt = new DateTime(2025, 11, 7, 0, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 11, 7, 0, 0, 0, 0, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new MembershipConfig
                {
                    Id = "mc002bronzeconfig00000000000000000",
                    Level = MembershipLevel.Bronze,
                    DiscountPercent = 10m,
                    RequiredAmount = 20000m,
                    CreatedBy = "System",
                    UpdatedBy = "System",
                    CreatedAt = new DateTime(2025, 11, 7, 0, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 11, 7, 0, 0, 0, 0, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new MembershipConfig
                {
                    Id = "mc003silverconfig00000000000000000",
                    Level = MembershipLevel.Silver,
                    DiscountPercent = 20m,
                    RequiredAmount = 50000m,
                    CreatedBy = "System",
                    UpdatedBy = "System",
                    CreatedAt = new DateTime(2025, 11, 7, 0, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 11, 7, 0, 0, 0, 0, DateTimeKind.Utc),
                    IsDeleted = false
                },
                new MembershipConfig
                {
                    Id = "mc004goldconfig0000000000000000000",
                    Level = MembershipLevel.Gold,
                    DiscountPercent = 30m,
                    RequiredAmount = 100000m,
                    CreatedBy = "System",
                    UpdatedBy = "System",
                    CreatedAt = new DateTime(2025, 11, 7, 0, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2025, 11, 7, 0, 0, 0, 0, DateTimeKind.Utc),
                    IsDeleted = false
                }
            );
        }
    }
}