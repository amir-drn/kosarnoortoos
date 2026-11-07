using System.Linq;
using System.Web.Mvc;
using Ghaforv3.Models;
using Ghaforv3.Security;

namespace Ghaforv3.Controllers
{
    public class AddressesController : ShopController
    {
        public ActionResult Index()
        {
            var me = Me;
            return View(db.Addresses.Where(a => a.UserId == me.Id).OrderByDescending(a => a.Id).ToList());
        }

        public ActionResult Edit(int id = 0)
        {
            if (id == 0) return View(new AddressForm { Receiver = Me.FullName, Phone = Me.Mobile });
            var me = Me;
            var a = db.Addresses.FirstOrDefault(x => x.Id == id && x.UserId == me.Id);
            if (a == null) return HttpNotFound();
            return View(new AddressForm { Id = a.Id, Receiver = a.Receiver, Phone = a.Phone, Province = a.Province, City = a.City, PostalCode = a.PostalCode, Details = a.Details });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Save(AddressForm m)
        {
            var me = Me;
            m.Phone = Digits.ToEnglish(m.Phone ?? "");
            m.PostalCode = Digits.ToEnglish(m.PostalCode ?? "");
            if (!ModelState.IsValid) return View("Edit", m);

            Address a;
            if (m.Id == 0)
            {
                if (db.Addresses.Count(x => x.UserId == me.Id) >= 10) { ModelState.AddModelError("", "حداکثر ۱۰ آدرس می‌توانی ذخیره کنی"); return View("Edit", m); }
                a = new Address { UserId = me.Id };
                db.Addresses.Add(a);
            }
            else
            {
                a = db.Addresses.FirstOrDefault(x => x.Id == m.Id && x.UserId == me.Id);   // فقط آدرس خودش
                if (a == null) return HttpNotFound();
            }
            a.Receiver = m.Receiver.Trim(); a.Phone = m.Phone; a.Province = m.Province.Trim(); a.City = m.City.Trim();
            a.PostalCode = string.IsNullOrWhiteSpace(m.PostalCode) ? null : m.PostalCode; a.Details = m.Details.Trim();
            db.SaveChanges();
            TempData["ok"] = "آدرس ذخیره شد";
            return RedirectToAction("Index");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var me = Me;
            var a = db.Addresses.FirstOrDefault(x => x.Id == id && x.UserId == me.Id);
            if (a == null) return HttpNotFound();
            if (db.Orders.Any(o => o.AddressId == id)) TempData["err"] = "این آدرس در سفارش‌های قبلی استفاده شده و حذف نمی‌شود.";
            else { db.Addresses.Remove(a); db.SaveChanges(); TempData["ok"] = "آدرس حذف شد"; }
            return RedirectToAction("Index");
        }
    }
}
