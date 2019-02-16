using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServiceClient.Common.Settings;
using IdentityServiceClient.Pages.Invitations.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Services.IdentityService;

namespace IdentityServiceClient.Pages.Invitations
{
    public class AcceptModel : PageModel
    {
        [BindProperty]
        public AcceptInvitationViewModel AcceptInvitation { get; set; }

        readonly IApplicationSettings _applicationSettings;

        public AcceptModel(IServiceProvider serviceProvider, IApplicationSettings applicationSettings)
        {
            _applicationSettings = applicationSettings;
        }

        public async Task<IActionResult> OnGet(string id)
        {
            try
            {
                using (var httpClient = new System.Net.Http.HttpClient())
                {
                    //var invitation = await _mediator.Send(new GetInvitationByIdQuery { Id = id });

                    httpClient.DefaultRequestHeaders.Add(_applicationSettings.IdentityService.ApiKeyName, _applicationSettings.IdentityService.ApiKeyValue);

                    var invitationClient = new Services.IdentityService.InvitationsClient(_applicationSettings.IdentityService.ApiUrl, httpClient);
                    var invitation = await invitationClient.IdAsync(id);

                    if (invitation != null && invitation.Id == id)
                    {
                        AcceptInvitation = new AcceptInvitationViewModel
                        {
                            InvitationExists = true,
                            InvitationId = invitation.Id,
                            InvitationExpired = invitation.IsExpired.Value
                        };
                    }
                    else
                    {
                        AcceptInvitation = new AcceptInvitationViewModel
                        {
                            InvitationExists = false
                        };
                    }
                }
            }
            catch
            {
                AcceptInvitation = new AcceptInvitationViewModel
                {
                    InvitationExists = false
                };
            }

            return Page();
        }

        public async Task<IActionResult> OnPost()
        {
            using (var httpClient = new System.Net.Http.HttpClient())
            {
                //var invitation = await _mediator.Send(new GetInvitationByIdQuery { Id = id });

                httpClient.DefaultRequestHeaders.Add(_applicationSettings.IdentityService.ApiKeyName, _applicationSettings.IdentityService.ApiKeyValue);

                var invitationClient = new Services.IdentityService.InvitationsClient(_applicationSettings.IdentityService.ApiUrl, httpClient);
                var invitation = await invitationClient.IdAsync(AcceptInvitation.InvitationId);

                var userClient = new Services.IdentityService.UsersClient(_applicationSettings.IdentityService.ApiUrl, httpClient);
                var createUserResponse = await userClient.CreateAsync(new CreateUserServiceModel
                {
                    // From Invitation
                    Email = invitation.Email,
                    Roles = invitation.Roles,

                    // From User Submission
                    UserName = AcceptInvitation.UserName,
                    FirstName = AcceptInvitation.FirstName,
                    LastName = AcceptInvitation.LastName,
                    Password = AcceptInvitation.Password,
                    ConfirmPassword = AcceptInvitation.ConfirmPassword
                });

                if (createUserResponse.IsSuccess.Value)
                {
                    // Delete invitation
                    await invitationClient.DeleteAsync(AcceptInvitation.InvitationId);

                    // Send to success page
                    return RedirectToPage("Success");
                }
                else
                {
                    if (createUserResponse.ValidationIssues != null)
                    {
                        foreach (var validationProperty in createUserResponse.ValidationIssues)
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