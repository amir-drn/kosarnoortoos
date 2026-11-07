using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using Ghaforv3.Helpers;
using Ghaforv3.Models;

namespace Ghaforv3.Controllers
{
    public class OrdersController : ShopController
    {
        public ActionResult Index()
        {
            OrderService.ReleaseExpired(db);
            var me = Me;
            var list = db.Orders.Include(o => o.OrderItems).Where(o => o.UserId == me.Id)
                .OrderByDescending(o => o.CreatedAt).Take(100).ToList();
            return View(list);
        }

        private Order Load(int id)
        {
            var me = Me;
            // شرط UserId جلوی دیدن سفارش دیگران را می‌گیرد
            return db.Orders.Include(o => o.OrderItems).Include(o => o.Address).Include(o => o.Payments)
                .FirstOrDefault(o => o.Id == id && o.UserId == me.Id);
        }

        public ActionResult Details(int id)
        {
            OrderService.ReleaseExpired(db);
            var o = Load(id);
            if (o == null) return HttpNotFound();
            return View(o);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Pay(int id)
        {
            var o = Load(id);
            if (o == null) return HttpNotFound();
            if (o.Status != OrderService.Pending) return RedirectToAction("Details", new { id });
            if (o.CreatedAt < DateTime.Now.AddMinutes(-Site.ReserveMinutes))
            {
                OrderService.Cancel(db, o);
                TempData["err"] = "مهلت پرداخت این سفارش تمام شده و موجودی آزاد شد. سفارش را دوباره ثبت کن.";
                return RedirectToAction("Details", new { id });
            }
            return StartPayment(o);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Cancel(int id)
        {
            var o = Load(id);
            if (o == null) return HttpNotFound();
            if (o.Status == OrderService.Pending) { OrderService.Cancel(db, o); TempData["ok"] = "سفارش لغو شد"; }
            return RedirectToAction("Details", new { id });
        }
    }
}
