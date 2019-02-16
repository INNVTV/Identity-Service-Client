using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServiceClient.Common.Settings;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace IdentityServiceClient.Pages.Password
{
    public class ForgotModel : PageModel
    {
        [BindProperty]
        public string UserNameOrEmail { get; set; }

        readonly IApplicationSettings _applicationSettings;

        public ForgotModel(IServiceProvider serviceProvider, IApplicationSettings applicationSettings)
        {
            _applicationSettings = applicationSettings;
        }

        public void OnGet()
        {
            ViewData["Message"] = String.Empty;
        }

        public async Task<IActionResult> OnPost()
        {
            if (!String.IsNullOrEmpty(UserNameOrEmail))
            {
                
                using (var httpClient = new System.Net.Http.HttpClient())
                {
                    //var invitation = await _mediator.Send(new GetInvitationByIdQuery { Id = id });

                    httpClient.DefaultRequestHeaders.Add(_applicationSettings.IdentityService.ApiKeyName, _applicationSettings.IdentityService.ApiKeyValue);

                    var passwordClient = new Services.IdentityService.PasswordClient(_applicationSettings.IdentityService.ApiUrl, httpClient);
                    var result = await passwordClient.ForgotAsync(UserNameOrEmail);

                    if(result.IsSuccess.Value)
                    {
                        return RedirectToPage("Sent");
                    }
                    else
                    {
                        ViewData["Message"] = result.Message;
                        return Page();
                    }
                }
            }
            else
            {
                ViewData["Message"] = "Please submit a username or password!";
                return Page();
            }
        }
    }
}