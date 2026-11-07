using System.Web;
using System.Web.Mvc;
using Ghaforv3.Models;

namespace Ghaforv3.Security
{
    // [AdminAuthorize] = حداقل ادمین   |   [AdminAuthorize(UserRole.SuperAdmin)] = فقط سوپر ادمین
    public class AdminAuthorizeAttribute : AuthorizeAttribute
    {
        private readonly byte _min;
        public AdminAuthorizeAttribute() { _min = (byte)UserRole.Admin; }
        public AdminAuthorizeAttribute(UserRole minRole) { _min = (byte)minRole; }

        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            var u = CurrentUser.Get(httpContext);
            return u != null && u.IsActive && u.Role >= _min;
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext ctx)
        {
            if (ctx.HttpContext.User.Identity.IsAuthenticated)
                ctx.Result = new RedirectResult("~/account/denied");   // وارد شده ولی دسترسی ندارد
            else
                base.HandleUnauthorizedRequest(ctx);                    // به صفحهٔ ورود می‌رود
        }
    }
}
