using System;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using System.Xml.Linq;
using Ghaforv3.Helpers;
using Ghaforv3.Models;

namespace Ghaforv3.Controllers
{
    // robots.txt و sitemap.xml برای موتورهای جست‌وجو
    public class SeoController : Controller
    {
        private readonly Ghaforv3Db db = new Ghaforv3Db();

        public ActionResult Robots()
        {
            var b = Site.BaseUrl(Request);
            var sb = new StringBuilder();
            sb.AppendLine("User-agent: *");
            foreach (var p in new[] { "/admin", "/cart", "/checkout", "/account", "/orders", "/wishlist", "/addresses", "/payment", "/login", "/register" })
                sb.AppendLine("Disallow: " + p);
            sb.AppendLine("Sitemap: " + b + "/sitemap.xml");
            return Content(sb.ToString(), "text/plain", Encoding.UTF8);
        }

        public ActionResult Sitemap()
        {
            var b = Site.BaseUrl(Request);
            XNamespace ns = "http://www.sitemaps.org/schemas/sitemap/0.9";
            var urls = new System.Collections.Generic.List<XElement>();
            Action<string, string, string> add = (path, freq, prio) =>
                urls.Add(new XElement(ns + "url", new XElement(ns + "loc", b + path), new XElement(ns + "changefreq", freq), new XElement(ns + "priority", prio)));

            add("/", "daily", "1.0");
            add(Url.Action("Index", "Products"), "daily", "0.9");
            foreach (var p in new[] { "About", "Contact", "Shipping", "Terms", "Privacy" }) add(Url.Action(p, "Pages"), "monthly", "0.3");
            foreach (var c in db.Categories.Where(x => x.IsActive).Select(x => x.Slug).ToList())
                add(Url.Action("Index", "Products", new { category = c }), "weekly", "0.7");
            foreach (var s in db.Products.Where(x => x.IsActive).OrderByDescending(x => x.Id).Select(x => x.Slug).Take(5000).ToList())
                add(Url.Action("Details", "Products", new { slug = s }), "weekly", "0.8");

            var doc = new XDocument(new XDeclaration("1.0", "utf-8", null), new XElement(ns + "urlset", urls));
            return Content(doc.Declaration + Environment.NewLine + doc.Root, "application/xml", Encoding.UTF8);
        }

        protected override void Dispose(bool disposing) { if (disposing) db.Dispose(); base.Dispose(disposing); }
    }
}
