using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;

namespace Ghaforv3.Security
{
    // محافظت CSRF برای درخواست‌های fetch/AJAX: توکن در هدر X-CSRF-Token می‌آید و با کوکی تطبیق داده می‌شود
    [System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Method, AllowMultiple = false)]
    public sealed class ValidateAjaxAntiForgeryTokenAttribute : FilterAttribute, IAuthorizationFilter
    {
        public void OnAuthorization(AuthorizationContext filterContext)
        {
            var req = filterContext.HttpContext.Request;
            var cookie = req.Cookies[AntiForgeryConfig.CookieName];
            var header = req.Headers["X-CSRF-Token"];
            try
            {
                AntiForgery.Validate(cookie == null ? null : cookie.Value, header);
            }
            catch (HttpAntiForgeryException)
            {
                filterContext.HttpContext.Response.StatusCode = 403;
                filterContext.HttpContext.Response.TrySkipIisCustomErrors = true;   // بدنهٔ JSON با صفحهٔ خطای IIS جایگزین نشود
                filterContext.Result = new JsonResult
                {
                    Data = new { ok = false, message = "نشست منقضی شده؛ صفحه را دوباره بارگذاری کن" },
                    JsonRequestBehavior = JsonRequestBehavior.AllowGet
                };
            }
        }
    }
}
