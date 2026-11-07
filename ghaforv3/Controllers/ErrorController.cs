using System.Web.Mvc;

namespace Ghaforv3.Controllers
{
    // صفحه‌های خطای ساده و بدون وابستگی به دیتابیس (اگر خود دیتابیس خراب باشد هم نمایش داده می‌شوند)
    [AllowAnonymous]
    public class ErrorController : Controller
    {
        public ActionResult Index() { return Fail(500, "مشکلی پیش آمد", "خطایی در سرور رخ داد. چند لحظه بعد دوباره امتحان کن."); }
        public ActionResult NotFound() { return Fail(404, "صفحه پیدا نشد", "آدرسی که وارد کردی وجود ندارد یا جابه‌جا شده است."); }
        public ActionResult Forbidden() { return Fail(403, "دسترسی ممکن نیست", "اجازهٔ دیدن این بخش را نداری."); }

        private ActionResult Fail(int code, string title, string text)
        {
            Response.StatusCode = code;
            Response.TrySkipIisCustomErrors = true;     // بدون این، IIS دوباره همین صفحه را صدا می‌زند (حلقه)
            ViewBag.Code = code; ViewBag.ErrTitle = title; ViewBag.ErrText = text;
            return View("~/Views/Shared/Error.cshtml");
        }
    }
}
