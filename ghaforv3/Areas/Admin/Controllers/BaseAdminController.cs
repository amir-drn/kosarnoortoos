using System.Web.Mvc;
using Ghaforv3.Models;
using Ghaforv3.Security;

namespace Ghaforv3.Areas.Admin.Controllers
{
    // پایهٔ همهٔ کنترلرهای پنل: ورود ادمین الزامی است و db مشترک است
    [AdminAuthorize]
    [OutputCache(NoStore = true, Duration = 0, VaryByParam = "None")]   // صفحات مدیریت در مرورگر/پروکسی ذخیره نشوند
    public abstract class BaseAdminController : Controller
    {
        protected readonly Ghaforv3Db db = new Ghaforv3Db();

        protected User Me { get { return CurrentUser.Get(HttpContext); } }

        // شمارهٔ صفحه را در بازهٔ درست نگه می‌دارد (page خیلی بزرگ باعث سرریز Skip و خطای ۵۰۰ می‌شد)
        protected static int ClampPage(int page, int pages) { return page < 1 ? 1 : (page > System.Math.Max(1, pages) ? System.Math.Max(1, pages) : page); }

        protected override void Dispose(bool disposing) { if (disposing) db.Dispose(); base.Dispose(disposing); }
    }
}
