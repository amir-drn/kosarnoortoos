using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace Ghaforv3.Security
{
    // فقط کاربر واردشدهٔ «فعال». اگر حساب غیرفعال شده باشد، از حساب خارج می‌شود.
    public class UserAuthorizeAttribute : AuthorizeAttribute
    {
        protected override bool AuthorizeCore(HttpContextBase httpContext)
        {
            var u = CurrentUser.Get(httpContext);
            return u != null && u.IsActive;
        }

        protected override void HandleUnauthorizedRequest(AuthorizationContext ctx)
        {
            if (ctx.HttpContext.User.Identity.IsAuthenticated) FormsAuthentication.SignOut();
            base.HandleUnauthorizedRequest(ctx);
        }
    }
}
