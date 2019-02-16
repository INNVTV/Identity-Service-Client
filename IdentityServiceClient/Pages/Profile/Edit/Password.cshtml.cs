using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServiceClient.Common.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServiceClient.Pages.Profile.Edit
{
    public class PasswordModel : PageModel
    {
        [BindProperty]
        public string Password { get; set; }

        [BindProperty]
        public string NewPassword { get; set; }

        [BindProperty]
        public string ConfirmNewPassword { get; set; }

        readonly IApplicationSettings _applicationSettings;

        public PasswordModel(IServiceProvider serviceProvider, IApplicationSettings applicationSettings)
        {
            _applicationSettings = applicationSettings;
        }

        public void OnGet()
        {
            Password = String.Empty;
            NewPassword = String.Empty;
            ConfirmNewPassword = String.Empty;
            ViewData["Message"] = String.Empty;
        }

        public async Task<IActionResult> OnPost()
        {
            ViewData["Message"] = String.Empty;

            using (var httpClient = new System.Net.Http.HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add(_applicationSettings.IdentityService.ApiKeyName, _applicationSettings.IdentityService.ApiKeyValue);

                var updateClient = new Services.IdentityService.UpdateClient(_applicationSettings.IdentityService.ApiUrl, httpClient);
                var result = await updateClient.PasswordAsync(new Services.IdentityService.UpdatePasswordCommand { Id = User.FindFirst("id").Value, OldPassword = Password, NewPassword = NewPassword, ConfirmNewPassword = ConfirmNewPassword });

                if (!result.IsSuccess.Value)
                {
                    if (result.ValidationIssues != null)
                    {
                        foreach (var validationProperty in result.ValidationIssues)
                        {
                            foreach (var propertyFailure in validationProperty.PropertyFailures)
                            {
                                ModelState.AddModelError(validationProperty.PropertyName, propertyFailure);
                            }
                        }

                        return Page();
                    }
                    else
                    {
                        ViewData["Message"] = result.Message;
                    }

                    return Page();
                }

            }

            // Delete JWT from cookies to force refresh of claims (using refresh token) in authentication midleware
            Response.Cookies.Delete(_applicationSettings.Cookies.JwtCookieName);

            return Redirect("/profile");
        }
    }
}