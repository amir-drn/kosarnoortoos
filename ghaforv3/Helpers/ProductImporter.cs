using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Ghaforv3.Models;
using Ghaforv3.Security;

namespace Ghaforv3.Helpers
{
    public class ImportRow
    {
        public int Line;                        // شمارهٔ سطر در فایل اکسل
        public string Action = "skip";          // create | update | error
        public string Title, Slug, Unit, Description, CategoryName, BrandName;
        public decimal? Price, OldPrice;
        public int? Stock;
        public bool? IsActive;
        public string Code, Warranty, BodyColors, LightColors;
        public int? CartonQty;
        public int? ProductId, CategoryId, BrandId;
        public bool NewCategory, NewBrand;
        public List<string> Errors = new List<string>();
        public List<string> Warnings = new List<string>();
    }

    public class ImportPlan
    {
        public List<ImportRow> Rows = new List<ImportRow>();
        public string Fatal;
        public string FileName;
        public int Created, Updated, ErrorCount;
    }

    // ورود محصولات از اکسل/CSV: ستون‌ها از روی «عنوان ستون» تشخیص داده می‌شوند (ترتیب مهم نیست).
    // کالایی که از قبل هست (بر اساس کد/Slug یا نام دقیق) به‌روزرسانی می‌شود؛ وگرنه کالای جدید ساخته می‌شود.
    // خانهٔ خالی، مقدار فعلی را پاک نمی‌کند.
    public static class ProductImporter
    {
        public const int MaxRows = 2000;

        public static readonly string[] Headers =
        {
            "نام کالا", "قیمت (تومان)", "قیمت قبل از تخفیف", "موجودی", "دسته‌بندی", "برند", "واحد", "فعال", "کد (Slug)", "توضیحات",
            "کد محصول", "تعداد در کارتن", "گارانتی", "رنگ بدنه", "رنگ نور"
        };
        public static readonly double[] Widths = { 38, 16, 18, 10, 18, 16, 10, 8, 24, 50, 14, 14, 40, 28, 36 };

        private class Cols { public int Title = -1, Price = -1, Old = -1, Stock = -1, Cat = -1, Brand = -1, Unit = -1, Desc = -1, Active = -1, Slug = -1, Code = -1, Carton = -1, Warranty = -1, Body = -1, Light = -1; }

        private static readonly Dictionary<string, string[]> Aliases = new Dictionary<string, string[]>
        {
            { "title", new[] { "نام کالا", "نام محصول", "عنوان", "نام", "title", "name", "product" } },
            { "price", new[] { "قیمت", "قیمت تومان", "قیمت (تومان)", "قیمت فروش", "price" } },
            { "old", new[] { "قیمت قبل از تخفیف", "قیمت قبلی", "قیمت اصلی", "oldprice", "old price" } },
            { "stock", new[] { "موجودی", "تعداد", "موجودی انبار", "stock", "qty", "quantity" } },
            { "cat", new[] { "دسته", "دسته‌بندی", "دسته بندی", "گروه", "category" } },
            { "brand", new[] { "برند", "brand" } },
            { "unit", new[] { "واحد", "unit" } },
            { "desc", new[] { "توضیحات", "شرح", "description" } },
            { "active", new[] { "فعال", "وضعیت", "active", "isactive" } },
            { "slug", new[] { "کد (slug)", "slug", "آدرس لاتین", "آدرس" } },
            { "code", new[] { "کد محصول", "کد کالا", "کد", "شناسه کالا", "شناسه", "product code", "code", "sku" } },
            { "carton", new[] { "تعداد در کارتن", "تعداد در بسته", "تعداد کارتن", "carton", "cartonqty" } },
            { "warranty", new[] { "گارانتی", "توضیحات گارانتی", "warranty" } },
            { "body", new[] { "رنگ بدنه", "رنگ‌های بدنه", "رنگ های بدنه", "bodycolor", "body color" } },
            { "light", new[] { "رنگ نور", "رنگ‌های نور", "رنگ های نور", "دمای رنگ", "lightcolor", "light color" } }
        };

