using System;
using System.Web.Mvc;
using Ghaforv3.Helpers;
using Ghaforv3.Models;
using Ghaforv3.Security;

namespace Ghaforv3.Controllers
{
    // پایهٔ کنترلرهای خرید: ورود کاربر فعال الزامی است
    [UserAuthorize]
    [OutputCache(NoStore = true, Duration = 0, VaryByParam = "None")]
    public abstract class ShopController : Controller
    {
        protected readonly Ghaforv3Db db = new Ghaforv3Db();

        protected User Me { get { return CurrentUser.Get(HttpContext); } }

        // پرداخت را شروع می‌کند و کاربر را به درگاه می‌فرستد
        protected ActionResult StartPayment(Order order)
        {
            if (LoginThrottle.Hit("pay", Me.Id.ToString(), 15, TimeSpan.FromMinutes(10)))
            {
                TempData["err"] = "تلاش‌های پرداخت زیاد بود؛ چند دقیقه بعد دوباره امتحان کن";
                return RedirectToAction("Details", "Orders", new { id = order.Id });
            }
            var gw = Gateways.Current(Request);
            if (gw == null)
            {
                TempData["err"] = "درگاه پرداخت در دسترس نیست. با پشتیبانی تماس بگیرید.";
                return RedirectToAction("Details", "Orders", new { id = order.Id });
            }
            var origin = Site.Origin(Request);
            var start = gw.Start(order, origin + Url.Action("Callback", "Payment"), Me.Mobile, origin + Url.Action("Demo", "Payment"));
            if (!start.Ok)
            {
                TempData["err"] = start.Error;
                return RedirectToAction("Details", "Orders", new { id = order.Id });
            }
            db.Payments.Add(new Payment
            {
                OrderId = order.Id, Amount = order.TotalAmount, Gateway = gw.Name,
                RefNumber = start.Authority, IsSuccess = false, CreatedAt = DateTime.Now
            });
            db.SaveChanges();
            return Redirect(start.RedirectUrl);
        }

        protected override void Dispose(bool disposing) { if (disposing) db.Dispose(); base.Dispose(disposing); }
    }
}
