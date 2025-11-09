using System;
using AutoMapper;
using EVSRS.BusinessObjects.DTO.AmenitiesDto;
using EVSRS.BusinessObjects.DTO.CarEVDto;
using EVSRS.BusinessObjects.DTO.CarManufactureDto;
using EVSRS.BusinessObjects.DTO.ContractDto;
using EVSRS.BusinessObjects.DTO.DepotDto;
using EVSRS.BusinessObjects.DTO.FeedbackDto;
using EVSRS.BusinessObjects.DTO.HandoverInspectionDto;
using EVSRS.BusinessObjects.DTO.OrderBookingDto;
using EVSRS.BusinessObjects.DTO.ModelDto;
using EVSRS.BusinessObjects.DTO.ReturnSettlementDto;
using EVSRS.BusinessObjects.DTO.SystemConfigDto;
using EVSRS.BusinessObjects.DTO.TokenDto;
using EVSRS.BusinessObjects.DTO.TransactionDto;
using EVSRS.BusinessObjects.DTO.UserDto;
using EVSRS.BusinessObjects.DTO.AuthDto;
using EVSRS.BusinessObjects.Enum;
using EVSRS.BusinessObjects.Entity;
using EVSRS.BusinessObjects.DTO.IdentifyDocumentDto;
using EVSRS.BusinessObjects.DTO.MembershipDto;

namespace EVSRS.Services.Mapper
{
    public class MapperEntities : Profile
    {
        public MapperEntities()
        {
            #region Token Mapper

            CreateMap<ApplicationUserToken, TokenResponseDto>().ReverseMap();
            CreateMap<ApplicationUserToken, RefreshTokenRequestDto>().ReverseMap();


            #endregion

            #region User Mapper

            CreateMap<ApplicationUser, UserResponseDto>()
                .ForMember(dest => dest.Role,
                    opt => opt.MapFrom(src => src.Role != null ? src.Role : null))
                .ReverseMap();
            CreateMap<ApplicationUser, UserRequestDto>().ReverseMap();
            CreateMap<ApplicationUser, RegisterUserRequestDto>();
            CreateMap<RegisterUserRequestDto, ApplicationUser>()
                .ForMember(dest => dest.HashPassword, opt => opt.Ignore())
                .ForMember(dest => dest.Salt, opt => opt.Ignore());
            CreateMap<ApplicationUser, SendOTPRequestDto>().ReverseMap();
            CreateMap<ApplicationUser, CreateStaffRequestDto>().ReverseMap()
                .ForMember(dest => dest.HashPassword, opt => opt.Ignore())
                .ForMember(dest => dest.Salt, opt => opt.Ignore())
                .ForMember(dest => dest.IsVerify, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.Role, opt => opt.MapFrom(src => Role.STAFF));
            CreateMap<RegisterUserAtDepotRequestDto, ApplicationUser>()
                .ForMember(dest => dest.HashPassword, opt => opt.Ignore())
                .ForMember(dest => dest.Salt, opt => opt.Ignore())
                .ForMember(dest => dest.UserName, opt => opt.Ignore())
                .ForMember(dest => dest.IsVerify, opt => opt.Ignore())
                .ForMember(dest => dest.Role, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.DepotId, opt => opt.Ignore())
                .ForMember(dest => dest.ProfilePicture, opt => opt.Ignore())
                .ForMember(dest => dest.DateOfBirth, opt => opt.Ignore())
                .ForMember(dest => dest.OTPs, opt => opt.Ignore())
                .ForMember(dest => dest.UserTokens, opt => opt.Ignore())
                .ForMember(dest => dest.Notifications, opt => opt.Ignore())
                .ForMember(dest => dest.Transactions, opt => opt.Ignore())
                .ForMember(dest => dest.Contracts, opt => opt.Ignore())
                .ForMember(dest => dest.Feedbacks, opt => opt.Ignore())
                .ForMember(dest => dest.IdentifyDocuments, opt => opt.Ignore())
                .ForMember(dest => dest.OrderBookings, opt => opt.Ignore())
                .ForMember(dest => dest.Depot, opt => opt.Ignore());
            // CreateMap<ApplicationUser, UpdateUserRequestDto>().ReverseMap();
            // CreateMap<ApplicationUser, UpdateProfileRequestDto>().ReverseMap();

            #endregion

            #region CarManufacture Mapper

            CreateMap<CarManufacture, CarManufactureResponseDto>().ReverseMap();
            CreateMap<CarManufacture, CarManufactureRequestDto>().ReverseMap();
            #endregion

            #region Model Mapper
            CreateMap<Model, ModelResponseDto>().ReverseMap();
            CreateMap<Model, ModelRequestDto>().ReverseMap();
            #endregion

            #region CarEV Mapper
            CreateMap<CarEV, CarEVResponseDto>().ReverseMap();
            CreateMap<CarEV, CarEVRequestDto>().ReverseMap();
            #endregion

            #region Amenities Mapper
            CreateMap<Amenities, AmenitiesResponseDto>().ReverseMap();
            CreateMap<Amenities, AmenitiesRequestDto>().ReverseMap();
            #endregion

            #region Feedback Mapper
            CreateMap<Feedback, FeedbackResponseDto>().ReverseMap();
            CreateMap<Feedback, FeedbackRequestDto>().ReverseMap();
            #endregion

            #region Depot  Mapper
            CreateMap<Depot, DepotRequestDto>().ReverseMap();
            CreateMap<Depot, DepotResponseDto>().ReverseMap();
            #endregion

            #region Transaction  Mapper
            CreateMap<Transaction, TransactionResponseDto>().ReverseMap();
            CreateMap<Transaction, TransactionRequestDto>().ReverseMap();
            #endregion

            #region OrderBooking Mapper
            CreateMap<OrderBooking, OrderBookingResponseDto>().ReverseMap();
            CreateMap<OrderBooking, OrderBookingRequestDto>().ReverseMap();
            CreateMap<OrderBookingOfflineRequestDto, OrderBooking>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.Code, opt => opt.Ignore())
                .ForMember(dest => dest.DepotId, opt => opt.Ignore())
                .ForMember(dest => dest.Status, opt => opt.Ignore())
                .ForMember(dest => dest.PaymentType, opt => opt.Ignore())
                .ForMember(dest => dest.PaymentStatus, opt => opt.Ignore())
                .ForMember(dest => dest.SubTotal, opt => opt.Ignore())
                .ForMember(dest => dest.TotalAmount, opt => opt.Ignore())
                .ForMember(dest => dest.DepositAmount, opt => opt.Ignore())
                .ForMember(dest => dest.RemainingAmount, opt => opt.Ignore())
                .ForMember(dest => dest.CheckOutedAt, opt => opt.Ignore())
                .ForMember(dest => dest.ReturnedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Discount, opt => opt.Ignore())
                .ForMember(dest => dest.ShippingFee, opt => opt.Ignore())
                .ForMember(dest => dest.RemainingBalance, opt => opt.Ignore())
                .ForMember(dest => dest.Type, opt => opt.Ignore())
                .ForMember(dest => dest.User, opt => opt.Ignore())
                .ForMember(dest => dest.Feedbacks, opt => opt.Ignore())
                .ForMember(dest => dest.Transactions, opt => opt.Ignore())
                .ForMember(dest => dest.HandoverInspections, opt => opt.Ignore())
                .ForMember(dest => dest.Contracts, opt => opt.Ignore())
                .ForMember(dest => dest.ReturnSettlement, opt => opt.Ignore())
                .ForMember(dest => dest.CarEvs, opt => opt.Ignore())
                .ForMember(dest => dest.Depot, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedBy, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore());
            #endregion

