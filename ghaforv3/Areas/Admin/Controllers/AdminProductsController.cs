using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Ghaforv3.Helpers;
using Ghaforv3.Models;

namespace Ghaforv3.Areas.Admin.Controllers
{
    public class AdminProductsController : BaseAdminController
    {
        private const int PageSize = 10;

        public ActionResult Index(string q, int page = 1)
        {
            IQueryable<Product> query = db.Products;
            if (!string.IsNullOrWhiteSpace(q)) query = query.Where(p => p.Title.Contains(q) || p.Slug.Contains(q));
            int total = query.Count();
            int pages = (int)Math.Ceiling(total / (double)PageSize);
            page = ClampPage(page, pages);
            var rows = query.OrderByDescending(p => p.Id).Skip((page - 1) * PageSize).Take(PageSize)
                .Select(p => new ProductRow { Id = p.Id, Title = p.Title, Slug = p.Slug, CategoryTitle = p.Category.Title,
                    Price = p.Price, Stock = p.Stock, IsActive = p.IsActive, ImageUrl = p.ImageUrl }).ToList();
            return View(new PagedVm<ProductRow> { Items = rows, Page = page, Pages = pages, Total = total, Q = q });
        }

        public ActionResult Create()
        {
            return View("Edit", Fill(new ProductFormVm { IsActive = true, Unit = "هر عدد" }));
        }

