using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace Ghaforv3
{
    public class MvcApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            AntiForgeryConfig.CookieName = "GH3_AF";                // کوکی جدا از پروژهٔ دیگر روی localhost
            MvcHandler.DisableMvcResponseHeader = true;            // هدر X-AspNetMvc-Version حذف شود
            AntiForgeryConfig.SuppressXFrameOptionsHeader = true;   // X-Frame-Options را خودمان در Web.config گذاشته‌ایم
        }

        // هدرهایی که اطلاعات سرور را لو می‌دهند حذف می‌شوند
        protected void Application_PreSendRequestHeaders()
        {
            if (Response == null || Response.Headers == null) return;
            try
            {
                Response.Headers.Remove("Server");
                Response.Headers.Remove("X-AspNet-Version");
                Response.Headers.Remove("X-Powered-By");
            }
            catch (PlatformNotSupportedException) { }
        }
    }
}
