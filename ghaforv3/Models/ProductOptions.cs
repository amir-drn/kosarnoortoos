using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Ghaforv3.Models
{
    // گزینه‌های قابل انتخاب محصول (رنگ بدنه و رنگ نور). در دیتابیس به‌صورت یک متن با جداکنندهٔ «،» ذخیره می‌شوند.
    public static class ProductOptions
    {
        public const int MaxOptions = 12;
        public const int MaxLength = 50;
        private static readonly char[] Seps = { ',', '\u060C', ';', '\u061B', '|', '\n', '\r' };

        public static List<string> Parse(string raw)
        {
            var list = new List<string>();
            if (string.IsNullOrWhiteSpace(raw)) return list;
            foreach (var part in raw.Split(Seps, StringSplitOptions.RemoveEmptyEntries))
            {
                var s = Regex.Replace(part, @"\s+", " ").Trim();
                if (s.Length == 0 || s.Length > MaxLength) continue;
                if (!list.Contains(s)) list.Add(s);
                if (list.Count >= MaxOptions) break;
            }
            return list;
        }

        // متن استاندارد برای ذخیره؛ خالی = null
        public static string Normalize(string raw)
        {
            var l = Parse(raw);
            return l.Count == 0 ? null : string.Join("\u060C", l);
        }

        // پیام خطا برای ورودی مدیر، یا null اگر مشکلی نیست
        public static string Validate(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            var parts = raw.Split(Seps, StringSplitOptions.RemoveEmptyEntries).Select(p => p.Trim()).Where(p => p.Length > 0).ToList();
            if (parts.Any(p => p.Length > MaxLength)) return "هر گزینه حداکثر " + MaxLength + " کاراکتر باشد";
            if (parts.Distinct().Count() > MaxOptions) return "حداکثر " + MaxOptions + " گزینه مجاز است";
            return null;
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
        public List<string> BodyColorList { get { return ProductOptions.Parse(BodyColors); } }
        public List<string> LightColorList { get { return ProductOptions.Parse(LightColors); } }
        public bool HasOptions { get { return BodyColorList.Count > 0 || LightColorList.Count > 0; } }
    }
}
