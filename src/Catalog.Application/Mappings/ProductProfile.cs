namespace Catalog.Application.Mappings;

using AutoMapper;
using Catalog.Application.DTOs;
using Catalog.Domain.Entities;
using Catalog.Domain.ValueObjects;

public class ProductProfile : Profile
{
    public ProductProfile()
    {
        // Entity → DTO (Money flattening)
        CreateMap<Product, ProductDto>()
            .ForMember(dest => dest.Price,    opt => opt.MapFrom(src => src.Price.Amount))
            .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Price.Currency));
    }
}