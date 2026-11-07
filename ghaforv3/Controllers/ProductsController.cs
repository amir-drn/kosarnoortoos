using System;
using System.Data.Entity;
using System.Linq;
using System.Web.Mvc;
using Ghaforv3.Models;

namespace Ghaforv3.Controllers
{
    public class ProductsController : Controller
    {
        private readonly Ghaforv3Db db = new Ghaforv3Db();
        private const int PageSize = 8;

        // آدرس‌ها: /Products?category=led&q=لامپ&sort=cheap&page=2
        public ActionResult Index(string category, string q, string sort, string brand, decimal? min, decimal? max, bool instock = false, int page = 1)
        {
            var query = db.Products.Include(p => p.Category).Where(p => p.IsActive);

            string categoryTitle = null;
            if (!string.IsNullOrWhiteSpace(category))
            {
                var cat = db.Categories.FirstOrDefault(c => c.Slug == category);
                if (cat != null)
                {
                    query = query.Where(p => p.CategoryId == cat.Id);
                    categoryTitle = cat.Title;
                }
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                if (q.Length > 100) q = q.Substring(0, 100);
                query = query.Where(p => p.Title.Contains(q));
            }

            if (!string.IsNullOrWhiteSpace(brand))
            {
                var b = db.Brands.FirstOrDefault(x => x.Slug == brand);
                if (b != null) { var bid = b.Id; query = query.Where(p => p.BrandId == bid); } else { brand = null; }
            }
            if (min.HasValue && min.Value > 0) { var lo = min.Value; query = query.Where(p => p.Price >= lo); }
            if (max.HasValue && max.Value > 0) { var hi = max.Value; query = query.Where(p => p.Price <= hi); }
            if (instock) query = query.Where(p => p.Stock > 0);

            // مرتب‌سازی باید قبل از Skip/Take انجام شود
            switch (sort)
            {
                case "cheap":     query = query.OrderBy(p => p.Price); break;
                case "expensive": query = query.OrderByDescending(p => p.Price); break;
                default:          query = query.OrderByDescending(p => p.CreatedAt).ThenBy(p => p.Id); break;
            }

            int total = query.Count();
            int totalPages = (int)Math.Ceiling(total / (double)PageSize);
            // اگر page خیلی بزرگ باشد، Skip سرریز می‌کند و خطای ۵۰۰ می‌دهد؛ پس همیشه محدودش می‌کنیم
            if (page < 1) page = 1;
            if (page > Math.Max(1, totalPages)) page = Math.Max(1, totalPages);

            var vm = new ProductListViewModel
            {
                Products = query.Skip((page - 1) * PageSize).Take(PageSize).ToList(),
                Categories = db.Categories.Where(c => c.IsActive && c.ParentId == null)
                    .OrderBy(c => c.SortOrder)
                    .Select(c => new CategoryCard { Title = c.Title, Slug = c.Slug, Icon = c.Icon }).ToList(),
                Category = category, CategoryTitle = categoryTitle,
                Brands = db.Brands.OrderBy(x => x.Title).ToList(), Brand = brand,
                MinPrice = min, MaxPrice = max, InStock = instock,
                Q = q, Sort = sort, Page = page, TotalPages = totalPages, TotalCount = total
            };
            return View(vm);
        }

        // پیشنهاد زندهٔ جستجو: /products/suggest?q=لامپ
        public ActionResult Suggest(string q)
        {
            q = (q ?? "").Trim();
            if (q.Length < 2 || q.Length > 60) return Json(new { items = new object[0] }, JsonRequestBehavior.AllowGet);
            var rows = db.Products.Where(p => p.IsActive && p.Title.Contains(q)).OrderBy(p => p.Title).Take(6)
                .Select(p => new { p.Title, p.Slug, p.Price, p.ImageUrl }).ToList();
            var items = rows.Select(p => new
            {
                title = p.Title,
                url = Url.Action("Details", "Products", new { slug = p.Slug }),
                price = Ghaforv3.Helpers.Fa.Money(p.Price),
                img = string.IsNullOrEmpty(p.ImageUrl) ? null : Url.Content(p.ImageUrl)
            }).ToList();
            return Json(new { items }, JsonRequestBehavior.AllowGet);
        }

        // آدرس: /product/led-10w-e27
        public ActionResult Details(string slug)
        {
            var p = db.Products.Include(x => x.Category).Include(x => x.Brand).Include(x => x.ProductImages)
                      .FirstOrDefault(x => x.Slug == slug && x.IsActive);
            if (p == null) return HttpNotFound();

            var vm = new ProductDetailsViewModel
            {
                Product = p,
                Related = db.Products.Include(x => x.Category)
                            .Where(x => x.IsActive && x.CategoryId == p.CategoryId && x.Id != p.Id)
                            .OrderBy(x => x.Id).Take(4).ToList()
            };
            return View(vm);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
