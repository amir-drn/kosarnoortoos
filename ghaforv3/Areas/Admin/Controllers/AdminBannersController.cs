using System.Linq;
using System.Web;
using System.Web.Mvc;
using Ghaforv3.Helpers;
using Ghaforv3.Models;

namespace Ghaforv3.Areas.Admin.Controllers
{
    // بنر بزرگ صفحهٔ اصلی. اولین بنر «فعال» بر اساس ترتیب نمایش داده می‌شود.
    public class AdminBannersController : BaseAdminController
    {
        public ActionResult Index() { return View(db.Banners.OrderBy(b => b.SortOrder).ThenBy(b => b.Id).ToList()); }

        public ActionResult Create() { return View("Edit", new BannerFormVm { IsActive = true, ButtonText = "مشاهده محصولات" }); }

        public ActionResult Edit(int id)
        {
            var b = db.Banners.Find(id);
            if (b == null) return HttpNotFound();
            return View(new BannerFormVm { Id = b.Id, Tag = b.Tag, Title = b.Title, Highlight = b.Highlight, ButtonText = b.ButtonText,
                LinkUrl = b.LinkUrl, SortOrder = b.SortOrder, IsActive = b.IsActive, ImageUrl = b.ImageUrl });
        }

        // لینک فقط می‌تواند آدرس داخلی سایت یا http(s) باشد؛ جلوی javascript: و مشابه آن گرفته می‌شود
        private static bool SafeLink(string l)
        {
            if (string.IsNullOrEmpty(l)) return true;
            if (l.StartsWith("/") && !l.StartsWith("//") && !l.Contains("\\")) return true;
            return l.StartsWith("https://") || l.StartsWith("http://");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Save(BannerFormVm m, HttpPostedFileBase image)
        {
            m.LinkUrl = (m.LinkUrl ?? "").Trim();
            if (!SafeLink(m.LinkUrl)) ModelState.AddModelError("LinkUrl", "لینک باید با / یا https:// شروع شود");

            string newUrl = null;
            if (ModelState.IsValid && image != null && image.ContentLength > 0)
            {
                string err;
                newUrl = ImageUpload.Save(Server, image, "banner", out err);
                if (err != null) ModelState.AddModelError("", err);
            }
            if (!ModelState.IsValid) { m.ImageUrl = m.Id == 0 ? null : db.Banners.Where(x => x.Id == m.Id).Select(x => x.ImageUrl).FirstOrDefault(); return View("Edit", m); }

            Banner b;
            if (m.Id == 0) { b = new Banner(); db.Banners.Add(b); }
            else { b = db.Banners.Find(m.Id); if (b == null) return HttpNotFound(); }

            b.Tag = string.IsNullOrWhiteSpace(m.Tag) ? null : m.Tag.Trim();
            b.Title = m.Title.Trim();
            b.Highlight = string.IsNullOrWhiteSpace(m.Highlight) ? null : m.Highlight.Trim();
            b.ButtonText = string.IsNullOrWhiteSpace(m.ButtonText) ? "مشاهده محصولات" : m.ButtonText.Trim();
            b.LinkUrl = m.LinkUrl.Length == 0 ? null : m.LinkUrl;
            b.SortOrder = m.SortOrder; b.IsActive = m.IsActive;
            if (newUrl != null) { ImageUpload.Delete(Server, b.ImageUrl); b.ImageUrl = newUrl; }
            db.SaveChanges();
            TempData["ok"] = "بنر ذخیره شد";
            return RedirectToAction("Index");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var b = db.Banners.Find(id);
            if (b == null) return HttpNotFound();
            db.Banners.Remove(b);
            db.SaveChanges();
            ImageUpload.Delete(Server, b.ImageUrl);
            TempData["ok"] = "بنر حذف شد";
            return RedirectToAction("Index");
        }
    }
}