        // ---------- نرمال‌سازی ----------
        public static string Clean(string s)
        {
            if (s == null) return null;
            s = s.Replace('\u064A', '\u06CC').Replace('\u0643', '\u06A9');          // ي ك عربی → ی ک فارسی
            s = Regex.Replace(s, @"\s+", " ").Trim();
            return s.Length == 0 ? null : s;
        }

        private static string Key(string s)
        {
            return (Digits.ToEnglish(Clean(s) ?? "")).ToLowerInvariant();
        }

        private static string HeaderKey(string s)
        {
            var k = Key(s);
            var sb = new StringBuilder();
            foreach (var ch in k) if (char.IsLetterOrDigit(ch)) sb.Append(ch);
            return sb.ToString();
        }

        private static Cols MapColumns(string[] header)
        {
            var map = new Cols();
            for (int i = 0; i < header.Length; i++)
            {
                var hk = HeaderKey(header[i]);
                if (hk.Length == 0) continue;
                foreach (var f in Aliases)
                {
                    if (!f.Value.Any(a => HeaderKey(a) == hk)) continue;
                    switch (f.Key)
                    {
                        case "title": if (map.Title < 0) map.Title = i; break;
                        case "price": if (map.Price < 0) map.Price = i; break;
                        case "old": if (map.Old < 0) map.Old = i; break;
                        case "stock": if (map.Stock < 0) map.Stock = i; break;
                        case "cat": if (map.Cat < 0) map.Cat = i; break;
                        case "brand": if (map.Brand < 0) map.Brand = i; break;
                        case "unit": if (map.Unit < 0) map.Unit = i; break;
                        case "desc": if (map.Desc < 0) map.Desc = i; break;
                        case "active": if (map.Active < 0) map.Active = i; break;
                        case "slug": if (map.Slug < 0) map.Slug = i; break;
                        case "code": if (map.Code < 0) map.Code = i; break;
                        case "carton": if (map.Carton < 0) map.Carton = i; break;
                        case "warranty": if (map.Warranty < 0) map.Warranty = i; break;
                        case "body": if (map.Body < 0) map.Body = i; break;
                        case "light": if (map.Light < 0) map.Light = i; break;
                    }
                    break;
                }
            }
            return map;
        }

        private static string Cell(string[] r, int idx)
        {
            if (idx < 0 || idx >= r.Length) return null;
            return Clean(r[idx]);
        }

        private static bool TryNumber(string s, out decimal v)
        {
            v = 0;
            if (string.IsNullOrWhiteSpace(s)) return false;
            s = Digits.ToEnglish(s).Replace("تومان", "").Replace("ریال", "").Replace(",", "").Replace("\u066C", "").Replace("\u060C", "").Replace(" ", "").Replace("\u066B", ".");
            if (!Regex.IsMatch(s, @"^-?\d+(\.\d+)?([eE][+-]?\d+)?$")) return false;
            return decimal.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out v);
        }

