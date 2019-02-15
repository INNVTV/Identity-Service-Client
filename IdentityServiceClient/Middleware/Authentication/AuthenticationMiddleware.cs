using IdentityServiceClient.Common.Settings;
using IdentityServiceClient.Middleware.Authentication.Helpers;
using Microsoft.AspNetCore.Http;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
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
        private string _identityServiceApiKeyName = String.Empty;
        private string _identityServiceApiKeyValue = String.Empty;
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
            _identityServiceApiKeyName = applicationSettings.IdentityService.ApiKeyName;
            _identityServiceApiKeyValue = applicationSettings.IdentityService.ApiKeyValue;

            _refreshTokenEncryptionPassPhrase = applicationSettings.JSONWebTokens.RefreshTokenEncryptionPassPhrase;
        }

        public async Task Invoke(HttpContext context /* other dependencies */)
        {
            //Log.Information("Authentication Middleware: {path}", context.Request.Path.Value.ToLower());

            bool validToken = false;

            if (   context.Request.Path.Value.ToLower().StartsWith("/login")
                || context.Request.Path.Value.ToLower().StartsWith("/error"))
            {
                // We skip authentication on these paths
                await next.Invoke(context);
            }
            else
            {
 
                var jwtValidationResults = new DecodeAndValidateJwtTokenResults { isValid = false };

                // Validate JWT Token ------------------------
                string jwtToken = String.Empty;
                var cookies = context.Request.Cookies;
                cookies.TryGetValue(_jwtCookieName, out jwtToken);

                //Log.Information("Authentication Middleware: JWT is validating... {jwt}", jwtToken);

                if (!String.IsNullOrEmpty(jwtToken))
                {
                    jwtValidationResults = Helpers.DecodeAndValidate.JwtToken(jwtToken, _jwtPublicKeyXmlString, _jwtAudience, _jwtIssuer);
                }
                else
                {
                    //Log.Information("Authentication Middleware: JWT does not exist");
                }

                if (jwtValidationResults.isValid)
                {
                    //Log.Information("Authentication Middleware: JWT is valid");

                    validToken = true;
                }
                else
                {
                    //Log.Information("Authentication Middleware: JWT is invalid - expired:{expired} | isSuccess:{isSuccess}",jwtValidationResults.isExpired, jwtValidationResults.isValid);

                    // JWT token is either invalid, expired or does not exist.
                    // Note: It may have been purposfully destroyed due to an editing action in the profile, forcing a use of the refreshToken

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

                        //Log.Information("Authentication Middleware: Attempting refresh using token: {token}", refreshToken);

                        using (var httpClient = new System.Net.Http.HttpClient())
                        {
                            httpClient.DefaultRequestHeaders.Add(_identityServiceApiKeyName, _identityServiceApiKeyValue);

                            var authenticatonClient = new Services.IdentityService.AuthenticationClient(_identityServiceApiUrl, httpClient);
                            var refreshAuthenticationResults = await authenticatonClient.RefreshAsync(new Services.IdentityService.AuthenticateRefreshTokenCommand { RefreshToken = refreshToken });

                            if (refreshAuthenticationResults.IsSuccess.Value)
                            {
                                // Authentication refreshed!
                                //-----------------------------------

                                //Log.Information("Authentication Middleware: Refresh token: {token} success", refreshToken);
                                //Log.Information("Authentication Middleware: New refresh token: {token}", refreshAuthenticationResults.RefreshToken);
                                
                                // Revalidate and Rehydrate claims on the user
                                jwtValidationResults = Helpers.DecodeAndValidate.JwtToken(refreshAuthenticationResults.JwtToken, _jwtPublicKeyXmlString, _jwtAudience, _jwtIssuer);

                                //Update the JWT token and new refresh token
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
                            else
                            {
                                //Log.Warning("Authentication Middleware: Refresh token: {token} did not succeed: {message}", refreshToken, refreshAuthenticationResults.Message);
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
                    // Pass claims into the user context (ClaimsPrincipal)
                    context.User = jwtValidationResults.ClaimsPrincipal;

                    // Continue our action
                    await next.Invoke(context);
                }
            }
            
        }
    }
}
