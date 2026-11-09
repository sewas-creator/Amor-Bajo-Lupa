
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace TestPatrones
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_BeginRequest()
        {
            var request = Context.Request;

            if (request.Url.Host.Equals(
                "amorbajolupaweb.somee.com",
                StringComparison.OrdinalIgnoreCase)
                && !request.IsSecureConnection)
            {
                var secureUrl = new UriBuilder(request.Url)
                {
                    Scheme = Uri.UriSchemeHttps,
                    Port = 443
                };

                Context.Response.RedirectPermanent(
                    secureUrl.Uri.ToString(), false);

                Context.ApplicationInstance.CompleteRequest();
            }
        }

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }
    }
}
