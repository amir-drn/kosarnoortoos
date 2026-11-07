using System.Linq;
using System.Web.Mvc;
using Ghaforv3.Helpers;
using Ghaforv3.Models;

namespace Ghaforv3.Areas.Admin.Controllers
{
    public class AdminController : BaseAdminController
    {
        public ActionResult Index()
        {
            OrderService.ReleaseExpired(db);     // سفارش‌های پرداخت‌نشدهٔ قدیمی لغو و موجودی آزاد می‌شود
            var vm = new DashboardVm
            {
                Products = db.Products.Count(),
                Categories = db.Categories.Count(),
                Customers = db.Users.Count(u => u.Role == 0),
                Orders = db.Orders.Count(),
                NewOrders = db.Orders.Count(o => o.Status == 1),
                Revenue = db.Orders.Where(o => o.Status >= 1 && o.Status <= 3).Sum(o => (decimal?)o.TotalAmount) ?? 0,
                LowStock = db.Products.Where(p => p.IsActive && p.Stock <= 5).OrderBy(p => p.Stock).Take(5)
                    .Select(p => new ProductRow { Id = p.Id, Title = p.Title, Slug = p.Slug, Stock = p.Stock, Price = p.Price, ImageUrl = p.ImageUrl }).ToList(),
                Latest = db.Orders.OrderByDescending(o => o.CreatedAt).Take(5)
                    .Select(o => new OrderRow { Id = o.Id, Customer = o.User.FullName, Total = o.TotalAmount, Status = o.Status, CreatedAt = o.CreatedAt }).ToList()
            };
            return View(vm);
        }
    }
}