        public ActionResult Edit(int id)
        {
            var p = db.Products.Find(id);
            if (p == null) return HttpNotFound();
            return View(Fill(new ProductFormVm
            {
                Id = p.Id, Title = p.Title, Slug = p.Slug, CategoryId = p.CategoryId, BrandId = p.BrandId,
                Price = p.Price, OldPrice = p.OldPrice, Unit = p.Unit, Stock = p.Stock, Description = p.Description,
                IsActive = p.IsActive, IsDailyOffer = p.IsDailyOffer, IsFlashSale = p.IsFlashSale,
                OfferEndsAt = p.OfferEndsAt, ImageUrl = p.ImageUrl,
                Code = p.Code, CartonQty = p.CartonQty, Warranty = p.Warranty, BodyColors = p.BodyColors, LightColors = p.LightColors
            }));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Save(ProductFormVm m, HttpPostedFileBase image)
        {
            m.Slug = (m.Slug ?? "").Trim().ToLowerInvariant();
            if (db.Products.Any(p => p.Slug == m.Slug && p.Id != m.Id)) ModelState.AddModelError("Slug", "این Slug قبلاً استفاده شده");
            if (m.OldPrice.HasValue && m.Price.HasValue && m.OldPrice < m.Price) ModelState.AddModelError("OldPrice", "قیمت قبل از تخفیف باید بیشتر از قیمت فعلی باشد");

            m.Code = string.IsNullOrWhiteSpace(m.Code) ? null : m.Code.Trim();
            if (m.Code != null && db.Products.Any(p => p.Code == m.Code && p.Id != m.Id)) ModelState.AddModelError("Code", "این کد محصول برای کالای دیگری ثبت شده");
            var bodyErr = ProductOptions.Validate(m.BodyColors); if (bodyErr != null) ModelState.AddModelError("BodyColors", bodyErr);
            var lightErr = ProductOptions.Validate(m.LightColors); if (lightErr != null) ModelState.AddModelError("LightColors", lightErr);

            string newUrl = null;
            if (ModelState.IsValid && image != null && image.ContentLength > 0)
            {
                string err;
                newUrl = ImageUpload.Save(Server, image, m.Slug, out err);
                if (err != null) ModelState.AddModelError("", err);
            }
            if (!ModelState.IsValid) return View("Edit", Fill(m));

            Product product;
            if (m.Id == 0) { product = new Product { CreatedAt = DateTime.Now }; db.Products.Add(product); }
            else { product = db.Products.Find(m.Id); if (product == null) return HttpNotFound(); }

            product.Title = m.Title.Trim(); product.Slug = m.Slug; product.CategoryId = m.CategoryId.Value; product.BrandId = m.BrandId;
            product.Price = m.Price.Value; product.OldPrice = m.OldPrice; product.Stock = m.Stock;
            product.Unit = string.IsNullOrWhiteSpace(m.Unit) ? "هر عدد" : m.Unit.Trim();
            product.Description = m.Description; product.IsActive = m.IsActive;
            product.Code = m.Code; product.CartonQty = m.CartonQty;
            product.Warranty = string.IsNullOrWhiteSpace(m.Warranty) ? null : m.Warranty.Trim();
            product.BodyColors = ProductOptions.Normalize(m.BodyColors); product.LightColors = ProductOptions.Normalize(m.LightColors);
            product.IsDailyOffer = m.IsDailyOffer; product.IsFlashSale = m.IsFlashSale; product.OfferEndsAt = m.OfferEndsAt;
            if (newUrl != null) { ImageUpload.Delete(Server, product.ImageUrl); product.ImageUrl = newUrl; }

            db.SaveChanges();
            System.Web.HttpRuntime.Cache.Remove(Ghaforv3.Controllers.HomeController.CatsCacheKey);   // شمارندهٔ محصولات دسته‌ها تازه شود
            TempData["ok"] = "محصول ذخیره شد";
            return RedirectToAction("Index");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var p = db.Products.Find(id);
            if (p == null) return HttpNotFound();
            try
            {
                db.Products.Remove(p);
                db.SaveChanges();
                ImageUpload.Delete(Server, p.ImageUrl);
                System.Web.HttpRuntime.Cache.Remove(Ghaforv3.Controllers.HomeController.CatsCacheKey);
                TempData["ok"] = "محصول حذف شد";
            }
            catch (DbUpdateException)
            {
                TempData["err"] = "این محصول در سفارش‌ها استفاده شده و حذف نمی‌شود؛ به‌جای حذف، غیرفعالش کن.";
            }
            return RedirectToAction("Index");
        }

        // ---------- ورود و خروج گروهی با اکسل ----------
        private const int MaxImportBytes = 4 * 1024 * 1024;

        public ActionResult Import() { return View(); }

        // قالب خالی با عنوان ستون‌ها
        public ActionResult Template()
        {
            var rows = new List<object[]> { ProductImporter.Headers.Cast<object>().ToArray() };
            return File(XlsxWriter.Write("محصولات", rows, ProductImporter.Widths), XlsxWriter.Mime, "products-template.xlsx");
        }

        // همهٔ محصولات با همین ستون‌ها؛ می‌توانی قیمت‌ها را ویرایش و دوباره وارد کنی
        public ActionResult ExportXlsx()
        {
            var bytes = XlsxWriter.Write("محصولات", ProductImporter.ExportRows(db), ProductImporter.Widths);
            return File(bytes, XlsxWriter.Mime, "products-" + DateTime.Now.ToString("yyyyMMdd-HHmm") + ".xlsx");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult ImportUpload(HttpPostedFileBase file)
        {
            if (file == null || file.ContentLength == 0) { TempData["err"] = "فایلی انتخاب نشده است"; return RedirectToAction("Import"); }
            if (file.ContentLength > MaxImportBytes) { TempData["err"] = "حجم فایل بیشتر از ۴ مگابایت است"; return RedirectToAction("Import"); }

            var ext = (System.IO.Path.GetExtension(file.FileName) ?? "").ToLowerInvariant();
            List<string[]> table;
            try
            {
                if (ext == ".xlsx") table = XlsxReader.ReadFirstSheet(file.InputStream, ProductImporter.MaxRows);
                else if (ext == ".csv") table = CsvReader.Read(file.InputStream, ProductImporter.MaxRows);
                else { TempData["err"] = "فقط فایل .xlsx یا .csv پذیرفته می‌شود (فرمت قدیمی .xls را با «Save As ▸ Excel Workbook» تبدیل کن)"; return RedirectToAction("Import"); }
            }
            catch (Exception)
            {
                TempData["err"] = "فایل خوانده نشد. مطمئن شو یک فایل xlsx سالم است.";
                return RedirectToAction("Import");
            }

            Session["import-table"] = table;
            var plan = ProductImporter.Build(db, table, System.IO.Path.GetFileName(file.FileName));
            return View("ImportPreview", plan);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult ImportConfirm()
        {
            var table = Session["import-table"] as List<string[]>;
            if (table == null) { TempData["err"] = "مهلت پیش‌نمایش تمام شده؛ فایل را دوباره بارگذاری کن"; return RedirectToAction("Import"); }
            Session.Remove("import-table");

            var plan = ProductImporter.Build(db, table, "");      // دوباره با وضعیت فعلی دیتابیس محاسبه می‌شود
            if (plan.Fatal != null) { TempData["err"] = plan.Fatal; return RedirectToAction("Import"); }
            try { ProductImporter.Apply(db, plan); }
            catch (Exception)
            {
                TempData["err"] = "ثبت انجام نشد و هیچ تغییری اعمال نشد. دوباره تلاش کن.";
                return RedirectToAction("Import");
            }
            System.Web.HttpRuntime.Cache.Remove(Ghaforv3.Controllers.HomeController.CatsCacheKey);
            TempData["ok"] = plan.Created + " کالای جدید ساخته و " + plan.Updated + " کالا به‌روزرسانی شد"
                + (plan.ErrorCount > 0 ? "؛ " + plan.ErrorCount + " ردیف خطا داشت و وارد نشد" : "");
            return RedirectToAction("Index");
        }

        // ---------- گالری عکس‌های اضافهٔ محصول ----------
        public ActionResult Images(int id)
        {
            var p = db.Products.Find(id);
            if (p == null) return HttpNotFound();
            return View(new ProductImagesVm { ProductId = p.Id, Title = p.Title, MainImageUrl = p.ImageUrl,
                Images = db.ProductImages.Where(i => i.ProductId == id).OrderBy(i => i.SortOrder).ThenBy(i => i.Id).ToList() });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult AddImages(int id, HttpPostedFileBase[] files)
        {
            var p = db.Products.Find(id);
            if (p == null) return HttpNotFound();
            int added = 0, order = (db.ProductImages.Where(i => i.ProductId == id).Max(i => (int?)i.SortOrder) ?? 0) + 1;
            int existing = db.ProductImages.Count(i => i.ProductId == id);
            string firstError = null;
            foreach (var f in (files ?? new HttpPostedFileBase[0]).Where(f => f != null && f.ContentLength > 0))
            {
                if (existing + added >= 12) { firstError = "حداکثر ۱۲ عکس برای هر محصول"; break; }
                string err;
                var url = ImageUpload.Save(Server, f, p.Slug + "-g", out err);
                if (err != null) { if (firstError == null) firstError = err; continue; }
                db.ProductImages.Add(new ProductImage { ProductId = id, ImageUrl = url, SortOrder = order++ });
                added++;
            }
            if (added > 0) db.SaveChanges();
            if (added > 0) TempData["ok"] = added + " عکس اضافه شد";
            if (firstError != null) TempData["err"] = firstError;
            return RedirectToAction("Images", new { id });
        }

        // حذف عکس اصلی محصول (کالا بدون عکس هم نمایش داده می‌شود)
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult DeleteMainImage(int id, string returnTo)
        {
            var p = db.Products.Find(id);
            if (p == null) return HttpNotFound();
            var old = p.ImageUrl;
            p.ImageUrl = null;
            db.SaveChanges();
            ImageUpload.Delete(Server, old);
            TempData["ok"] = "عکس اصلی محصول حذف شد";
            return returnTo == "images" ? RedirectToAction("Images", new { id }) : RedirectToAction("Edit", new { id });
        }

        // حذف چند عکس انتخاب‌شده از گالری
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult BulkDeleteImages(int id, int[] ids)
        {
            if (db.Products.Find(id) == null) return HttpNotFound();
            if (ids == null || ids.Length == 0) { TempData["err"] = "هیچ عکسی انتخاب نشده است"; return RedirectToAction("Images", new { id }); }
            var imgs = db.ProductImages.Where(i => i.ProductId == id && ids.Contains(i.Id)).ToList();   // فقط عکس‌های همین محصول
            foreach (var i in imgs) db.ProductImages.Remove(i);
            db.SaveChanges();
            foreach (var i in imgs) ImageUpload.Delete(Server, i.ImageUrl);
            TempData["ok"] = imgs.Count + " عکس حذف شد";
            return RedirectToAction("Images", new { id });
        }

        // حذف همهٔ عکس‌های گالری
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult DeleteAllImages(int id)
        {
            if (db.Products.Find(id) == null) return HttpNotFound();
            var imgs = db.ProductImages.Where(i => i.ProductId == id).ToList();
            foreach (var i in imgs) db.ProductImages.Remove(i);
            db.SaveChanges();
            foreach (var i in imgs) ImageUpload.Delete(Server, i.ImageUrl);
            TempData["ok"] = "همهٔ عکس‌های گالری حذف شد";
            return RedirectToAction("Images", new { id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult DeleteImage(int id, int imageId)
        {
            var img = db.ProductImages.FirstOrDefault(i => i.Id == imageId && i.ProductId == id);   // فقط عکسِ همین محصول
            if (img == null) return HttpNotFound();
            db.ProductImages.Remove(img);
            db.SaveChanges();
            ImageUpload.Delete(Server, img.ImageUrl);
            TempData["ok"] = "عکس حذف شد";
            return RedirectToAction("Images", new { id });
        }

        private ProductFormVm Fill(ProductFormVm m)
        {
            m.Categories = new SelectList(db.Categories.OrderBy(c => c.SortOrder).ToList(), "Id", "Title", m.CategoryId);
            m.Brands = new SelectList(db.Brands.OrderBy(b => b.Title).ToList(), "Id", "Title", m.BrandId);
            if (m.Id > 0 && m.ImageUrl == null) m.ImageUrl = db.Products.Where(p => p.Id == m.Id).Select(p => p.ImageUrl).FirstOrDefault();
            return m;
        }
    }
}
