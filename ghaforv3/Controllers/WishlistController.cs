using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using Ghaforv3.Models;
using Ghaforv3.Security;

namespace Ghaforv3.Controllers
{
    // لیست علاقه‌مندی‌ها (جدول Wishlists؛ رابطهٔ چند-به-چند User.Products)
    public class WishlistController : Controller
    {
        private readonly Ghaforv3Db db = new Ghaforv3Db();
        private const int Max = 200;

        [UserAuthorize]
        public ActionResult Index()
        {
            var id = CurrentUser.Get(HttpContext).Id;
            var list = db.Users.Where(u => u.Id == id).SelectMany(u => u.Products)
                .Where(p => p.IsActive).Include(p => p.Category).ToList();
            return View(list);
        }

        private ActionResult NeedLogin()
        {
            Response.StatusCode = 401;
            Response.TrySkipIisCustomErrors = true;
            return Json(new { ok = false, login = true, message = "برای ذخیرهٔ علاقه‌مندی‌ها وارد شو" });
        }

        // شناسهٔ محصولات علاقه‌مند (برای پر کردن قلب‌ها بعد از بارگذاری صفحه)
        public ActionResult Ids()
        {
            var me = CurrentUser.Get(HttpContext);
            if (me == null || !me.IsActive) return NeedLogin();
            var ids = db.Users.Where(u => u.Id == me.Id).SelectMany(u => u.Products).Select(p => p.Id).ToList();
            return Json(new { ok = true, ids }, JsonRequestBehavior.AllowGet);
        }

        [HttpPost, ValidateAjaxAntiForgeryToken]
        public ActionResult Toggle(int id)
        {
            var me = CurrentUser.Get(HttpContext);
            if (me == null || !me.IsActive) return NeedLogin();

            var user = db.Users.Include(u => u.Products).First(u => u.Id == me.Id);
            var existing = user.Products.FirstOrDefault(p => p.Id == id);
            bool liked;
            if (existing != null) { user.Products.Remove(existing); liked = false; }
            else
            {
                if (user.Products.Count >= Max) return Json(new { ok = false, message = "لیست علاقه‌مندی‌ها پر است" });
                var p = db.Products.FirstOrDefault(x => x.Id == id && x.IsActive);
                if (p == null) return Json(new { ok = false, message = "محصول پیدا نشد" });
                user.Products.Add(p); liked = true;
            }
            db.SaveChanges();
            return Json(new { ok = true, liked, message = liked ? "به علاقه‌مندی‌ها اضافه شد" : "از علاقه‌مندی‌ها برداشته شد" });
        }

        protected override void Dispose(bool disposing) { if (disposing) db.Dispose(); base.Dispose(disposing); }
    }
}