            #region HandoverInspection Mapper
            CreateMap<HandoverInspection, HandoverInspectionResponseDto>().ReverseMap();
            CreateMap<HandoverInspection, HandoverInspectionRequestDto>().ReverseMap();
            #endregion

            #region Contract Mapper
            CreateMap<Contract, ContractResponseDto>().ReverseMap();
            CreateMap<Contract, ContractRequestDto>().ReverseMap();
            #endregion

            #region ReturnSettlement Mapper
            CreateMap<ReturnSettlement, ReturnSettlementResponseDto>().ReverseMap();
            CreateMap<ReturnSettlementRequestDto, ReturnSettlement>()
                .ForMember(dest => dest.SettlementItems, opt => opt.Ignore()); // Ignore to prevent duplicate
            CreateMap<ReturnSettlement, ReturnSettlementRequestDto>();
            CreateMap<SettlementItem, SettlementItemResponseDto>().ReverseMap();
            CreateMap<SettlementItem, SettlementItemRequestDto>().ReverseMap();
            #endregion

            #region IdentifyDocument Mapper
            CreateMap<IdentifyDocument, IdentifyDocumentResponseDto>().ReverseMap();
            CreateMap<IdentifyDocument, IdentifyDocumentRequestDto>().ReverseMap();
            #endregion

            #region SystemConfig Mapper
            CreateMap<SystemConfig, SystemConfigResponseDto>().ReverseMap();
            CreateMap<SystemConfig, SystemConfigRequestDto>().ReverseMap();
            #endregion

            #region Membership Mapper
            CreateMap<Membership, MembershipResponseDto>()
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.User != null ? src.User.UserName : "Unknown"))
                .ForMember(dest => dest.Level, opt => opt.MapFrom(src => src.MembershipConfig != null ? src.MembershipConfig.Level : MembershipLevel.None))
                .ForMember(dest => dest.LevelName, opt => opt.MapFrom(src => 
                    src.MembershipConfig != null 
                        ? (src.MembershipConfig.Level == MembershipLevel.None ? "Chưa có hạng" :
                           src.MembershipConfig.Level == MembershipLevel.Bronze ? "Đồng" :
                           src.MembershipConfig.Level == MembershipLevel.Silver ? "Bạc" :
                           src.MembershipConfig.Level == MembershipLevel.Gold ? "Vàng" : "Unknown")
                        : "Chưa có hạng"))
                .ForMember(dest => dest.DiscountPercent, opt => opt.MapFrom(src => src.MembershipConfig != null ? src.MembershipConfig.DiscountPercent : 0))
                .ForMember(dest => dest.RequiredAmount, opt => opt.MapFrom(src => src.MembershipConfig != null ? src.MembershipConfig.RequiredAmount : 0))
                .ForMember(dest => dest.ProgressToNextLevel, opt => opt.Ignore())
                .ForMember(dest => dest.AmountToNextLevel, opt => opt.Ignore())
                .ForMember(dest => dest.NextLevelName, opt => opt.Ignore());
            
            CreateMap<MembershipConfig, MembershipConfigResponseDto>()
                .ForMember(dest => dest.LevelName, opt => opt.MapFrom(src => 
                    src.Level == MembershipLevel.None ? "Chưa có hạng" :
                    src.Level == MembershipLevel.Bronze ? "Đồng" :
                    src.Level == MembershipLevel.Silver ? "Bạc" :
                    src.Level == MembershipLevel.Gold ? "Vàng" : "Unknown"));
            #endregion
        }
    }
}