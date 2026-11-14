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

        private static string Opt(string s)
        {
            s = (s ?? "").Trim();
            return s.Length == 0 || s.Length > ProductOptions.MaxLength ? null : s;
        }

        // با جاوااسکریپت (fetch) صدا زده می‌شود و JSON برمی‌گرداند
        [HttpPost, ValidateAjaxAntiForgeryToken]
        public ActionResult Add(int id, int qty = 1, string body = null, string light = null)
        {
            var p = db.Products.FirstOrDefault(x => x.Id == id && x.IsActive);
            if (p == null) return Json(new { ok = false, message = "محصول پیدا نشد" });
            if (p.Stock <= 0) return Json(new { ok = false, message = "این محصول ناموجود است" });

            // رنگ‌ها فقط از بین گزینه‌هایی که مدیر تعریف کرده پذیرفته می‌شود
            var bodies = p.BodyColorList; var lights = p.LightColorList;
            body = Opt(body); light = Opt(light);
            if (bodies.Count > 0) { if (body == null || !bodies.Contains(body)) return Json(new { ok = false, message = "رنگ بدنه را انتخاب کن" }); }
            else body = null;
            if (lights.Count > 0) { if (light == null || !lights.Contains(light)) return Json(new { ok = false, message = "رنگ نور را انتخاب کن" }); }
            else light = null;

            var cart = CartStore.Get(Session);
            var item = cart.Items.FirstOrDefault(i => i.ProductId == id && i.BodyColor == body && i.LightColor == light);
            if (item == null && cart.Items.Count >= MaxLines)
                return Json(new { ok = false, message = "سبد خرید پر است" });

            // موجودی برای «کل» ردیف‌های همین کالا (با رنگ‌های مختلف) حساب می‌شود
            int others = cart.Items.Where(i => i.ProductId == id && !ReferenceEquals(i, item)).Sum(i => i.Quantity);
            int room = Math.Min(MaxPerLine, p.Stock - others);
            if (room <= 0) return Json(new { ok = false, message = "تمام موجودی این کالا در سبد شماست" });

            // سرریز عدد صحیح: qty را قبل از جمع کردن محدود می‌کنیم
            int add = Math.Min(Math.Max(1, qty), MaxPerLine);
            int have = item == null ? 0 : item.Quantity;
            int want = Math.Min(have + add, room);
            string message = "به سبد خرید اضافه شد: " + p.Title;
            if (have + add > room) message = "حداکثر تعداد قابل سفارش این محصول " + (room + others) + " عدد است";

            if (item == null)
            {
                item = new CartItem { LineId = cart.NextLineId++, ProductId = p.Id, BodyColor = body, LightColor = light };
                cart.Items.Add(item);
            }
            item.Title = p.Title; item.Slug = p.Slug; item.ImageUrl = p.ImageUrl;
            item.UnitPrice = p.PriceFor(body, light); item.Quantity = want;

            return Json(new { ok = true, count = cart.Count, message });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Update(int line, int qty)
        {
            var cart = CartStore.Get(Session);
            var item = cart.Items.FirstOrDefault(i => i.LineId == line);
            if (item != null)
            {
                if (qty <= 0) cart.Items.Remove(item);
                else
                {
                    int pid = item.ProductId;
                    int stock = db.Products.Where(p => p.Id == pid && p.IsActive).Select(p => p.Stock).FirstOrDefault();
                    int others = cart.Items.Where(i => i.ProductId == pid && !ReferenceEquals(i, item)).Sum(i => i.Quantity);
                    int room = Math.Min(MaxPerLine, stock - others);
                    if (room <= 0) cart.Items.Remove(item);                       // دیگر موجود نیست
                    else item.Quantity = Math.Min(qty, room);
                }
            }
            return RedirectToAction("Index");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Remove(int line)
        {
            var cart = CartStore.Get(Session);
            cart.Items.RemoveAll(i => i.LineId == line);
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
