
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

            app.UseGoogleAuthentication(
                new GoogleOAuth2AuthenticationOptions
                {
                    ClientId = System.Configuration.ConfigurationManager.AppSettings["GoogleClientId"],
                    ClientSecret = System.Configuration.ConfigurationManager.AppSettings["GoogleClientSecret"],
                    CallbackPath = new PathString("/signin-google")
                });
        }
    }
}
