namespace Catalog.Application.Mappings;

using AutoMapper;
using Catalog.Application.DTOs;
using Catalog.Domain.Entities;

public class CatalogueProfile : Profile
{
    public CatalogueProfile()
    {
        // Entity → DTO
        CreateMap<Catalogue, CatalogueDto>();

        // Request → Entity
        CreateMap<CreateCatalogueRequest, Catalogue>()
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(_ => true));

        CreateMap<UpdateCatalogueRequest, Catalogue>()
            .ForAllMembers(opt => opt.Condition((src, dest, val) => val is not null));
    }
}