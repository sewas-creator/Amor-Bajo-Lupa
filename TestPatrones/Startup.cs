using System;
using System.Configuration;
using System.Security.Claims;
using System.Web.Helpers;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.Google;
using Owin;

[assembly: OwinStartup(typeof(TestPatrones.Startup))]

namespace TestPatrones
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            AntiForgeryConfig.UniqueClaimTypeIdentifier = ClaimTypes.NameIdentifier;

            app.SetDefaultSignInAsAuthenticationType(
                CookieAuthenticationDefaults.AuthenticationType);

            app.UseCookieAuthentication(
                new CookieAuthenticationOptions
                {
                    AuthenticationType =
                        CookieAuthenticationDefaults.AuthenticationType,
                    LoginPath = new PathString("/Cuenta/Acceso")
                });

            // En Somee se leen desde App_Data\GoogleAuth.secrets.config.
            // Las variables de entorno son opcionales y tienen prioridad.
            var clientId = GetSetting("GOOGLE_CLIENT_ID", "GoogleClientId");
            var clientSecret = GetSetting("GOOGLE_CLIENT_SECRET", "GoogleClientSecret");

            app.UseGoogleAuthentication(
                new GoogleOAuth2AuthenticationOptions
                {
                    ClientId = clientId,
                    ClientSecret = clientSecret,
                    CallbackPath = new PathString("/signin-google")
                });
        }

        private static string GetSetting(string environmentName, string appSettingName)
        {
            string value = Environment.GetEnvironmentVariable(environmentName);

            if (string.IsNullOrWhiteSpace(value))
            {
                value = ConfigurationManager.AppSettings[appSettingName];
            }

            if (string.IsNullOrWhiteSpace(value))
            {
                throw new InvalidOperationException(
                    "Falta configurar " + appSettingName +
                    " en App_Data/GoogleAuth.secrets.config o como variable de entorno " +
                    environmentName + ".");
            }

            return value.Trim();
        }
    }
}
