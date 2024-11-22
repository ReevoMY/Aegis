using AutoMapper;
using ByteDash.Manpower.LicenseServer.Entities.Books;
using ByteDash.Manpower.LicenseServer.Services.Dtos.Books;

namespace ByteDash.Manpower.LicenseServer.ObjectMapping;

public class LicenseServerAutoMapperProfile : Profile
{
    public LicenseServerAutoMapperProfile()
    {
        CreateMap<Book, BookDto>();
        CreateMap<CreateUpdateBookDto, Book>();
        CreateMap<BookDto, CreateUpdateBookDto>();
        /* Create your AutoMapper object mappings here */
    }
}
