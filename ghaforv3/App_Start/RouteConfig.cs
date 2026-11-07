using System.Web.Mvc;
using System.Web.Routing;

namespace Ghaforv3
{
    public class RouteConfig
    {
        // کنترلرهای پنل ادمین داخل Area هستند؛ این مسیرها فقط کنترلرهای این namespace را می‌بینند
        private static readonly string[] Root = { "Ghaforv3.Controllers" };

        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapRoute("Robots", "robots.txt", new { controller = "Seo", action = "Robots" }, new[] { "Ghaforv3.Controllers" });
            routes.MapRoute("Sitemap", "sitemap.xml", new { controller = "Seo", action = "Sitemap" }, new[] { "Ghaforv3.Controllers" });
            routes.MapRoute("StaticPages", "{action}", new { controller = "Pages" },
                new { action = "about|contact|terms|privacy|shipping" }, new[] { "Ghaforv3.Controllers" });

            routes.MapRoute("Product", "product/{slug}", new { controller = "Products", action = "Details" }, Root);
            routes.MapRoute("Shop", "shop/{category}", new { controller = "Products", action = "Index", category = UrlParameter.Optional }, Root);
            routes.MapRoute("Cart", "cart/{action}", new { controller = "Cart", action = "Index" }, Root);

            routes.MapRoute("Login", "login", new { controller = "Account", action = "Login" }, Root);
            // میان‌برها: /admin/login و /admin/setup
            routes.MapRoute("AdminLogin", "admin/login", new { controller = "Account", action = "Login" }, Root);
            routes.MapRoute("AdminSetup", "admin/setup", new { controller = "Account", action = "Setup" }, Root);

            routes.MapRoute("Default", "{controller}/{action}/{id}",
                new { controller = "Home", action = "Index", id = UrlParameter.Optional }, Root);
        }
    }
}
