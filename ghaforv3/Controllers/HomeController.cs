using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Caching;
using System.Web.Mvc;
using Ghaforv3.Models;

namespace Ghaforv3.Controllers
{
    public class HomeController : Controller
    {
        private readonly Ghaforv3Db db = new Ghaforv3Db();

        public ActionResult Index()
        {
            var vm = new HomeViewModel
            {
                Banner = db.Banners.Where(b => b.IsActive).OrderBy(b => b.SortOrder).FirstOrDefault(),

                Categories = db.Categories
                    .Where(c => c.IsActive && c.ParentId == null)
                    .OrderBy(c => c.SortOrder)
                    .Select(c => new CategoryCard
                    {
                        Title = c.Title, Slug = c.Slug, Icon = c.Icon,
                        Count = c.Products.Count(p => p.IsActive)
                    }).ToList(),

                ProductOfDay = db.Products.Include(p => p.Category)
                    .Where(p => p.IsActive && p.IsDailyOffer).FirstOrDefault(),

                FlashSale = db.Products.Include(p => p.Category)
                    .Where(p => p.IsActive && p.IsFlashSale).OrderBy(p => p.Id).Take(8).ToList()
            };
            return View(vm);
        }

        public const string CatsCacheKey = "menu-cats";

        // لیست دسته‌ها برای منوی دسکتاپ و فوتر (۵ دقیقه در حافظه می‌ماند تا هر صفحه یک کوئری اضافه نزند)
        [ChildActionOnly]
        public ActionResult Categories(string mode)
        {
            var list = HttpRuntime.Cache[CatsCacheKey] as List<CategoryCard>;
            if (list == null)
            {
                list = db.Categories
                    .Where(c => c.IsActive && c.ParentId == null)
                    .OrderBy(c => c.SortOrder)
                    .Select(c => new CategoryCard
                    {
                        Title = c.Title, Slug = c.Slug, Icon = c.Icon,
                        Count = c.Products.Count(p => p.IsActive)
                    }).ToList();
                HttpRuntime.Cache.Insert(CatsCacheKey, list, null, DateTime.UtcNow.AddMinutes(5), Cache.NoSlidingExpiration);
            }
            ViewBag.Mode = mode;
            return PartialView("_Cats", list);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();   // اتصال دیتابیس را آزاد می‌کند
            base.Dispose(disposing);
        }
    }
}
