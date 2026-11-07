using System;
using System.Configuration;
using System.Linq;
using System.Web.Mvc;
using System.Web.Security;
using Ghaforv3.Models;
using Ghaforv3.Security;

namespace Ghaforv3.Controllers
{
    public class AccountController : Controller
    {
        private readonly Ghaforv3Db db = new Ghaforv3Db();

        // برای یکسان‌سازی زمان پاسخ وقتی شماره وجود ندارد (جلوگیری از تشخیص «کدام شماره‌ها ثبت‌نام کرده‌اند» از روی زمان)
        private static readonly string DummyHash = PasswordHasher.Hash("not-a-real-password");

        // ---------- حساب من ----------
        public ActionResult Index()
        {
            var me = CurrentUser.Get(HttpContext);
            if (me == null || !me.IsActive)
            {
                FormsAuthentication.SignOut();
                return RedirectToAction("Login", new { returnUrl = Url.Action("Index", "Account") });
            }
            var vm = new AccountVm
            {
                FullName = me.FullName, Mobile = me.Mobile, Email = me.Email, IsStaff = me.Role >= 1,
                Orders = db.Orders.Where(o => o.UserId == me.Id).OrderByDescending(o => o.CreatedAt).Take(20)
                    .Select(o => new OrderRow { Id = o.Id, Customer = "", Total = o.TotalAmount, Status = o.Status, CreatedAt = o.CreatedAt }).ToList()
            };
            return View(vm);
        }

