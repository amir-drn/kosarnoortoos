using System.Linq;
using System.Web.Mvc;
using Ghaforv3.Models;

namespace Ghaforv3.Areas.Admin.Controllers
{
    public class AdminBrandsController : BaseAdminController
    {
        public ActionResult Index()
        {
            var rows = db.Brands.OrderBy(b => b.Title).ToList()
                .Select(b => new { b.Id, b.Title, b.Slug, Count = db.Products.Count(p => p.BrandId == b.Id) }).ToList();
            ViewBag.Counts = rows.ToDictionary(r => r.Id, r => r.Count);
            return View(db.Brands.OrderBy(b => b.Title).ToList());
        }

        public ActionResult Create() { return View("Edit", new BrandFormVm()); }

        public ActionResult Edit(int id)
        {
            var b = db.Brands.Find(id);
            if (b == null) return HttpNotFound();
            return View(new BrandFormVm { Id = b.Id, Title = b.Title, Slug = b.Slug });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Save(BrandFormVm m)
        {
            m.Slug = (m.Slug ?? "").Trim().ToLowerInvariant();
            if (db.Brands.Any(x => x.Slug == m.Slug && x.Id != m.Id)) ModelState.AddModelError("Slug", "این Slug قبلاً استفاده شده");
            if (!ModelState.IsValid) return View("Edit", m);

            Brand b;
            if (m.Id == 0) { b = new Brand(); db.Brands.Add(b); }
            else { b = db.Brands.Find(m.Id); if (b == null) return HttpNotFound(); }
            b.Title = m.Title.Trim(); b.Slug = m.Slug;
            db.SaveChanges();
            TempData["ok"] = "برند ذخیره شد";
            return RedirectToAction("Index");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var b = db.Brands.Find(id);
            if (b == null) return HttpNotFound();
            if (db.Products.Any(p => p.BrandId == id)) { TempData["err"] = "این برند محصول دارد و حذف نمی‌شود."; return RedirectToAction("Index"); }
            db.Brands.Remove(b);
            db.SaveChanges();
            TempData["ok"] = "برند حذف شد";
            return RedirectToAction("Index");
        }
    }
}
