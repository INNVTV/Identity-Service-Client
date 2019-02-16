using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServiceClient.Common.Settings;
using IdentityServiceClient.Pages.Password.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Services.IdentityService;

namespace IdentityServiceClient.Pages.Password
{
    public class ResetModel : PageModel
    {
        [BindProperty]
        public ResetPasswordViewModel ResetPassword { get; set; }

        readonly IApplicationSettings _applicationSettings;

        public ResetModel(IServiceProvider serviceProvider, IApplicationSettings applicationSettings)
        {
            _applicationSettings = applicationSettings;
        }

        public async Task<IActionResult> OnGet(string resetCode)
        {
            ResetPassword = new ResetPasswordViewModel();

            using (var httpClient = new System.Net.Http.HttpClient())
            {
                //var invitation = await _mediator.Send(new GetInvitationByIdQuery { Id = id });

                httpClient.DefaultRequestHeaders.Add(_applicationSettings.IdentityService.ApiKeyName, _applicationSettings.IdentityService.ApiKeyValue);

                var passwordClient = new Services.IdentityService.PasswordClient(_applicationSettings.IdentityService.ApiUrl, httpClient);
                var result = await passwordClient.ResetGetAsync(resetCode);

                ResetPassword.ResetRequestValid = result.IsValid.Value;
                if (result.IsValid.Value)
                {
                    ResetPassword.ResetCode = resetCode;
                    ResetPassword.UserId = result.UserId;
                }
            }

            return Page();
        }

        public async Task<IActionResult> OnPost()
        {
            using (var httpClient = new System.Net.Http.HttpClient())
            {
                //var invitation = await _mediator.Send(new GetInvitationByIdQuery { Id = id });

                httpClient.DefaultRequestHeaders.Add(_applicationSettings.IdentityService.ApiKeyName, _applicationSettings.IdentityService.ApiKeyValue);

                var passwordClient = new Services.IdentityService.PasswordClient(_applicationSettings.IdentityService.ApiUrl, httpClient);
                var result = await passwordClient.ResetPostAsync(new ResetPasswordCommand { ResetCode = ResetPassword.ResetCode, UserId = ResetPassword.UserId, NewPassword = ResetPassword.NewPassword, ConfirmNewPassword = ResetPassword.ConfirmNewPassword });

                if (result.IsSuccess.Value)
                {
                    return RedirectToPage("Success");
                }
                else
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

                    return Page();
                }
            }
        }
    }
}