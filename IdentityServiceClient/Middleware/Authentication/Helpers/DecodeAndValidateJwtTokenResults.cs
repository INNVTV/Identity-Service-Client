using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace IdentityServiceClient.Middleware.Authentication.Helpers
{
    public class DecodeAndValidateJwtTokenResults
    {
        public DecodeAndValidateJwtTokenResults()
        {
            isValid = false;
            isExpired = false;
        }

        public bool isValid { get; set; }
        public bool isExpired { get; set; }
        public ClaimsPrincipal ClaimsPrincipal { get; set; }
        public SecurityToken SecurityToken { get; set; }
    }
}