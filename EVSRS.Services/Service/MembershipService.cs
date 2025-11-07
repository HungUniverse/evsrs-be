using EVSRS.BusinessObjects.DTO.MembershipDto;
using EVSRS.BusinessObjects.Entity;
using EVSRS.BusinessObjects.Enum;
using EVSRS.Repositories.Implement;
using EVSRS.Services.Interface;

namespace EVSRS.Services.Service
{
    public class MembershipService : IMembershipService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IValidationService _validationService;

        public MembershipService(
            IUnitOfWork unitOfWork,
            IValidationService validationService)
        {
            _unitOfWork = unitOfWork;
            _validationService = validationService;
        }

        public async Task<MembershipResponseDto?> GetMembershipByUserIdAsync(string userId)
        {
            await _validationService.ValidateAndThrowAsync(userId);

            var membership = await _unitOfWork.MembershipRepository.GetByUserIdAsync(userId);
            if (membership == null)
            {
                return null;
            }

            var config = membership.MembershipConfig ?? await _unitOfWork.MembershipConfigRepository
                .GetMembershipConfigByIdAsync(membership.MembershipConfigId);

            if (config == null)
            {
                throw new KeyNotFoundException($"MembershipConfig with ID {membership.MembershipConfigId} not found");
            }

            // Tính progress và next level
            var allConfigs = await _unitOfWork.MembershipConfigRepository.GetAllMembershipConfigsAsync();
            var sortedConfigs = allConfigs.OrderBy(c => c.RequiredAmount).ToList();
            
            var currentIndex = sortedConfigs.FindIndex(c => c.Id == config.Id);
            var nextConfig = currentIndex >= 0 && currentIndex < sortedConfigs.Count - 1 ? sortedConfigs[currentIndex + 1] : null;

            decimal? progressToNextLevel = null;
            decimal? amountToNextLevel = null;
            string? nextLevelName = null;

            if (nextConfig != null)
            {
                amountToNextLevel = nextConfig.RequiredAmount - membership.TotalOrderBill;
                if (amountToNextLevel < 0) amountToNextLevel = 0;

                var rangeToNext = nextConfig.RequiredAmount - config.RequiredAmount;
                if (rangeToNext > 0)
                {
                    var currentProgress = membership.TotalOrderBill - config.RequiredAmount;
                    progressToNextLevel = Math.Min(100, Math.Round((currentProgress / rangeToNext) * 100, 2));
                }

                nextLevelName = nextConfig.Level switch
                {
                    MembershipLevel.Bronze => "Đồng",
                    MembershipLevel.Silver => "Bạc",
                    MembershipLevel.Gold => "Vàng",
                    _ => null
                };
            }

            return new MembershipResponseDto
            {
                Id = membership.Id,
                UserId = membership.UserId,
                UserName = membership.User?.UserName ?? "Unknown",
                Level = config.Level,
                LevelName = config.Level switch
                {
                    MembershipLevel.None => "Chưa có hạng",
                    MembershipLevel.Bronze => "Đồng",
                    MembershipLevel.Silver => "Bạc",
                    MembershipLevel.Gold => "Vàng",
                    _ => "Unknown"
                },
                DiscountPercent = config.DiscountPercent,
                RequiredAmount = config.RequiredAmount,
                TotalOrderBill = membership.TotalOrderBill,
                ProgressToNextLevel = progressToNextLevel,
                AmountToNextLevel = amountToNextLevel,
                NextLevelName = nextLevelName,
                MembershipConfigId = config.Id,
                CreatedAt = membership.CreatedAt,
                UpdatedAt = membership.UpdatedAt
            };
        }

