using System.Web.Mvc;

namespace Ghaforv3.Areas.Admin
{
    public class AdminAreaRegistration : AreaRegistration
    {
        public override string AreaName { get { return "Admin"; } }

        public override void RegisterArea(AreaRegistrationContext context)
        {
            context.MapRoute("AdminHome", "admin", new { controller = "Admin", action = "Index" });

            foreach (var s in new[] { "Products", "Categories", "Orders", "Users", "Banners", "Brands" })
            {
                context.MapRoute("Admin" + s, "admin/" + s.ToLower() + "/{action}/{id}",
                    new { controller = "Admin" + s, action = "Index", id = UrlParameter.Optional });
            }
        }
    }
}