using System;
using System.Linq;
using System.Web.Mvc;
using Ghaforv3.Models;
using Ghaforv3.Security;

namespace Ghaforv3.Controllers
{
    public class CartController : Controller
    {
        private readonly Ghaforv3Db db = new Ghaforv3Db();
        private const int MaxPerLine = 99;       // سقف تعداد هر ردیف سبد
        private const int MaxLines = 50;         // سقف تعداد ردیف‌ها (جلوگیری از پر کردن حافظهٔ Session)

        public ActionResult Index()
        {
            var cart = CartStore.Get(Session);
            CartStore.Sync(db, cart);
            return View(cart);
        }

        // با جاوااسکریپت (fetch) صدا زده می‌شود و JSON برمی‌گرداند
        [HttpPost, ValidateAjaxAntiForgeryToken]
        public ActionResult Add(int id, int qty = 1)
        {
            var p = db.Products.FirstOrDefault(x => x.Id == id && x.IsActive);
            if (p == null) return Json(new { ok = false, message = "محصول پیدا نشد" });
            if (p.Stock <= 0) return Json(new { ok = false, message = "این محصول ناموجود است" });

            var cart = CartStore.Get(Session);
            var item = cart.Items.FirstOrDefault(i => i.ProductId == id);
            if (item == null && cart.Items.Count >= MaxLines)
                return Json(new { ok = false, message = "سبد خرید پر است" });

            // سرریز عدد صحیح: qty را قبل از جمع کردن محدود می‌کنیم
            int add = Math.Min(Math.Max(1, qty), MaxPerLine);
            int have = item == null ? 0 : item.Quantity;
            int limit = Math.Min(p.Stock, MaxPerLine);
            int want = Math.Min(have + add, limit);
            string message = "به سبد خرید اضافه شد: " + p.Title;
            if (have + add > limit) message = "حداکثر تعداد قابل سفارش این محصول " + limit + " عدد است";

            if (item == null)
            {
                item = new CartItem { ProductId = p.Id };
                cart.Items.Add(item);
            }
            item.Title = p.Title; item.Slug = p.Slug; item.ImageUrl = p.ImageUrl;
            item.UnitPrice = p.Price; item.Quantity = want;

            return Json(new { ok = true, count = cart.Count, message });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Update(int id, int qty)
        {
            var cart = CartStore.Get(Session);
            var item = cart.Items.FirstOrDefault(i => i.ProductId == id);
            if (item != null)
            {
                if (qty <= 0) cart.Items.Remove(item);
                else
                {
                    int stock = db.Products.Where(p => p.Id == id && p.IsActive).Select(p => p.Stock).FirstOrDefault();
                    if (stock <= 0) cart.Items.Remove(item);                       // دیگر موجود نیست
                    else item.Quantity = Math.Min(Math.Min(qty, stock), MaxPerLine);
                }
            }
            return RedirectToAction("Index");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Remove(int id)
        {
            var cart = CartStore.Get(Session);
            cart.Items.RemoveAll(i => i.ProductId == id);
            return RedirectToAction("Index");
        }

        // عدد روی آیکن سبد؛ داخل Layout با Html.Action صدا زده می‌شود
        [ChildActionOnly]
        public ActionResult Badge()
        {
            return PartialView("_Badge", CartStore.Get(Session).Count);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