        /// <summary>
        /// Cập nhật TotalOrderBill và tự động nâng hạng khi order complete
        /// </summary>
        public async Task UpdateMembershipAfterOrderCompleteAsync(string userId, decimal orderAmount)
        {
            await _validationService.ValidateAndThrowAsync(userId);

            if (orderAmount < 0)
            {
                throw new ArgumentException("Order amount cannot be negative");
            }

            // 1. Lấy hoặc tạo membership cho user
            var membership = await _unitOfWork.MembershipRepository.GetByUserIdAsync(userId);

            if (membership == null)
            {
                // Tạo membership mới với None level
                await CreateInitialMembershipForUserAsync(userId);
                membership = await _unitOfWork.MembershipRepository.GetByUserIdAsync(userId);
                
                if (membership == null)
                {
                    throw new Exception("Failed to create initial membership");
                }
            }

            // 2. Cộng thêm orderAmount vào TotalOrderBill
            membership.TotalOrderBill += orderAmount;

            // 3. Xác định hạng mới dựa trên TotalOrderBill
            var newConfig = await DetermineConfigFromTotalBillAsync(membership.TotalOrderBill);

            // 4. Nâng hạng nếu config mới khác config hiện tại
            var oldConfigId = membership.MembershipConfigId;
            if (membership.MembershipConfigId != newConfig.Id)
            {
                var oldConfig = await _unitOfWork.MembershipConfigRepository
                    .GetMembershipConfigByIdAsync(oldConfigId);
                
                membership.MembershipConfigId = newConfig.Id;
                
                Console.WriteLine($"🎉 User {userId} upgraded from {oldConfig?.Level} to {newConfig.Level}! " +
                    $"Total: {membership.TotalOrderBill:N0} VND");
            }

            // 5. Lưu thay đổi
            await _unitOfWork.MembershipRepository.UpdateMembershipAsync(membership);
            await _unitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// Tạo membership ban đầu với hạng None cho user mới
        /// </summary>
        public async Task CreateInitialMembershipForUserAsync(string userId)
        {
            await _validationService.ValidateAndThrowAsync(userId);

            // Kiểm tra user đã có membership chưa
            var existingMembership = await _unitOfWork.MembershipRepository.GetByUserIdAsync(userId);
            if (existingMembership != null)
            {
                return; // Đã có rồi, không tạo nữa
            }

            // Lấy config None
            var noneConfig = await _unitOfWork.MembershipConfigRepository
                .GetMembershipConfigByLevelAsync(MembershipLevel.None);

            if (noneConfig == null)
            {
                throw new Exception("None membership config not found. Please seed the database first.");
            }

            // Tạo membership mới
            var membership = new Membership
            {
                UserId = userId,
                MembershipConfigId = noneConfig.Id,
                TotalOrderBill = 0m
            };

            await _unitOfWork.MembershipRepository.CreateMembershipAsync(membership);
            await _unitOfWork.SaveChangesAsync();

            Console.WriteLine($"✅ Created initial membership (None) for user {userId}");
        }

        /// <summary>
        /// Xác định MembershipConfig phù hợp dựa trên TotalOrderBill
        /// Logic: Tìm config cao nhất mà totalBill >= RequiredAmount
        /// </summary>
        private async Task<MembershipConfig> DetermineConfigFromTotalBillAsync(decimal totalBill)
        {
            // Lấy tất cả configs
            var configs = await _unitOfWork.MembershipConfigRepository.GetAllMembershipConfigsAsync();

            if (!configs.Any())
            {
                throw new Exception("No membership configs found in database");
            }

            // Sắp xếp theo RequiredAmount giảm dần
            var sortedConfigs = configs.OrderByDescending(c => c.RequiredAmount).ToList();

            // Tìm config đầu tiên mà totalBill >= RequiredAmount
            var matchedConfig = sortedConfigs.FirstOrDefault(c => totalBill >= c.RequiredAmount);

            if (matchedConfig == null)
            {
                // Fallback về None nếu không match (không nên xảy ra vì None có RequiredAmount = 0)
                return configs.First(c => c.Level == MembershipLevel.None);
            }

            return matchedConfig;
        }
    }
}