        private static string Slugify(string title)
        {
            var sb = new StringBuilder();
            bool dash = false;
            foreach (var ch in Digits.ToEnglish(title ?? "").ToLowerInvariant())
            {
                if ((ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9')) { sb.Append(ch); dash = false; }
                else if (sb.Length > 0 && !dash) { sb.Append('-'); dash = true; }
            }
            var s = sb.ToString().Trim('-');
            if (s.Length > 60) s = s.Substring(0, 60).Trim('-');
            return s;
        }

        // آدرس لاتین یکتا؛ اگر عنوان حرف لاتین نداشت، یک کد تصادفی کوتاه می‌سازد
        private static string UniqueSlug(string title, string prefix, HashSet<string> used)
        {
            var s = Slugify(title);
            if (s.Length < 3) s = prefix + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
            var c = s; int n = 2;
            while (used.Contains(c)) c = s + "-" + (n++);
            used.Add(c);
            return c;
        }

        private static int FindHeader(List<string[]> table)
        {
            for (int i = 0; i < Math.Min(table.Count, 10); i++)
            {
                var m = MapColumns(table[i]);
                if (m.Title >= 0 && m.Price >= 0) return i;
            }
            return -1;
        }

        // ---------- ساخت پیش‌نمایش (چیزی در دیتابیس نوشته نمی‌شود) ----------
        public static ImportPlan Build(Ghaforv3Db db, List<string[]> table, string fileName)
        {
            var plan = new ImportPlan { FileName = fileName };
            int h = FindHeader(table);
            if (h < 0)
            {
                plan.Fatal = "ردیف عنوان ستون‌ها پیدا نشد. ستون‌های «نام کالا» و «قیمت» لازم‌اند؛ بهتر است از دکمهٔ «دریافت قالب» استفاده کنی.";
                return plan;
            }
            var map = MapColumns(table[h]);
            if (table.Count - h - 1 > MaxRows) { plan.Fatal = "در هر بار حداکثر " + MaxRows + " ردیف قابل ورود است؛ فایل را به چند بخش تقسیم کن."; return plan; }

            var prods = db.Products.Select(p => new { p.Id, p.Slug, p.Title, p.Code }).ToList();
            var bySlug = new Dictionary<string, int>();
            var byTitle = new Dictionary<string, int>();
            var byCode = new Dictionary<string, int>();
            var usedSlugs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var p in prods)
            {
                bySlug[p.Slug.ToLowerInvariant()] = p.Id;
                usedSlugs.Add(p.Slug);
                var k = Key(p.Title);
                if (!byTitle.ContainsKey(k)) byTitle[k] = p.Id;
                if (!string.IsNullOrWhiteSpace(p.Code) && !byCode.ContainsKey(Key(p.Code))) byCode[Key(p.Code)] = p.Id;
            }
            var cats = db.Categories.Select(c => new { c.Id, c.Title, c.Slug }).ToList();
            var catByKey = new Dictionary<string, int>();
            foreach (var c in cats) { catByKey[Key(c.Title)] = c.Id; catByKey[c.Slug.ToLowerInvariant()] = c.Id; }
            var brands = db.Brands.Select(b => new { b.Id, b.Title, b.Slug }).ToList();
            var brandByKey = new Dictionary<string, int>();
            foreach (var b in brands) { brandByKey[Key(b.Title)] = b.Id; brandByKey[b.Slug.ToLowerInvariant()] = b.Id; }

            var seenIds = new HashSet<int>();
            var seenNewTitles = new HashSet<string>();
            var seenNewCodes = new HashSet<string>();

            for (int i = h + 1; i < table.Count; i++)
            {
                var r = table[i];
                if (r == null || r.All(string.IsNullOrWhiteSpace)) continue;
                var row = new ImportRow { Line = i + 1 };
                plan.Rows.Add(row);

                row.Title = Cell(r, map.Title);
                if (row.Title == null) row.Errors.Add("نام کالا خالی است");
                else if (row.Title.Length > 200) row.Errors.Add("نام کالا بیشتر از ۲۰۰ کاراکتر است");

                // --- تشخیص کالای موجود ---
                var slugRaw = Cell(r, map.Slug);
                if (slugRaw != null) slugRaw = Digits.ToEnglish(slugRaw).ToLowerInvariant();
                if (slugRaw != null && (slugRaw.Length > 220 || !Regex.IsMatch(slugRaw, "^[a-z0-9-]+$")))
                { row.Errors.Add("کد (Slug) فقط حروف کوچک انگلیسی، عدد و خط تیره می‌تواند باشد"); slugRaw = null; }

                row.Code = Cell(r, map.Code);
                if (row.Code != null && row.Code.Length > 50) { row.Errors.Add("کد محصول بیشتر از ۵۰ کاراکتر است"); row.Code = null; }

                int? id = null;
                int found;
                if (slugRaw != null && bySlug.TryGetValue(slugRaw, out found)) id = found;
                else if (row.Code != null && byCode.TryGetValue(Key(row.Code), out found)) id = found;
                else if (row.Title != null && byTitle.TryGetValue(Key(row.Title), out found))
                {
                    id = found;
                    if (slugRaw != null) row.Warnings.Add("کد پیدا نشد؛ بر اساس نام کالا به‌روزرسانی شد");
                }
                row.ProductId = id;

                if (id.HasValue)
                {
                    if (!seenIds.Add(id.Value)) row.Errors.Add("این کالا قبلاً در همین فایل آمده است");
                }
                else if (row.Title != null && !seenNewTitles.Add(Key(row.Title)))
                    row.Errors.Add("این نام کالا قبلاً در همین فایل آمده است");
                if (!id.HasValue && row.Code != null && !seenNewCodes.Add(Key(row.Code)))
                    row.Errors.Add("این کد محصول قبلاً در همین فایل آمده است");

                // --- قیمت‌ها ---
                var priceCell = Cell(r, map.Price);
                decimal pv;
                if (priceCell != null)
                {
                    if (!TryNumber(priceCell, out pv) || pv < 0 || pv > 999999999999m) row.Errors.Add("قیمت معتبر نیست: «" + priceCell + "»");
                    else row.Price = Math.Round(pv, 0, MidpointRounding.AwayFromZero);
                }
                else if (!id.HasValue) row.Errors.Add("قیمت کالای جدید لازم است");

                var oldCell = Cell(r, map.Old);
                if (oldCell != null)
                {
                    if (!TryNumber(oldCell, out pv) || pv < 0 || pv > 999999999999m) row.Errors.Add("قیمت قبل از تخفیف معتبر نیست: «" + oldCell + "»");
                    else
                    {
                        pv = Math.Round(pv, 0, MidpointRounding.AwayFromZero);
                        if (row.Price.HasValue && pv <= row.Price.Value) row.Warnings.Add("قیمت قبل از تخفیف از قیمت فروش بیشتر نیست؛ نادیده گرفته شد");
                        else if (pv > 0) row.OldPrice = pv;
                    }
                }

                // --- موجودی، واحد، فعال، توضیحات ---
                var stockCell = Cell(r, map.Stock);
                if (stockCell != null)
                {
                    if (!TryNumber(stockCell, out pv) || pv < 0 || pv > 1000000 || pv != Math.Floor(pv)) row.Errors.Add("موجودی باید عدد صحیح بین ۰ تا ۱٬۰۰۰٬۰۰۰ باشد: «" + stockCell + "»");
                    else row.Stock = (int)pv;
                }
                row.Unit = Cell(r, map.Unit);
                if (row.Unit != null && row.Unit.Length > 20) row.Errors.Add("واحد بیشتر از ۲۰ کاراکتر است");
                row.Description = Cell(r, map.Desc);
                if (row.Description != null && row.Description.Length > 4000) row.Errors.Add("توضیحات بیشتر از ۴۰۰۰ کاراکتر است");

                var activeCell = Cell(r, map.Active);
                if (activeCell != null)
                {
                    var a = Key(activeCell);
                    if (new[] { "بله", "فعال", "1", "true", "yes", "y" }.Contains(a)) row.IsActive = true;
                    else if (new[] { "خیر", "غیرفعال", "0", "false", "no", "n" }.Contains(a)) row.IsActive = false;
                    else row.Errors.Add("ستون «فعال» فقط بله/خیر می‌پذیرد: «" + activeCell + "»");
                }

                // --- تعداد در کارتن، گارانتی، رنگ‌ها ---
                var cartonCell = Cell(r, map.Carton);
                if (cartonCell != null)
                {
                    if (!TryNumber(cartonCell, out pv) || pv < 1 || pv > 100000 || pv != Math.Floor(pv)) row.Errors.Add("تعداد در کارتن باید عدد صحیح مثبت باشد: «" + cartonCell + "»");
                    else row.CartonQty = (int)pv;
                }
                row.Warranty = Cell(r, map.Warranty);
                if (row.Warranty != null && row.Warranty.Length > 1000) row.Errors.Add("توضیحات گارانتی بیشتر از ۱۰۰۰ کاراکتر است");
                var bodyRaw = Cell(r, map.Body);
                if (bodyRaw != null) { var e = ProductOptions.Validate(bodyRaw); if (e != null) row.Errors.Add("رنگ بدنه: " + e); else row.BodyColors = ProductOptions.Normalize(bodyRaw); }
                var lightRaw = Cell(r, map.Light);
                if (lightRaw != null) { var e = ProductOptions.Validate(lightRaw); if (e != null) row.Errors.Add("رنگ نور: " + e); else row.LightColors = ProductOptions.Normalize(lightRaw); }

                // --- دسته و برند (اگر نبودند، ساخته می‌شوند) ---
                var catCell = Cell(r, map.Cat);
                if (catCell != null)
                {
                    if (catCell.Length > 100) row.Errors.Add("نام دسته بیشتر از ۱۰۰ کاراکتر است");
                    else
                    {
                        int cid;
                        if (catByKey.TryGetValue(Key(catCell), out cid)) row.CategoryId = cid;
                        else { row.NewCategory = true; row.CategoryName = catCell; }
                    }
                }
                else if (!id.HasValue) row.Errors.Add("دسته‌بندی کالای جدید لازم است");

                var brandCell = Cell(r, map.Brand);
                if (brandCell != null)
                {
                    if (brandCell.Length > 100) row.Errors.Add("نام برند بیشتر از ۱۰۰ کاراکتر است");
                    else
                    {
                        int bid;
                        if (brandByKey.TryGetValue(Key(brandCell), out bid)) row.BrandId = bid;
                        else { row.NewBrand = true; row.BrandName = brandCell; }
                    }
                }

                // --- کد یکتا برای کالای جدید ---
                if (!id.HasValue && row.Errors.Count == 0)
                {
                    if (slugRaw != null)
                    {
                        if (usedSlugs.Contains(slugRaw)) row.Errors.Add("این کد (Slug) قبلاً استفاده شده");
                        else { usedSlugs.Add(slugRaw); row.Slug = slugRaw; }
                    }
                    else row.Slug = UniqueSlug(row.Title, "p", usedSlugs);
                }

                row.Action = row.Errors.Count > 0 ? "error" : (id.HasValue ? "update" : "create");
                if (row.Action == "create") plan.Created++;
                else if (row.Action == "update") plan.Updated++;
                else plan.ErrorCount++;
            }

            if (plan.Rows.Count == 0) plan.Fatal = "هیچ ردیف کالایی زیر عنوان ستون‌ها پیدا نشد.";
            return plan;
        }

        // ---------- ثبت نهایی (همه‌چیز در یک تراکنش؛ اگر خطا شود هیچ تغییری ثبت نمی‌شود) ----------
        public static void Apply(Ghaforv3Db db, ImportPlan plan)
        {
            var good = plan.Rows.Where(r => r.Action != "error").ToList();
            if (good.Count == 0) return;

            using (var tx = db.Database.BeginTransaction())
            {
                var catSlugs = new HashSet<string>(db.Categories.Select(c => c.Slug), StringComparer.OrdinalIgnoreCase);
                var brandSlugs = new HashSet<string>(db.Brands.Select(b => b.Slug), StringComparer.OrdinalIgnoreCase);
                int order = db.Categories.Max(c => (int?)c.SortOrder) ?? 0;
                var newCats = new Dictionary<string, Category>();
                var newBrands = new Dictionary<string, Brand>();

                var updIds = good.Where(r => r.Action == "update").Select(r => r.ProductId.Value).ToList();
                var existing = db.Products.Where(p => updIds.Contains(p.Id)).ToDictionary(p => p.Id);

                foreach (var row in good)
                {
                    Category cat = null; Brand brand = null;
                    if (row.NewCategory)
                    {
                        var k = Key(row.CategoryName);
                        if (!newCats.TryGetValue(k, out cat))
                        {
                            cat = new Category { Title = row.CategoryName, Slug = UniqueSlug(row.CategoryName, "cat", catSlugs), IsActive = true, SortOrder = ++order };
                            db.Categories.Add(cat);
                            newCats[k] = cat;
                        }
                    }
                    if (row.NewBrand)
                    {
                        var k = Key(row.BrandName);
                        if (!newBrands.TryGetValue(k, out brand))
                        {
                            brand = new Brand { Title = row.BrandName, Slug = UniqueSlug(row.BrandName, "brand", brandSlugs) };
                            db.Brands.Add(brand);
                            newBrands[k] = brand;
                        }
                    }

                    Product p;
                    if (row.Action == "create")
                    {
                        p = new Product
                        {
                            Title = row.Title, Slug = row.Slug, Description = row.Description,
                            Price = row.Price.Value, OldPrice = row.OldPrice, Stock = row.Stock ?? 0,
                            Unit = row.Unit ?? "عدد", IsActive = row.IsActive ?? true, CreatedAt = DateTime.Now,
                            Code = row.Code, CartonQty = row.CartonQty, Warranty = row.Warranty, BodyColors = row.BodyColors, LightColors = row.LightColors
                        };
                        db.Products.Add(p);
                    }
                    else
                    {
                        if (!existing.TryGetValue(row.ProductId.Value, out p)) continue;
                        if (row.Price.HasValue) p.Price = row.Price.Value;
                        if (row.OldPrice.HasValue) p.OldPrice = row.OldPrice;
                        else if (row.Price.HasValue && p.OldPrice.HasValue && p.OldPrice.Value <= p.Price) p.OldPrice = null;   // تخفیف قدیمی دیگر معنی ندارد
                        if (row.Stock.HasValue) p.Stock = row.Stock.Value;
                        if (row.Description != null) p.Description = row.Description;
                        if (row.Unit != null) p.Unit = row.Unit;
                        if (row.IsActive.HasValue) p.IsActive = row.IsActive.Value;
                        if (row.Code != null) p.Code = row.Code;
                        if (row.CartonQty.HasValue) p.CartonQty = row.CartonQty;
                        if (row.Warranty != null) p.Warranty = row.Warranty;
                        if (row.BodyColors != null) p.BodyColors = row.BodyColors;
                        if (row.LightColors != null) p.LightColors = row.LightColors;
                    }
                    if (cat != null) p.Category = cat; else if (row.CategoryId.HasValue) p.CategoryId = row.CategoryId.Value;
                    if (brand != null) p.Brand = brand; else if (row.BrandId.HasValue) p.BrandId = row.BrandId.Value;
                }
                db.SaveChanges();
                tx.Commit();
            }
        }

        // ---------- خروجی ----------
        public static List<object[]> ExportRows(Ghaforv3Db db)
        {
            var list = new List<object[]> { Headers.Cast<object>().ToArray() };
            var items = db.Products.OrderBy(p => p.Id).Select(p => new
            {
                p.Title, p.Price, p.OldPrice, p.Stock, Category = p.Category.Title,
                Brand = p.Brand == null ? null : p.Brand.Title, p.Unit, p.IsActive, p.Slug, p.Description,
                p.Code, p.CartonQty, p.Warranty, p.BodyColors, p.LightColors
            }).ToList();
            foreach (var p in items)
                list.Add(new object[] { p.Title, p.Price, p.OldPrice, p.Stock, p.Category, p.Brand, p.Unit, p.IsActive ? "بله" : "خیر", p.Slug, p.Description, p.Code, p.CartonQty, p.Warranty, p.BodyColors, p.LightColors });
            return list;
        }
    }
}
