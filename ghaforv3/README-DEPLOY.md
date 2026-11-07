# Ghaforv3 — راهنمای راه‌اندازی

پروژهٔ مستقل و جدا از پروژهٔ قبلی: اسم، دیتابیس (`Ghaforv3`)، کوکی‌ها (`.GH3_AUTH`, `GH3_SESSION`, `GH3_AF`) و پورت (10636) متفاوت است، پس هر دو را می‌شود هم‌زمان اجرا کرد.

## اولین اجرا (EF Code First)
1. `Ghaforv3.sln` را در Visual Studio 2019/2022 باز کن؛ NuGet خودکار بسته‌ها (از جمله EntityFramework 6.4.4) را برمی‌گرداند. اگر نه: راست‌کلیک روی Solution ▸ Restore NuGet Packages.
2. در `Ghaforv3/Web.config` بخش `<connectionStrings>`، مقدار `data source=` را اسم SQL Server خودت بگذار (الان `ALIAKBARLAPTOP`؛ نمونه‌های دیگر: `.` ، `.\SQLEXPRESS` ، `(localdb)\MSSQLLocalDB`).
3. F5. اولین درخواست، دیتابیس **Ghaforv3** را با همهٔ جدول‌ها و ایندکس‌ها و چند نمونه‌داده می‌سازد (چند ثانیه صبر کن).
4. برای ساخت مدیر: `http://localhost:10636/admin/setup` (فقط روی localhost).

## تغییر در مدل‌ها بعد از ساخت دیتابیس
چون دیتابیس با `CreateDatabaseIfNotExists` ساخته می‌شود، اگر کلاس‌های `Models/*.cs` یا `Data/Ghaforv3Db.cs` را عوض کنی خطای «model backing the context has changed» می‌گیری. دو راه:
- در توسعه: دیتابیس Ghaforv3 را Drop کن تا دوباره ساخته شود.
- برای دیتای واقعی: در Package Manager Console `Enable-Migrations` ▸ `Add-Migration Init` ▸ `Update-Database`.

## تنظیمات فروشگاه (appSettings)
`SiteUrl`، `ShippingFee`، `FreeShippingFrom`، `ReserveMinutes`، `PaymentGateway` (`Demo` فقط روی localhost / `ZarinPal`)، `ZarinPalMerchantId`، `ContactPhone/Email/Address/Hours`، `Social*`، `SetupKey`.

## جریان خرید
سبد ← `/checkout` (ورود الزامی) ← ساخت سفارش + رزرو موجودی ← درگاه ← `/payment/callback` (تأیید از سرور درگاه) ← `/orders/details/{id}`.
سفارش پرداخت‌نشده بعد از `ReserveMinutes` لغو و موجودی برگردانده می‌شود.

## پیش از انتشار
Publish با پروفایل Release و HTTPS؛ `PaymentGateway=ZarinPal` را تنظیم و یک پرداخت واقعی کوچک تست کن؛ متن صفحه‌های قوانین/حریم خصوصی را بازبینی حقوقی کن.
