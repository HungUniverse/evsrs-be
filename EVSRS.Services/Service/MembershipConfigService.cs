using AutoMapper;
using EVSRS.BusinessObjects.DTO.MembershipDto;
using EVSRS.BusinessObjects.Entity;
using EVSRS.BusinessObjects.Enum;
using EVSRS.Repositories.Implement;
using EVSRS.Services.Interface;

namespace EVSRS.Services.Service
{
    public class MembershipConfigService : IMembershipConfigService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IValidationService _validationService;

        public MembershipConfigService(
            IUnitOfWork unitOfWork, 
            IMapper mapper, 
            IValidationService validationService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _validationService = validationService;
        }

        public async Task<List<MembershipConfigResponseDto>> GetAllMembershipConfigsAsync()
        {
            var configs = await _unitOfWork.MembershipConfigRepository.GetAllMembershipConfigsAsync();
            var result = configs.Select(c => new MembershipConfigResponseDto
            {
                Id = c.Id,
                Level = c.Level,
                LevelName = c.Level.ToString(),
                DiscountPercent = c.DiscountPercent,
                RequiredAmount = c.RequiredAmount,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt
            }).ToList();

            return result;
        }

        /// <summary>
        /// Lấy cấu hình membership theo Id.
        /// Trả về <see cref="MembershipConfigResponseDto"/> hoặc null nếu không tìm thấy.
        /// </summary>
        /// <param name="id">ID của MembershipConfig</param>
        public async Task<MembershipConfigResponseDto?> GetMembershipConfigByIdAsync(string id)
        {
            await _validationService.ValidateAndThrowAsync(id);
            
            var config = await _unitOfWork.MembershipConfigRepository.GetMembershipConfigByIdAsync(id);
            if (config == null)
            {
                return null;
            }

            return new MembershipConfigResponseDto
            {
                Id = config.Id,
                Level = config.Level,
                LevelName = config.Level.ToString(),
                DiscountPercent = config.DiscountPercent,
                RequiredAmount = config.RequiredAmount,
                CreatedAt = config.CreatedAt,
                UpdatedAt = config.UpdatedAt
            };
        }

        /// <summary>
        /// Lấy cấu hình membership theo mức độ (Level).
        /// Trả về <see cref="MembershipConfigResponseDto"/> hoặc null nếu không tồn tại cấu hình cho level này.
        /// </summary>
        /// <param name="level">Mức độ membership</param>
        public async Task<MembershipConfigResponseDto?> GetMembershipConfigByLevelAsync(MembershipLevel level)
        {
            var config = await _unitOfWork.MembershipConfigRepository.GetMembershipConfigByLevelAsync(level);
            if (config == null)
            {
                return null;
            }

            return new MembershipConfigResponseDto
            {
                Id = config.Id,
                Level = config.Level,
                LevelName = config.Level.ToString(),
                DiscountPercent = config.DiscountPercent,
                RequiredAmount = config.RequiredAmount,
                CreatedAt = config.CreatedAt,
                UpdatedAt = config.UpdatedAt
            };
        }

        /// <summary>
        /// Tạo mới một cấu hình membership từ dữ liệu đầu vào.
        /// Kiểm tra trùng level và trả về DTO của cấu hình vừa tạo.
        /// </summary>
        /// <param name="dto">Dữ liệu tạo cấu hình</param>
        public async Task<MembershipConfigResponseDto> CreateMembershipConfigAsync(CreateMembershipConfigDto dto)
        {
            await _validationService.ValidateAndThrowAsync(dto);

            // Kiểm tra xem level đã tồn tại chưa
            var exists = await _unitOfWork.MembershipConfigRepository.IsLevelExistsAsync(dto.Level);
            if (exists)
            {
                throw new InvalidOperationException($"Membership config for level '{dto.Level}' already exists.");
            }

            var config = new MembershipConfig
            {
                Level = dto.Level,
                DiscountPercent = dto.DiscountPercent,
                RequiredAmount = dto.RequiredAmount
            };

            await _unitOfWork.MembershipConfigRepository.CreateMembershipConfigAsync(config);
            await _unitOfWork.SaveChangesAsync();

            return new MembershipConfigResponseDto
            {
                Id = config.Id,
                Level = config.Level,
                LevelName = config.Level.ToString(),
                DiscountPercent = config.DiscountPercent,
                RequiredAmount = config.RequiredAmount,
                CreatedAt = config.CreatedAt,
                UpdatedAt = config.UpdatedAt
            };
        }

        /// <summary>
        /// Cập nhật các trường có trong <paramref name="dto"/> cho cấu hình membership xác định bởi <paramref name="id"/>.
        /// Trả về DTO sau khi cập nhật.
        /// </summary>
        /// <param name="id">ID của MembershipConfig cần cập nhật</param>
        /// <param name="dto">Dữ liệu cập nhật</param>
        public async Task<MembershipConfigResponseDto> UpdateMembershipConfigAsync(string id, UpdateMembershipConfigDto dto)
        {
            await _validationService.ValidateAndThrowAsync(dto);
            
            var config = await _unitOfWork.MembershipConfigRepository.GetMembershipConfigByIdAsync(id);
            if (config == null)
            {
                throw new KeyNotFoundException($"Membership config with ID {id} not found.");
            }

            // Cập nhật các trường nếu có giá trị mới
            if (dto.DiscountPercent.HasValue)
            {
                config.DiscountPercent = dto.DiscountPercent.Value;
            }

            if (dto.RequiredAmount.HasValue)
            {
                config.RequiredAmount = dto.RequiredAmount.Value;
            }

            await _unitOfWork.MembershipConfigRepository.UpdateMembershipConfigAsync(config);
            await _unitOfWork.SaveChangesAsync();

            return new MembershipConfigResponseDto
            {
                Id = config.Id,
                Level = config.Level,
                LevelName = config.Level.ToString(),
                DiscountPercent = config.DiscountPercent,
                RequiredAmount = config.RequiredAmount,
                CreatedAt = config.CreatedAt,
                UpdatedAt = config.UpdatedAt
            };
        }

        /// <summary>
        /// Xóa cấu hình membership theo Id. Không cho phép xóa level <see cref="MembershipLevel.None"/>.
        /// </summary>
        /// <param name="id">ID của MembershipConfig cần xóa</param>
        public async Task DeleteMembershipConfigAsync(string id)
        {
            await _validationService.ValidateAndThrowAsync(id);
            
            var config = await _unitOfWork.MembershipConfigRepository.GetMembershipConfigByIdAsync(id);
            if (config == null)
            {
                throw new KeyNotFoundException($"Membership config with ID {id} not found.");
            }

            // Không cho phép xóa config None
            if (config.Level == MembershipLevel.None)
            {
                throw new InvalidOperationException("Cannot delete the None membership config.");
            }

            await _unitOfWork.MembershipConfigRepository.DeleteMembershipConfigAsync(config);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
