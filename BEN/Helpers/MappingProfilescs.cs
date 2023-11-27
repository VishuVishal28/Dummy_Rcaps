using AutoMapper;
using BEN.DTOs;
using Core.Entities.Identity;

namespace BEN.Helpers
{
    public class MappingProfilescs : Profile
    {
        public MappingProfilescs()
        {
            CreateMap<UserAddress, AddressDto>();
            CreateMap<RegisterDto, AppUser>()
                .ForMember(
                    dest => dest.Email,
                    opt => opt.MapFrom(src => src.Email))
                 .ForMember(
                    dest => dest.TwoFactorEnabled,
                    opt => opt.MapFrom(src => true))
                .ForMember(
                    dest => dest.Created,
                    opt => opt.MapFrom(src => DateTime.UtcNow));
        }
    }
}