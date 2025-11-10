using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SmartExpenseTracker.Areas.Identity.Pages.Account
{
    [AllowAnonymous]
    public class RegisterConfirmationModel : PageModel
    {
        public string Email { get; set; } = string.Empty;
        public bool RequiresApproval { get; set; } = true;

        public IActionResult OnGet(string email, string returnUrl = null, bool requiresApproval = true)
        {
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToPage("/Index");
            }

            Email = email;
            RequiresApproval = requiresApproval;

            return Page();
        }
    }
}
