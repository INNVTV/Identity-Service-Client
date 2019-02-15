using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServiceClient.Common.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Serilog;

namespace IdentityServiceClient.Pages.Profile.Edit
{
    public class UsernameModel : PageModel
    {
        [BindProperty]
        public string UserName { get; set; }

        readonly IApplicationSettings _applicationSettings;

        public UsernameModel(IServiceProvider serviceProvider, IApplicationSettings applicationSettings)
        {
            _applicationSettings = applicationSettings;
        }

        public void OnGet()
        {
            UserName = User.FindFirst("userName").Value;
            ViewData["Message"] = String.Empty;
        }

        public async Task<IActionResult> OnPost()
        {
            using (var httpClient = new System.Net.Http.HttpClient())
            {
                httpClient.DefaultRequestHeaders.Add(_applicationSettings.IdentityService.ApiKeyName, _applicationSettings.IdentityService.ApiKeyValue);

                var updateClient = new Services.IdentityService.UpdateClient(_applicationSettings.IdentityService.ApiUrl, httpClient);
                var result = await updateClient.UsernameAsync(new Services.IdentityService.UpdateUserNameCommand { Id = User.FindFirst("id").Value, NewUserName = UserName });

                if (!result.IsSuccess.Value)
                {
                    if(result.ValidationIssues != null)
                    {
                        ViewData["Message"] = result.ValidationIssues.FirstOrDefault().PropertyFailures.FirstOrDefault();
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