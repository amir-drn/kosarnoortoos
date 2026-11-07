using System.Linq;
using System.Web;
using System.Web.Mvc;
using Ghaforv3.Helpers;
using Ghaforv3.Models;

namespace Ghaforv3.Areas.Admin.Controllers
{
    public class AdminCategoriesController : BaseAdminController
    {
        public ActionResult Index()
        {
            var rows = db.Categories.OrderBy(c => c.SortOrder)
                .Select(c => new CategoryRow { Id = c.Id, Title = c.Title, Slug = c.Slug, Icon = c.Icon, IsActive = c.IsActive,
                    Count = db.Products.Count(p => p.CategoryId == c.Id) }).ToList();
            return View(rows);
        }

        public ActionResult Create() { return View("Edit", Fill(new CategoryFormVm { IsActive = true })); }

        public ActionResult Edit(int id)
        {
            var c = db.Categories.Find(id);
            if (c == null) return HttpNotFound();
            return View(Fill(new CategoryFormVm { Id = c.Id, Title = c.Title, Slug = c.Slug, Icon = c.Icon,
                SortOrder = c.SortOrder, IsActive = c.IsActive, ParentId = c.ParentId }));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Save(CategoryFormVm m, HttpPostedFileBase image)
        {
            m.Slug = (m.Slug ?? "").Trim().ToLowerInvariant();
            if (db.Categories.Any(c => c.Slug == m.Slug && c.Id != m.Id)) ModelState.AddModelError("Slug", "این Slug قبلاً استفاده شده");
            if (m.Id != 0 && m.ParentId == m.Id) ModelState.AddModelError("ParentId", "دسته نمی‌تواند والد خودش باشد");

            // آیکن یا ایموجی کوتاه است یا آدرس امنِ عکس؛ هر چیز دیگری (مثل ../) رد می‌شود
            m.Icon = (m.Icon ?? "").Trim();
            if (m.Icon.Length > 0 && (m.Icon.StartsWith("~/") || m.Icon.StartsWith("/") || m.Icon.Contains("..") || m.Icon.Contains("\\") || m.Icon.Contains(":")))
            {
                if (!ImageUpload.IsSafeUrl(m.Icon)) ModelState.AddModelError("Icon", "آدرس آیکن معتبر نیست");
            }
            else if (m.Icon.Length > 16) ModelState.AddModelError("Icon", "آیکن باید یک ایموجی کوتاه یا آدرس عکس باشد");

            string newUrl = null;
            if (ModelState.IsValid && image != null && image.ContentLength > 0)
            {
                string err;
                newUrl = ImageUpload.Save(Server, image, "cat-" + m.Slug, out err);
                if (err != null) ModelState.AddModelError("", err);
            }
            if (!ModelState.IsValid) return View("Edit", Fill(m));

            Category category;
            if (m.Id == 0) { category = new Category(); db.Categories.Add(category); }
            else { category = db.Categories.Find(m.Id); if (category == null) return HttpNotFound(); }

            category.Title = m.Title.Trim(); category.Slug = m.Slug; category.SortOrder = m.SortOrder; category.IsActive = m.IsActive; category.ParentId = m.ParentId;
            if (newUrl != null) { ImageUpload.Delete(Server, category.Icon); category.Icon = newUrl; }
            else category.Icon = string.IsNullOrEmpty(m.Icon) ? null : m.Icon;

            db.SaveChanges();
            System.Web.HttpRuntime.Cache.Remove(Ghaforv3.Controllers.HomeController.CatsCacheKey);
            TempData["ok"] = "دسته ذخیره شد";
            return RedirectToAction("Index");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var c = db.Categories.Find(id);
            if (c == null) return HttpNotFound();
            if (db.Products.Any(p => p.CategoryId == id)) { TempData["err"] = "این دسته محصول دارد و حذف نمی‌شود."; return RedirectToAction("Index"); }
            if (db.Categories.Any(x => x.ParentId == id)) { TempData["err"] = "این دسته زیردسته دارد و حذف نمی‌شود."; return RedirectToAction("Index"); }
            db.Categories.Remove(c);
            db.SaveChanges();
            ImageUpload.Delete(Server, c.Icon);
            System.Web.HttpRuntime.Cache.Remove(Ghaforv3.Controllers.HomeController.CatsCacheKey);
            TempData["ok"] = "دسته حذف شد";
            return RedirectToAction("Index");
        }

        private CategoryFormVm Fill(CategoryFormVm m)
        {
            m.Parents = new SelectList(db.Categories.Where(c => c.ParentId == null && c.Id != m.Id).OrderBy(c => c.SortOrder).ToList(), "Id", "Title", m.ParentId);
            return m;
        }
    }
}
