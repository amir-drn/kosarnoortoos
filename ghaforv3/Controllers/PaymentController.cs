using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using Ghaforv3.Helpers;
using Ghaforv3.Models;

namespace Ghaforv3.Controllers
{
    // بازگشت از درگاه پرداخت. مبلغ و وضعیت همیشه از سرور درگاه «تأیید» می‌شود، نه از پارامترهای آدرس.
    public class PaymentController : Controller
    {
        private readonly Ghaforv3Db db = new Ghaforv3Db();

        private ActionResult Done(Order order)
        {
            return RedirectToAction("Details", "Orders", new { id = order.Id });
        }

        public ActionResult Callback(string authority, string status)
        {
            if (string.IsNullOrEmpty(authority) || authority.Length > 100) return HttpNotFound();
            var pay = db.Payments.Include(p => p.Order).FirstOrDefault(p => p.RefNumber == authority);
            if (pay == null) return HttpNotFound();
            var order = pay.Order;

            if (pay.IsSuccess) return Done(order);     // بار دوم باز شدن همین آدرس؛ کاری لازم نیست

            if (!string.Equals(status, "OK", StringComparison.OrdinalIgnoreCase))
            {
                TempData["err"] = "پرداخت انجام نشد یا لغو شد. تا پایان مهلت می‌توانی دوباره پرداخت کنی.";
                return Done(order);
            }

            var gw = Gateways.ByName(pay.Gateway, Request);
            if (gw == null) { TempData["err"] = "درگاه پرداخت در دسترس نیست"; return Done(order); }

            var v = gw.Verify(order, authority);
            if (!v.Ok) { TempData["err"] = v.Error ?? "پرداخت تأیید نشد"; return Done(order); }

            pay.IsSuccess = true;
            pay.RefNumber = string.IsNullOrEmpty(v.RefId) ? authority : (v.RefId.Length > 100 ? v.RefId.Substring(0, 100) : v.RefId);

            if (order.Status == OrderService.Canceled)
            {
                // سفارش به خاطر تمام شدن مهلت لغو شده بود ولی پول کسر شده؛ اگر موجودی هست دوباره بازش می‌کنیم
                if (!OrderService.Reopen(db, order))
                {
                    db.SaveChanges();
                    TempData["err"] = "پرداخت انجام شد اما موجودی کالا تمام شده بود. مبلغ بازگردانده می‌شود؛ با پشتیبانی تماس بگیر. کد پیگیری: " + Fa.Digits(pay.RefNumber);
                    return Done(order);
                }
            }
            else if (order.Status == OrderService.Pending)
            {
                order.Status = OrderService.Paid;
            }
            db.SaveChanges();
            TempData["ok"] = "پرداخت با موفقیت انجام شد. کد پیگیری: " + Fa.Digits(pay.RefNumber);
            return Done(order);
        }

        // ---------- بانک آزمایشی (فقط localhost) ----------
        public ActionResult Demo(string authority)
        {
            if (!Request.IsLocal) return HttpNotFound();
            var pay = db.Payments.FirstOrDefault(p => p.RefNumber == authority && p.Gateway == "Demo");
            if (pay == null) return HttpNotFound();
            return View(pay);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult DemoPay(string authority, bool ok)
        {
            if (!Request.IsLocal) return HttpNotFound();
            return RedirectToAction("Callback", new { authority, status = ok ? "OK" : "NOK" });
        }

        protected override void Dispose(bool disposing) { if (disposing) db.Dispose(); base.Dispose(disposing); }
    }
}
