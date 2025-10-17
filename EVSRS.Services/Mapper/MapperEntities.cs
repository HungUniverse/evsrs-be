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
using EVSRS.BusinessObjects.DTO.TokenDto;
using EVSRS.BusinessObjects.DTO.TransactionDto;
using EVSRS.BusinessObjects.DTO.UserDto;
using EVSRS.BusinessObjects.Enum;
using EVSRS.BusinessObjects.Entity;
using EVSRS.BusinessObjects.DTO.IdentifyDocumentDto;

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
            CreateMap<ReturnSettlement, ReturnSettlementRequestDto>().ReverseMap();
            CreateMap<SettlementItem, SettlementItemResponseDto>().ReverseMap();
            CreateMap<SettlementItem, SettlementItemRequestDto>().ReverseMap();
            #endregion

            #region IdentifyDocument Mapper
            CreateMap<IdentifyDocument, IdentifyDocumentResponseDto>().ReverseMap();
            CreateMap<IdentifyDocument, IdentifyDocumentRequestDto>().ReverseMap();
            #endregion
        }
    }
}