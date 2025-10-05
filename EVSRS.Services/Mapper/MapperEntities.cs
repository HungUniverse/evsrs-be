using System;
using AutoMapper;
using EVSRS.BusinessObjects.DTO.TokenDto;
using EVSRS.BusinessObjects.DTO.UserDto;
using EVSRS.BusinessObjects.Entity;

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
            CreateMap<ApplicationUser, RegisterUserRequestDto>();
            CreateMap<RegisterUserRequestDto, ApplicationUser>()
                .ForMember(dest => dest.HashPassword, opt => opt.Ignore())
                .ForMember(dest => dest.Salt, opt => opt.Ignore());
            CreateMap<ApplicationUser, SendOTPRequestDto>().ReverseMap();
            // CreateMap<ApplicationUser, UpdateUserRequestDto>().ReverseMap();
            // CreateMap<ApplicationUser, UpdateProfileRequestDto>().ReverseMap();

            #endregion
        }
    }
}