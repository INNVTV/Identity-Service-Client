using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServiceClient.Common.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServiceClient.Pages.Profile.Edit
{
    public class NameModel : PageModel
    {
        [BindProperty]
        public string FirstName { get; set; }

        [BindProperty]
        public string LastName { get; set; }

        readonly IApplicationSettings _applicationSettings;

        public NameModel(IServiceProvider serviceProvider, IApplicationSettings applicationSettings)
        {
            _applicationSettings = applicationSettings;
        }

        public void OnGet()
        {
            FirstName = User.FindFirst("firstName").Value;
            LastName = User.FindFirst("lastName").Value;
            ViewData["Message"] = String.Empty;
        }

        public async Task<IActionResult> OnPost()
        {
            ViewData["Message"] = String.Empty;

            using (var httpClient = new System.Net.Http.HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add(_applicationSettings.IdentityService.ApiKeyName, _applicationSettings.IdentityService.ApiKeyValue);

                var updateClient = new Services.IdentityService.UpdateClient(_applicationSettings.IdentityService.ApiUrl, httpClient);
                var result = await updateClient.FullNameAsync(new Services.IdentityService.UpdateFullNameCommand { Id = User.FindFirst("id").Value, NewFirstName = FirstName, NewLastName = LastName });

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