        // ---------- ورود ----------
        [HttpGet]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginVm());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Login(LoginVm m, string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            if (!ModelState.IsValid) return View(m);

            var mobile = Digits.ToEnglish(m.Mobile ?? "");
            if (mobile.Length > 15) mobile = mobile.Substring(0, 15);
            var ip = Request.UserHostAddress;
            if (LoginThrottle.IsLocked(mobile, ip))
            {
                ModelState.AddModelError("", "تعداد تلاش‌های ناموفق زیاد بود؛ ۱۰ دقیقه بعد دوباره امتحان کن");
                return View(m);
            }

            var u = db.Users.FirstOrDefault(x => x.Mobile == mobile);
            bool ok = PasswordHasher.Verify(m.Password, u == null ? DummyHash : u.PasswordHash);
            if (u == null || !u.IsActive || !ok)
            {
                LoginThrottle.Fail(mobile, ip);
                ModelState.AddModelError("", "شمارهٔ موبایل یا رمز عبور اشتباه است");
                return View(m);
            }

            LoginThrottle.Clear(mobile, ip);
            if (PasswordHasher.NeedsRehash(u.PasswordHash))          // ارتقای هش قدیمی به فرمت جدید و قوی‌تر
            {
                u.PasswordHash = PasswordHasher.Hash(m.Password);
                db.SaveChanges();
            }
            FormsAuthentication.SetAuthCookie(u.Id.ToString(), m.Remember);
            if (Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
            return u.Role >= 1
                ? RedirectToAction("Index", "Admin", new { area = "Admin" })
                : RedirectToAction("Index", "Home");
        }

        // ---------- ثبت‌نام (کاربر عادی) ----------
        [HttpGet]
        public ActionResult Register(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View(new RegisterVm());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Register(RegisterVm m, string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            m.Mobile = Digits.ToEnglish(m.Mobile ?? "");
            if (LoginThrottle.Hit("register", Request.UserHostAddress, 10, TimeSpan.FromHours(1)))
            {
                ModelState.AddModelError("", "تعداد ثبت‌نام از این شبکه زیاد بود؛ کمی بعد دوباره امتحان کن");
                return View(m);
            }
            if (db.Users.Any(x => x.Mobile == m.Mobile)) ModelState.AddModelError("Mobile", "این شماره قبلاً ثبت‌نام کرده؛ وارد شو");
            if (!ModelState.IsValid) return View(m);

            var user = new User
            {
                FullName = m.FullName.Trim(), Mobile = m.Mobile,
                Email = string.IsNullOrWhiteSpace(m.Email) ? null : m.Email.Trim(),
                PasswordHash = PasswordHasher.Hash(m.Password),
                Role = 0, IsActive = true, CreatedAt = DateTime.Now      // همیشه کاربر عادی
            };
            db.Users.Add(user);
            db.SaveChanges();
            FormsAuthentication.SetAuthCookie(user.Id.ToString(), false);
            if (Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction("Index", "Account");
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("Index", "Home");
        }

        public ActionResult Denied() { return View(); }

        // ---------- ساخت اولین سوپر ادمین ----------
        // امنیت: تا وقتی سوپر ادمین نداریم این صفحه باز است، پس باید قفل باشد.
        //  • اگر در Web.config مقدار SetupKey پر باشد، ساخت ادمین فقط با همان کلید ممکن است
        //  • اگر خالی باشد، فقط از روی خود سرور (localhost) کار می‌کند
        private static string SetupKey { get { return ConfigurationManager.AppSettings["SetupKey"]; } }
        private bool SetupOpen()
        {
            if (db.Users.Any(u => u.Role == 2)) return false;
            return !string.IsNullOrWhiteSpace(SetupKey) || Request.IsLocal;
        }

        [HttpGet]
        public ActionResult Setup()
        {
            if (!SetupOpen()) return HttpNotFound();
            ViewBag.NeedKey = !string.IsNullOrWhiteSpace(SetupKey);
            return View(new SetupVm());
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult Setup(SetupVm m)
        {
            if (!SetupOpen()) return HttpNotFound();
            ViewBag.NeedKey = !string.IsNullOrWhiteSpace(SetupKey);
            if (LoginThrottle.Hit("setup", Request.UserHostAddress, 10, TimeSpan.FromMinutes(10)))
            {
                ModelState.AddModelError("", "تلاش‌های زیاد؛ ده دقیقه بعد دوباره امتحان کن");
                return View(m);
            }
            if (!string.IsNullOrWhiteSpace(SetupKey) && !PasswordHasher.SecureEquals(SetupKey, m.SetupKey ?? ""))
                ModelState.AddModelError("SetupKey", "کلید راه‌اندازی درست نیست");
            m.Mobile = Digits.ToEnglish(m.Mobile ?? "");
            if (db.Users.Any(u => u.Mobile == m.Mobile)) ModelState.AddModelError("Mobile", "این شماره قبلاً ثبت شده");
            if (!ModelState.IsValid) return View(m);

            var user = new User
            {
                FullName = m.FullName.Trim(), Mobile = m.Mobile,
                PasswordHash = PasswordHasher.Hash(m.Password),
                Role = 2, IsActive = true, CreatedAt = DateTime.Now
            };
            db.Users.Add(user);
            db.SaveChanges();
            FormsAuthentication.SetAuthCookie(user.Id.ToString(), false);
            return RedirectToAction("Index", "Admin", new { area = "Admin" });
        }

        // ---------- ویرایش پروفایل و تغییر رمز ----------
        [UserAuthorize]
        public ActionResult Profile()
        {
            var me = CurrentUser.Get(HttpContext);
            return View(new ProfileVm { FullName = me.FullName, Email = me.Email, Mobile = me.Mobile });
        }

        [UserAuthorize, HttpPost, ValidateAntiForgeryToken]
        public ActionResult Profile(ProfileVm m)
        {
            var me = CurrentUser.Get(HttpContext);
            m.Mobile = me.Mobile;     // شمارهٔ موبایل از اینجا تغییر نمی‌کند
            if (!ModelState.IsValid) return View(m);
            var u = db.Users.First(x => x.Id == me.Id);
            u.FullName = m.FullName.Trim();
            u.Email = string.IsNullOrWhiteSpace(m.Email) ? null : m.Email.Trim();
            db.SaveChanges();
            TempData["ok"] = "پروفایل ذخیره شد";
            return RedirectToAction("Index");
        }

        [UserAuthorize]
        public ActionResult Password() { return View(new PasswordVm()); }

        [UserAuthorize, HttpPost, ValidateAntiForgeryToken]
        public ActionResult Password(PasswordVm m)
        {
            var me = CurrentUser.Get(HttpContext);
            if (LoginThrottle.Hit("pwd", me.Id.ToString(), 8, TimeSpan.FromMinutes(10)))
            {
                ModelState.AddModelError("", "تلاش‌های زیاد؛ ده دقیقه بعد دوباره امتحان کن");
                return View(new PasswordVm());
            }
            if (!ModelState.IsValid) return View(new PasswordVm());
            var u = db.Users.First(x => x.Id == me.Id);
            if (!PasswordHasher.Verify(m.Current, u.PasswordHash))
            {
                ModelState.AddModelError("Current", "رمز فعلی درست نیست");
                return View(new PasswordVm());
            }
            u.PasswordHash = PasswordHasher.Hash(m.New);
            db.SaveChanges();
            TempData["ok"] = "رمز عبور تغییر کرد";
            return RedirectToAction("Index");
        }

        protected override void Dispose(bool disposing) { if (disposing) db.Dispose(); base.Dispose(disposing); }
    }
}
