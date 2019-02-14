using IdentityServiceClient.Common.Settings;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServiceClient.Middleware.Authentication
{
    public class AuthenticationMiddleware
    {

        private readonly RequestDelegate next;

        private string _jwtPublicKeyXmlString = String.Empty;
        private string _jwtIssuer = String.Empty;
        private string _jwtAudience = String.Empty;

        private string _jwtCookieName = String.Empty;
        private string _refreshTokenCookieName = String.Empty;
        private int _cookieExpirationHours = 0;

        private string _identityServiceApiUrl = String.Empty;
        private string _identityServiceApiKey = String.Empty;
        private string _refreshTokenEncryptionPassPhrase = String.Empty;

        public AuthenticationMiddleware(RequestDelegate next, IApplicationSettings applicationSettings)
        {
            this.next = next;

            _jwtPublicKeyXmlString = applicationSettings.JSONWebTokens.PublicKeyXmlString;
            _jwtIssuer = applicationSettings.JSONWebTokens.Issuer;
            _jwtAudience = applicationSettings.JSONWebTokens.Audience;

            _jwtCookieName = applicationSettings.Cookies.JwtCookieName;
            _refreshTokenCookieName = applicationSettings.Cookies.RefreshTokenCookieName;
            _cookieExpirationHours = applicationSettings.Cookies.CookieExpirationHours;

            _identityServiceApiUrl = applicationSettings.IdentityService.ApiUrl;
            _identityServiceApiKey = applicationSettings.IdentityService.ApiKey;

            _refreshTokenEncryptionPassPhrase = applicationSettings.JSONWebTokens.RefreshTokenEncryptionPassPhrase;
        }

        public async Task Invoke(HttpContext context /* other dependencies */)
        {
            bool validToken = false;

            if (context.Request.Path.Value.ToLower().StartsWith("/login"))
            {
                // We skip authentication on these paths
                await next.Invoke(context);
            }
            else
            {
                // Validate JWT Token ------------------------
                string jwtToken;
                var cookies = context.Request.Cookies;
                cookies.TryGetValue(_jwtCookieName, out jwtToken);

                var jwtValidationResults = Helpers.DecodeAndValidate.JwtToken(jwtToken, _jwtPublicKeyXmlString, _jwtAudience, _jwtIssuer);

                if(jwtValidationResults.isValid)
                {
                    validToken = true;
                }
                else
                {
                    // =============================================================
                    //  Attempt to refresh the JWT token using the refresh token
                    //==============================================================
                    string refreshToken;
                    cookies.TryGetValue(_refreshTokenCookieName, out refreshToken);
                    if (!String.IsNullOrEmpty(refreshToken))
                    {
                        // UrlDecode the cookie value
                        refreshToken = System.Web.HttpUtility.HtmlDecode(refreshToken);

                        // Decrypt the encrypted refresh token value
                        refreshToken = Common.Encryption.StringEncryption.DecryptString(refreshToken, _refreshTokenEncryptionPassPhrase);

                        using (var httpClient = new System.Net.Http.HttpClient())
                        {
                            httpClient.DefaultRequestHeaders.Add("X-API-KEY", _identityServiceApiKey);

                            var authenticatonClient = new Services.IdentityService.AuthenticationClient(_identityServiceApiUrl, httpClient);
                            var refreshAuthenticationResults = await authenticatonClient.RefreshAsync(new Services.IdentityService.AuthenticateRefreshTokenCommand { RefreshToken = refreshToken });

                            if (refreshAuthenticationResults.IsSuccess.Value)
                            {
                                // Authentication refreshed. Update the JWT token and new refresh token
                                context.Response.Cookies.Delete(_jwtCookieName);
                                context.Response.Cookies.Delete(_refreshTokenCookieName);

                                context.Response.Cookies.Append(
                                  _jwtCookieName,
                                  refreshAuthenticationResults.JwtToken,
                                  new CookieOptions()
                                  {
                                      IsEssential = true,
                                      HttpOnly = true,
                                      Secure = true,
                                      Expires = DateTime.UtcNow.AddHours(_cookieExpirationHours),
                                      SameSite = SameSiteMode.Strict
                                  });

                                // Refresh tokens last longer than JWT tokens and can be used by an attacker to gain access to secure areas even after a JWT expires.
                                // Since every request sends the cookies that belong to the domain we need to store our refresh token in a more secure manner in case they are ever intercepted.
                                // We use a passphrase that is only known to the server to encrypt and decrypt ALL user reresh tokens
                                // In a desktop/native app they should be stored encrypted until ready for use.


                                context.Response.Cookies.Append(
                                  _refreshTokenCookieName,
                                  // Encrypted Token
                                  // Note: You will need to use: System.Web.HttpUtility.UrlDecode(strToDecode) when reading back in
                                  Common.Encryption.StringEncryption.EncryptString(
                                      refreshAuthenticationResults.RefreshToken, _refreshTokenEncryptionPassPhrase
                                      ),
                                  new CookieOptions()
                                  {
                                      IsEssential = true,
                                      HttpOnly = true,
                                      Secure = true,
                                      Expires = DateTime.UtcNow.AddHours(_cookieExpirationHours),
                                      SameSite = SameSiteMode.Strict
                                  });

                                // Set valid to true
                                validToken = true;
                            }
                        }
                    }
                }

                // Redirect or pass on based on results -------------
                if (!validToken)
                {
                    context.Response.Redirect("/login");
                }
                else
                {
                    await next.Invoke(context);
                }
            }
            
        }
    }
}
