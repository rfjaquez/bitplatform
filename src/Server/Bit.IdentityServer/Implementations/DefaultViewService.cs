﻿using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Bit.Core.Contracts;
using Bit.Core.Implementations;
using Bit.Owin.Contracts;
using IdentityServer3.Core.Models;
using IdentityServer3.Core.Services;
using IdentityServer3.Core.Validation;
using IdentityServer3.Core.ViewModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Bit.IdentityServer.Implementations
{
    public class DefaultViewService : IViewService
    {
        public virtual IHtmlPageProvider HtmlPageProvider { get; set; }

        public virtual IUrlStateProvider UrlStateProvider { get; set; }

        public virtual IPathProvider PathProvider { get; set; }

        public virtual IAppEnvironmentProvider AppEnvironmentProvider { get; set; }

        public virtual Task<Stream> ClientPermissions(ClientPermissionsViewModel model)
        {
            string content = @"<!DOCTYPE html>
                            <html>
                                <head>
                                    <title>ClientPermissions >> Not Implemented</title>
                                </head>
                                <body>ClientPermissions >> Not Implemented</body>
                            </html>";

            return ReturnHtmlAsync(model, content, CancellationToken.None);
        }

        public virtual Task<Stream> Consent(ConsentViewModel model, ValidatedAuthorizeRequest authorizeRequest)
        {
            string content = @"<!DOCTYPE html>
                            <html>
                                <head>
                                    <title>Consent >> Not Implemented</title>
                                </head>
                                <body>Consent >> Not Implemented</body>
                            </html>";

            return ReturnHtmlAsync(model, content, CancellationToken.None);
        }

        public virtual Task<Stream> Error(ErrorViewModel model)
        {
            string content = $@"<!DOCTYPE html>
                            <html>
                                <head>
                                    <title>{model.ErrorMessage}</title>
                                </head>
                                <body>{model.ErrorMessage} <br /> RequestId: {model.RequestId}</body>
                            </html>";

            return ReturnHtmlAsync(model, content, CancellationToken.None);
        }

        public virtual Task<Stream> LoggedOut(LoggedOutViewModel model, SignOutMessage message)
        {
            string content = null;

            string url = model?.RedirectUrl ?? message?.ReturnUrl;

            if (!string.IsNullOrEmpty(url))
            {
                content = $@"<!DOCTYPE html>
                            <html>
                                <head>
                                    <meta http-equiv='refresh' content='0;{url}'>
                                </head>
                                <body></body>
                            </html>";
            }
            else
            {
                content = $@"<!DOCTYPE html>
                            <html>
                                <head>
                                    <title>No redirect url on logout</title>
                                </head>
                                <body>
                                    No redirect url on logout. Perhaps your redirect url is not listed in {nameof(ClientProvider.GetClients)} of {nameof(ClientProvider)}
                                </body>
                            </html>";
            }

            return ReturnHtmlAsync(model, content, CancellationToken.None);
        }

        public virtual async Task<Stream> Login(LoginViewModel model, SignInMessage message)
        {
            JsonSerializerSettings jsonSerSettings = DefaultJsonContentFormatter.SerializeSettings();
            jsonSerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();

            if (model.Custom == null && message.ReturnUrl != null)
            {
                try
                {
                    dynamic custom = model.Custom = UrlStateProvider.GetState(new Uri(message.ReturnUrl));

                    string signInType = null;

                    try
                    {
                        signInType = custom.SignInType;
                    }
                    catch { }

                    if (signInType != null && model.ExternalProviders.Any(extProvider => extProvider.Type == signInType))
                    {
                        string redirectUri = model.ExternalProviders.Single(extProvider => extProvider.Type == signInType).Href;

                        return await ReturnHtmlAsync(model, $@"<!DOCTYPE html>
                            <html>
                                <head>
                                    <meta http-equiv='refresh' content='0;{redirectUri}'>
                                </head>
                                <body></body>
                            </html>", CancellationToken.None).ConfigureAwait(false);
                    }
                }
                catch
                {
                    model.Custom = new { };
                }
            }

            string json = JsonConvert.SerializeObject(new
            {
                model.AdditionalLinks,
                model.AllowRememberMe,
                model.AntiForgery,
                model.ClientLogoUrl,
                model.ClientName,
                model.ClientUrl,
                model.CurrentUser,
                model.Custom,
                model.ErrorMessage,
                model.ExternalProviders,
                model.LoginUrl,
                model.LogoutUrl,
                model.RememberMe,
                model.RequestId,
                model.SiteName,
                model.SiteUrl,
                model.Username,
                ReturnUrl = message.ReturnUrl == null ? "" : new Uri(message.ReturnUrl).ParseQueryString()["redirect_uri"]
            }, Formatting.None, jsonSerSettings);

            string loginPageHtmlInitialHtml = File.ReadAllText(PathProvider.StaticFileMapPath(AppEnvironmentProvider.GetActiveAppEnvironment().GetConfig("LoginPagePath", "loginPage.html")));

            string loginPageHtmlFinalHtml = (await HtmlPageProvider.GetHtmlPageAsync(loginPageHtmlInitialHtml, CancellationToken.None).ConfigureAwait(false))
                .Replace("{{model.LoginModel.toJson()}}", Microsoft.Security.Application.Encoder.HtmlEncode(json));

            return await ReturnHtmlAsync(model, loginPageHtmlFinalHtml, CancellationToken.None).ConfigureAwait(false);
        }

        private async Task<Stream> ReturnHtmlAsync(CommonViewModel model, string html, CancellationToken cancellationToken)
        {
            MemoryStream viewStream = new MemoryStream();

            StreamWriter writter = new StreamWriter(viewStream);

            await writter.WriteAsync(html).ConfigureAwait(false);

            await writter.FlushAsync().ConfigureAwait(false);

            viewStream.Seek(0, SeekOrigin.Begin);

            return viewStream;
        }

        public virtual Task<Stream> Logout(LogoutViewModel model, SignOutMessage message)
        {
            // Based on current InvokeLogOut Middleware, this method will not be called, because of context.Authentication.SignOut("custom", "Bearer"); code.

            string content = $@"<!DOCTYPE html>
                            <html>
                                <body>
                                    <form id='logoutForm' method='post' action='{model.LogoutUrl}'>
                                        <input type='hidden' name='{model.AntiForgery.Name}' value='{model.AntiForgery.Value}'>
                                    </form>
                                    <script>
                                        document.getElementById('logoutForm').submit();
                                    </script>
                                </body>
                            </html>";

            return ReturnHtmlAsync(model, content, CancellationToken.None);
        }
    }
}
