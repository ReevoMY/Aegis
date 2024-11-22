using System;
using ByteDash.Manpower.LicenseServer.Permissions;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;
using ByteDash.Manpower.LicenseServer.Services.Dtos.Books;
using ByteDash.Manpower.LicenseServer.Entities.Books;

namespace ByteDash.Manpower.LicenseServer.Services.Books;

public class BookAppService :
    CrudAppService<
        Book, //The Book entity
        BookDto, //Used to show books
        Guid, //Primary key of the book entity
        PagedAndSortedResultRequestDto, //Used for paging/sorting
        CreateUpdateBookDto>, //Used to create/update a book
    IBookAppService //implement the IBookAppService
{
    public BookAppService(IRepository<Book, Guid> repository)
        : base(repository)
    {
        GetPolicyName = LicenseServerPermissions.Books.Default;
        GetListPolicyName = LicenseServerPermissions.Books.Default;
        CreatePolicyName = LicenseServerPermissions.Books.Create;
        UpdatePolicyName = LicenseServerPermissions.Books.Edit;
        DeletePolicyName = LicenseServerPermissions.Books.Delete;
    }
}