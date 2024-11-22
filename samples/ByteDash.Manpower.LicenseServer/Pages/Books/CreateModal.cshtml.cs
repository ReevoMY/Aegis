using System.Threading.Tasks;
using ByteDash.Manpower.LicenseServer.Services.Books;
using ByteDash.Manpower.LicenseServer.Services.Dtos.Books;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.Mvc.UI.RazorPages;

namespace ByteDash.Manpower.LicenseServer.Pages.Books
{
    public class CreateModalModel : AbpPageModel
    {
        [BindProperty]
        public CreateUpdateBookDto Book { get; set; }

        private readonly IBookAppService _bookAppService;

        public CreateModalModel(IBookAppService bookAppService)
        {
            _bookAppService = bookAppService;
        }

        public void OnGet()
        {
            Book = new CreateUpdateBookDto();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await _bookAppService.CreateAsync(Book);
            return NoContent();
        }
    }
}