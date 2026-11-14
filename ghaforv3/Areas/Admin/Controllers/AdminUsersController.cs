using System;
using System.Linq;
using System.Web.Mvc;
using Ghaforv3.Models;
using Ghaforv3.Security;

namespace Ghaforv3.Areas.Admin.Controllers
{
    // سوپر ادمین همهٔ نقش‌ها را می‌بیند و می‌سازد؛ ادمین فقط «کاربر عادی» را
    public class AdminUsersController : BaseAdminController
    {
        private const int PageSize = 10;

        private byte[] Allowed { get { return Me.Role == 2 ? new byte[] { 0, 1, 2 } : new byte[] { 0 }; } }

        public ActionResult Index(string q, int page = 1)
        {
            // نکته: Contains روی byte[] در EF ترجمه نمی‌شود (خطای ۵۰۰)؛ پس با شرط ساده فیلتر می‌کنیم
            IQueryable<User> query = db.Users;
            if (Me.Role != 2) query = query.Where(u => u.Role == 0);
            if (!string.IsNullOrWhiteSpace(q)) query = query.Where(u => u.FullName.Contains(q) || u.Mobile.Contains(q));
            int total = query.Count();
            int pages = (int)Math.Ceiling(total / (double)PageSize);
            page = ClampPage(page, pages);
            var rows = query.OrderByDescending(u => u.Id).Skip((page - 1) * PageSize).Take(PageSize)
                .Select(u => new UserRow { Id = u.Id, FullName = u.FullName, Mobile = u.Mobile, Role = u.Role, IsActive = u.IsActive }).ToList();
            return View(new PagedVm<UserRow> { Items = rows, Page = page, Pages = pages, Total = total, Q = q });
        }

        public ActionResult Create() { return View("Edit", Fill(new UserFormVm { IsActive = true })); }

        public ActionResult Edit(int id)
        {
            var u = db.Users.Find(id);
            if (u == null || !Allowed.Contains(u.Role)) return HttpNotFound();
            return View(Fill(new UserFormVm { Id = u.Id, FullName = u.FullName, Mobile = u.Mobile, Email = u.Email, Role = u.Role, IsActive = u.IsActive }));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Save(UserFormVm m)
        {
            m.Mobile = Digits.ToEnglish(m.Mobile ?? "");
            bool isNew = m.Id == 0;

            if (!Allowed.Contains(m.Role)) ModelState.AddModelError("Role", "اجازهٔ انتخاب این نقش را نداری");
            if (db.Users.Any(x => x.Mobile == m.Mobile && x.Id != m.Id)) ModelState.AddModelError("Mobile", "این شماره قبلاً ثبت شده");
            if (isNew && string.IsNullOrEmpty(m.Password)) ModelState.AddModelError("Password", "رمز عبور را وارد کن");
            if (!string.IsNullOrEmpty(m.Password) && (m.Password.Length < 8 || m.Password.Length > 100)) ModelState.AddModelError("Password", "رمز باید بین ۸ تا ۱۰۰ کاراکتر باشد");

            var u = isNew ? null : db.Users.Find(m.Id);
            if (!isNew)
            {
                if (u == null) return HttpNotFound();
                if (!Allowed.Contains(u.Role)) return new HttpStatusCodeResult(403);
                if (u.Id == Me.Id && (m.Role != u.Role || !m.IsActive))
                    ModelState.AddModelError("", "نقش یا فعال‌بودن حساب خودت را نمی‌توانی تغییر بدهی");
            }
            if (!ModelState.IsValid) return View("Edit", Fill(m));

            if (isNew) { u = new User { CreatedAt = DateTime.Now }; db.Users.Add(u); }
            u.FullName = m.FullName.Trim(); u.Mobile = m.Mobile; u.Email = string.IsNullOrWhiteSpace(m.Email) ? null : m.Email.Trim();
            u.Role = m.Role; u.IsActive = m.IsActive;
            if (!string.IsNullOrEmpty(m.Password)) u.PasswordHash = PasswordHasher.Hash(m.Password);

            db.SaveChanges();
            TempData["ok"] = "کاربر ذخیره شد";
            return RedirectToAction("Index");
        }

        // حذف کاربر: بدون سفارش، نه خودت و نه سوپر ادمین. کاربر دارای سفارش را مسدود (غیرفعال) کن.
        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Delete(int id)
        {
            var u = db.Users.Find(id);
            if (u == null) return HttpNotFound();
            if (!Allowed.Contains(u.Role)) return new HttpStatusCodeResult(403);
            if (u.Id == Me.Id) { TempData["err"] = "حساب خودت را نمی‌توانی حذف کنی"; return RedirectToAction("Edit", new { id }); }
            if (u.Role == 2) { TempData["err"] = "سوپر ادمین حذف نمی‌شود؛ اول نقشش را به کاربر عادی تغییر بده"; return RedirectToAction("Edit", new { id }); }
            if (db.Orders.Any(o => o.UserId == id)) { TempData["err"] = "این کاربر سفارش دارد و حذف نمی‌شود؛ به‌جای حذف، حسابش را مسدود (غیرفعال) کن"; return RedirectToAction("Edit", new { id }); }
            db.Entry(u).Collection(x => x.Products).Load();
            u.Products.Clear();                      // علاقه‌مندی‌ها
            db.Users.Remove(u);                      // آدرس‌ها خودکار حذف می‌شوند
            db.SaveChanges();
            TempData["ok"] = "کاربر حذف شد";
            return RedirectToAction("Index");
        }

        private UserFormVm Fill(UserFormVm m)
        {
            m.Roles = Allowed.Select(r => new SelectListItem { Value = r.ToString(), Text = AdminLabels.Role(r), Selected = r == m.Role }).ToList();
            return m;
        }
    }
}
