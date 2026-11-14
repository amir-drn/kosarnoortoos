using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Ghaforv3.Models
{
    // گزینه‌های قابل انتخاب محصول (رنگ بدنه و رنگ نور). در دیتابیس به‌صورت یک متن با جداکنندهٔ «،» ذخیره می‌شوند.
    public static class ProductOptions
    {
        public const int MaxOptions = 12;
        public const int MaxLength = 50;
        private static readonly char[] Seps = { ',', '\u060C', ';', '\u061B', '|', '/', '\\', '\n', '\r' };   // ویرگول، «،»، ; ، | ، / و \ ، خط جدید

        // یک گزینه؛ می‌تواند قیمت خودش را داشته باشد:
        //   بنفش=65000   ← قیمت نهایی کالا با این گزینه (به‌جای قیمت پایه)
        //   بنفش+5000    ← افزایش نسبت به قیمت پایه
        public class ProductOption
        {
            public string Name;
            public decimal? Absolute;       // قیمت نهایی
            public decimal? Surcharge;      // افزایش
            // تفاوت قیمت این گزینه با قیمت پایه
            public decimal DeltaFrom(decimal basePrice)
            {
                if (Absolute.HasValue) return Absolute.Value - basePrice;
                if (Surcharge.HasValue) return Surcharge.Value;
                return 0;
            }
            public string Token()
            {
                if (Absolute.HasValue) return Name + "=" + decimal.Round(Absolute.Value, 0).ToString("0", CultureInfo.InvariantCulture);
                if (Surcharge.HasValue) return Name + "+" + decimal.Round(Surcharge.Value, 0).ToString("0", CultureInfo.InvariantCulture);
                return Name;
            }
        }

        private static readonly Regex Thousands = new Regex(@"(?<=\d)[,\u060C\u066C](?=\d{3}(?!\d))", RegexOptions.Compiled);
        private static readonly Regex PriceTail = new Regex(@"^(?<n>.+?)\s*(?<op>[=+])\s*(?<v>[\d\u06F0-\u06F9\u0660-\u0669\.\u066B ]+?)\s*(?:تومان)?$", RegexOptions.Compiled);
        private const decimal MaxPrice = 999999999999m;

        private static string LatinDigits(string s)
        {
            var sb = new System.Text.StringBuilder(s.Length);
            foreach (var ch in s)
            {
                if (ch >= '\u06F0' && ch <= '\u06F9') sb.Append((char)('0' + (ch - '\u06F0')));
                else if (ch >= '\u0660' && ch <= '\u0669') sb.Append((char)('0' + (ch - '\u0660')));
                else if (ch == '\u066B') sb.Append('.');
                else sb.Append(ch);
            }
            return sb.ToString();
        }

        // هر تکه از متن را به گزینه تبدیل می‌کند؛ priceError اگر قیمت نامعتبر باشد پر می‌شود
        private static ProductOption ParseOne(string part, out string priceError)
        {
            priceError = null;
            var s = Regex.Replace(part, @"\s+", " ").Trim();
            if (s.Length == 0) return null;
            var o = new ProductOption { Name = s };
            if (s.IndexOf('=') >= 0 || s.IndexOf('+') >= 0)
            {
                var m = PriceTail.Match(s);
                if (m.Success)
                {
                    decimal v;
                    var num = LatinDigits(m.Groups["v"].Value).Replace(" ", "");
                    if (decimal.TryParse(num, NumberStyles.Number, CultureInfo.InvariantCulture, out v) && v >= 0 && v <= MaxPrice)
                    {
                        o.Name = m.Groups["n"].Value.Trim();
                        v = decimal.Round(v, 0, MidpointRounding.AwayFromZero);
                        if (m.Groups["op"].Value == "=") o.Absolute = v; else o.Surcharge = v;
                    }
                    else priceError = "قیمت گزینه «" + s + "» معتبر نیست";
                }
                else if (s.EndsWith("=") || s.EndsWith("+")) priceError = "بعد از = یا + قیمت را بنویس: «" + s + "»";
            }
            return o;
        }

        private static IEnumerable<string> Split(string raw)
        {
            raw = Thousands.Replace(raw ?? "", "");           // 65,000 یا ۶۵٬۰۰۰ را نشکن
            return raw.Split(Seps, StringSplitOptions.RemoveEmptyEntries);
        }

        public static List<ProductOption> ParseOptions(string raw)
        {
            var list = new List<ProductOption>();
            if (string.IsNullOrWhiteSpace(raw)) return list;
            foreach (var part in Split(raw))
            {
                string err;
                var o = ParseOne(part, out err);
                if (o == null || err != null || o.Name.Length == 0 || o.Name.Length > MaxLength) continue;
                if (list.Any(x => x.Name == o.Name)) continue;
                list.Add(o);
                if (list.Count >= MaxOptions) break;
            }
            return list;
        }

        // فقط نام گزینه‌ها
        public static List<string> Parse(string raw)
        {
            return ParseOptions(raw).Select(o => o.Name).ToList();
        }

        // متن استاندارد برای ذخیره؛ خالی = null
        public static string Normalize(string raw)
        {
            var l = ParseOptions(raw);
            return l.Count == 0 ? null : string.Join("\u060C", l.Select(o => o.Token()));
        }

        // پیام خطا برای ورودی مدیر، یا null اگر مشکلی نیست
        public static string Validate(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var names = new List<string>();
            foreach (var part in Split(raw))
            {
                if (part.Trim().Length == 0) continue;
                string err;
                var o = ParseOne(part, out err);
                if (err != null) return err;
                if (o == null) continue;
                if (o.Name.Length == 0 || o.Name.Length > MaxLength) return "هر گزینه حداکثر " + MaxLength + " کاراکتر باشد";
                if (!names.Contains(o.Name)) names.Add(o.Name);
            }
            if (names.Count > MaxOptions) return "حداکثر " + MaxOptions + " گزینه مجاز است";
            return null;
        }

        // قیمت نهایی یک ترکیب رنگ: اگر گزینه قیمت مطلق دارد، تفاوتش با قیمت پایه اعمال می‌شود (و تفاوت دو گروه جمع می‌شود)
        public static decimal PriceFor(decimal basePrice, string bodyColors, string lightColors, string body, string light)
        {
            decimal delta = 0;
            var b = string.IsNullOrEmpty(body) ? null : ParseOptions(bodyColors).FirstOrDefault(o => o.Name == body);
            var l = string.IsNullOrEmpty(light) ? null : ParseOptions(lightColors).FirstOrDefault(o => o.Name == light);
            if (b != null) delta += b.DeltaFrom(basePrice);
            if (l != null) delta += l.DeltaFrom(basePrice);
            return Math.Max(0, basePrice + delta);
        }

        public static string Describe(string body, string light)
        {
            var p = new List<string>();
            if (!string.IsNullOrEmpty(body)) p.Add("رنگ بدنه: " + body);
            if (!string.IsNullOrEmpty(light)) p.Add("رنگ نور: " + light);
            return string.Join(" · ", p);
        }

        // رنگ نمایشی دایرهٔ انتخاب (CSS)؛ از روی نام فارسی/انگلیسی یا عدد کلوین حدس زده می‌شود
        public static string Swatch(string name, bool isLight)
        {
            var n = (name ?? "").ToLowerInvariant().Replace('\u064A', '\u06CC').Replace('\u0643', '\u06A9');
            Func<string[], bool> has = keys => keys.Any(k => n.Contains(k));
            if (isLight)
            {
                if (has(new[] { "rgb", "رنگی", "چندرنگ" })) return "conic-gradient(#ef4444,#facc15,#22c55e,#06b6d4,#3b82f6,#d946ef,#ef4444)";
                var m = Regex.Match(n.Replace(",", "").Replace("\u066C", ""), @"(\d{4})");
                if (m.Success)
                {
                    int k = int.Parse(m.Groups[1].Value);
                    if (k <= 3500) return "#ffb347";
                    if (k <= 5000) return "#fff1c9";
                    return "#dbeafe";
                }
                if (has(new[] { "بنفش", "purple", "violet" })) return "#8b5cf6";
                if (has(new[] { "صورتی", "pink" })) return "#ec4899";
                if (has(new[] { "قرمز", "red" })) return "#ef4444";
                if (has(new[] { "آبی", "blue" })) return "#3b82f6";
                if (has(new[] { "سبز", "green" })) return "#22c55e";
                if (has(new[] { "آفتابی", "گرم", "warm" })) return "#ffb347";
                if (has(new[] { "طبیعی", "خنثی", "neutral", "natural" })) return "#fff1c9";
                if (has(new[] { "مهتابی", "سرد", "یخی", "cool", "white", "سفید" })) return "#dbeafe";
                return "#fde68a";
            }
            if (has(new[] { "سفید", "white" })) return "#ffffff";
            if (has(new[] { "مشکی", "black", "سیاه" })) return "#111827";
            if (has(new[] { "طلایی", "gold" })) return "#d4af37";
            if (has(new[] { "نقره", "silver" })) return "#c0c0c0";
            if (has(new[] { "مسی", "copper" })) return "#b87333";
            if (has(new[] { "برنز", "bronze" })) return "#cd7f32";
            if (has(new[] { "کروم", "نیکل", "استیل", "chrome" })) return "#cfd3d8";
            if (has(new[] { "نوک‌مدادی", "نوک مدادی", "ذغالی", "خاکستری", "طوسی", "gray", "grey", "antracit" })) return "#6b7280";
            if (has(new[] { "بنفش", "purple", "violet" })) return "#8b5cf6";
            if (has(new[] { "صورتی", "pink" })) return "#ec4899";
            if (has(new[] { "قرمز", "red" })) return "#dc2626";
            if (has(new[] { "آبی", "blue" })) return "#2563eb";
            if (has(new[] { "سبز", "green" })) return "#16a34a";
            if (has(new[] { "زرد", "yellow" })) return "#facc15";
            if (has(new[] { "نارنجی", "orange" })) return "#f97316";
            if (has(new[] { "قهوه", "brown" })) return "#7c4a2d";
            if (has(new[] { "کرم", "بژ", "beige", "cream" })) return "#f1e2c0";
            if (has(new[] { "چوب", "wood" })) return "#b08968";
            return "#e5e7eb";
        }
    }

    public partial class Product
    {
        // فقط‌خواندنی؛ در دیتابیس ذخیره نمی‌شوند
        [NotMapped] public List<string> BodyColorList { get { return ProductOptions.Parse(BodyColors); } }
        [NotMapped] public List<string> LightColorList { get { return ProductOptions.Parse(LightColors); } }
        [NotMapped] public bool HasOptions { get { return BodyColorList.Count > 0 || LightColorList.Count > 0; } }
        [NotMapped] public List<ProductOptions.ProductOption> BodyOptions { get { return ProductOptions.ParseOptions(BodyColors); } }
        [NotMapped] public List<ProductOptions.ProductOption> LightOptions { get { return ProductOptions.ParseOptions(LightColors); } }
        // آیا قیمت بسته به رنگ فرق می‌کند؟
        [NotMapped]
        public bool HasPriceVariants
        {
            get
            {
                return BodyOptions.Any(o => o.DeltaFrom(Price) != 0) || LightOptions.Any(o => o.DeltaFrom(Price) != 0);
            }
        }
        public decimal PriceFor(string body, string light) { return ProductOptions.PriceFor(Price, BodyColors, LightColors, body, light); }
    }
}
