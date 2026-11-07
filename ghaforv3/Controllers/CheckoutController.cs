using System;
using System.Linq;
using System.Web.Mvc;
using Ghaforv3.Helpers;
using Ghaforv3.Models;
using Ghaforv3.Security;

namespace Ghaforv3.Controllers
{
    public class CheckoutController : ShopController
    {
        private CheckoutVm Build(Cart cart, int addressId, AddressForm form)
        {
            var me = Me;
            var subtotal = cart.Total;
            var addresses = db.Addresses.Where(a => a.UserId == me.Id).OrderByDescending(a => a.Id).ToList();
            return new CheckoutVm
            {
                Cart = cart, Addresses = addresses,
                AddressId = addressId > 0 ? addressId : (addresses.Count > 0 ? addresses[0].Id : 0),
                NewAddress = form ?? new AddressForm { Receiver = me.FullName, Phone = me.Mobile },
                Subtotal = subtotal, Shipping = Site.Shipping(subtotal), Total = subtotal + Site.Shipping(subtotal)
            };
        }

        public ActionResult Index()
        {
            OrderService.ReleaseExpired(db);
            var cart = CartStore.Get(Session);
            CartStore.Sync(db, cart);
            if (cart.Items.Count == 0) return RedirectToAction("Index", "Cart");
            return View(Build(cart, 0, null));
        }

        [HttpPost, ValidateAntiForgeryToken]
        // پارامتر newAddress با فیلدهای NewAddress.* فرم هم‌نام است (مقایسهٔ بدون حساسیت به حروف)
        public ActionResult Place(int addressId, AddressForm newAddress)
        {
            var me = Me;
            var cart = CartStore.Get(Session);
            CartStore.Sync(db, cart);
            if (cart.Items.Count == 0) return RedirectToAction("Index", "Cart");

            if (LoginThrottle.Hit("order", me.Id.ToString(), 10, TimeSpan.FromMinutes(10)))
            {
                ModelState.AddModelError("", "تعداد سفارش‌های پشت‌سرهم زیاد بود؛ چند دقیقه بعد دوباره امتحان کن");
                return View("Index", Build(cart, addressId, null));
            }

            int aid = addressId;
            if (aid > 0)
            {
                ModelState.Clear();    // فرم آدرس جدید خالی است و نباید خطا بدهد
                // فقط آدرس‌های خود کاربر؛ جلوگیری از دسترسی به آدرس دیگران
                if (!db.Addresses.Any(a => a.Id == aid && a.UserId == me.Id)) return HttpNotFound();
            }
            else
            {
                if (newAddress == null)
                {
                    ModelState.AddModelError("", "اطلاعات آدرس را کامل کن");
                    return View("Index", Build(cart, 0, null));
                }
                newAddress.Phone = Digits.ToEnglish(newAddress.Phone ?? "");
                newAddress.PostalCode = Digits.ToEnglish(newAddress.PostalCode ?? "");
                if (!ModelState.IsValid) return View("Index", Build(cart, 0, newAddress));
                var addr = new Address
                {
                    UserId = me.Id, Receiver = newAddress.Receiver.Trim(), Phone = newAddress.Phone,
                    Province = newAddress.Province.Trim(), City = newAddress.City.Trim(),
                    PostalCode = string.IsNullOrWhiteSpace(newAddress.PostalCode) ? null : newAddress.PostalCode,
                    Details = newAddress.Details.Trim()
                };
                db.Addresses.Add(addr);
                db.SaveChanges();
                aid = addr.Id;
            }

            Order order;
            var error = OrderService.Create(db, me.Id, aid, cart.Items, out order);
            if (error != null)
            {
                ModelState.AddModelError("", error);
                CartStore.Sync(db, cart);
                return View("Index", Build(cart, aid, null));
            }

            cart.Items.Clear();      // کالاها در سفارش رزرو شده‌اند
            return StartPayment(order);
        }
    }
}
