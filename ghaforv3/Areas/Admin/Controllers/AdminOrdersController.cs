using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using Ghaforv3.Helpers;
using Ghaforv3.Models;

namespace Ghaforv3.Areas.Admin.Controllers
{
    public class AdminOrdersController : BaseAdminController
    {
        private const int PageSize = 10;

        public ActionResult Index(int? status, int page = 1)
        {
            IQueryable<Order> query = db.Orders;
            if (status.HasValue && status.Value >= 0 && status.Value <= 4) { byte s = (byte)status.Value; query = query.Where(o => o.Status == s); }
            int total = query.Count();
            int pages = (int)Math.Ceiling(total / (double)PageSize);
            page = ClampPage(page, pages);
            var rows = query.OrderByDescending(o => o.CreatedAt).Skip((page - 1) * PageSize).Take(PageSize)
                .Select(o => new OrderRow { Id = o.Id, Customer = o.User.FullName, Total = o.TotalAmount, Status = o.Status, CreatedAt = o.CreatedAt }).ToList();
            return View(new PagedVm<OrderRow> { Items = rows, Page = page, Pages = pages, Total = total, Status = status });
        }

        // فاکتور قابل چاپ / ذخیره به‌صورت PDF
        public ActionResult Invoice(int id)
        {
            var o = db.Orders.Include(x => x.User).Include(x => x.Address).Include(x => x.OrderItems).Include(x => x.Payments).FirstOrDefault(x => x.Id == id);
            if (o == null) return HttpNotFound();
            return View("~/Views/Shared/_Invoice.cshtml", o);
        }

        public ActionResult Details(int id)
        {
            var o = db.Orders.Include(x => x.User).Include(x => x.Address).Include(x => x.OrderItems).Include(x => x.Payments).FirstOrDefault(x => x.Id == id);
            if (o == null) return HttpNotFound();
            return View(new OrderDetailsVm
            {
                Id = o.Id, Customer = o.User.FullName, Mobile = o.User.Mobile,
                Receiver = o.Address.Receiver, Phone = o.Address.Phone,
                Address = o.Address.Province + "، " + o.Address.City + "، " + o.Address.Details,
                Status = o.Status, Total = o.TotalAmount, CreatedAt = o.CreatedAt,
                Items = o.OrderItems.Select(i => new OrderItemRow { Title = i.Title, UnitPrice = i.UnitPrice, Quantity = i.Quantity, BodyColor = i.BodyColor, LightColor = i.LightColor }).ToList(),
                PaymentLines = o.Payments.OrderBy(p => p.CreatedAt)
                    .Select(p => Fa.Stamp(p.CreatedAt) + " · " + p.Gateway + " · " + (p.IsSuccess ? "موفق (کد " + p.RefNumber + ")" : "ناموفق یا نیمه‌کاره")).ToList(),
                RefundNeeded = o.Status == OrderService.Canceled && o.Payments.Any(p => p.IsSuccess)
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult SetStatus(int id, int status)
        {
            var o = db.Orders.Find(id);
            if (o == null || status < 0 || status > 4) return HttpNotFound();
            var error = OrderService.SetStatus(db, o, (byte)status);     // لغو، موجودی را برمی‌گرداند
            if (error != null) { TempData["err"] = error; return RedirectToAction("Details", new { id }); }
            TempData["ok"] = "وضعیت سفارش به «" + AdminLabels.Order((byte)status) + "» تغییر کرد";
            return RedirectToAction("Details", new { id });
        }
    }
}
