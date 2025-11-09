using AutoMapper;
using EVSRS.BusinessObjects.DTO.MembershipDto;
using EVSRS.BusinessObjects.Entity;
using EVSRS.BusinessObjects.Enum;
using EVSRS.Repositories.Implement;
using EVSRS.Services.Interface;
using Microsoft.AspNetCore.Http;

namespace EVSRS.Services.Service
{
    public class MembershipService : IMembershipService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IValidationService _validationService;

        public MembershipService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IHttpContextAccessor httpContextAccessor,
            IValidationService validationService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _httpContextAccessor = httpContextAccessor;
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
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException("UserId cannot be null or empty");
            }

            if (orderAmount <= 0)
            {
                throw new ArgumentException("Order amount must be greater than 0");
            }

            var membership = await _unitOfWork.MembershipRepository.GetByUserIdAsync(userId);

            if (membership == null)
            {
                await CreateInitialMembershipForUserAsync(userId);
                membership = await _unitOfWork.MembershipRepository.GetByUserIdAsync(userId);
                
                if (membership == null)
                {
                    throw new Exception($"Failed to create membership for user {userId}");
                }
            }

            var oldTotal = membership.TotalOrderBill;
            var oldLevel = membership.MembershipConfigId;
            
            // Modify properties - Entity đã được track từ GetByUserIdAsync, EF sẽ tự detect changes
            membership.TotalOrderBill += orderAmount;
            membership.UpdatedAt = DateTime.UtcNow;
            membership.UpdatedBy = GetCurrentUserName();

            var newConfig = await DetermineConfigFromTotalBillAsync(membership.TotalOrderBill);

            if (membership.MembershipConfigId != newConfig.Id)
            {
                membership.MembershipConfigId = newConfig.Id;
            }

            // ✅ KHÔNG CẦN gọi UpdateMembershipAsync vì:
            // 1. Entity đã được track từ khi load với GetByUserIdAsync (có Include)
            // 2. Khi gọi UpdateAsync → Attach() có thể gây conflict với entity đã được track
            // 3. EF sẽ tự động detect changes khi modify properties và gọi SaveChangesAsync
            
            var saveResult = await _unitOfWork.SaveChangesAsync();
            
            if (saveResult <= 0)
            {
                throw new Exception($"Failed to save membership changes for user {userId}. " +
                    $"SaveChanges returned {saveResult}. Old total: {oldTotal}, New total: {membership.TotalOrderBill}, Added: {orderAmount}");
            }
            
            // Log để debug (có thể remove sau)
            if (oldLevel != membership.MembershipConfigId)
            {
                Console.WriteLine($"🎉 User {userId} upgraded membership: TotalOrderBill {oldTotal} -> {membership.TotalOrderBill}, Level changed");
            }
        }

        /// <summary>
        /// Tạo membership ban đầu với hạng None cho user mới
        /// </summary>
        public async Task CreateInitialMembershipForUserAsync(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException("UserId cannot be null or empty");
            }

            var existingMembership = await _unitOfWork.MembershipRepository.GetByUserIdAsync(userId);
            if (existingMembership != null)
            {
                return;
            }

            var noneConfig = await _unitOfWork.MembershipConfigRepository
                .GetMembershipConfigByLevelAsync(MembershipLevel.None);

            if (noneConfig == null)
            {
                throw new Exception("None membership config not found. Please seed the database first.");
            }

            var membership = new Membership
            {
                Id = Guid.NewGuid().ToString(),
                UserId = userId,
                MembershipConfigId = noneConfig.Id,
                TotalOrderBill = 0m,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = GetCurrentUserName(),
                UpdatedBy = GetCurrentUserName(),
                IsDeleted = false
            };

            await _unitOfWork.MembershipRepository.CreateMembershipAsync(membership);
            await _unitOfWork.SaveChangesAsync();
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

        private string GetCurrentUserName()
        {
            return _httpContextAccessor.HttpContext?.User?.FindFirst("name")?.Value ?? "System";
        }
    }
}
