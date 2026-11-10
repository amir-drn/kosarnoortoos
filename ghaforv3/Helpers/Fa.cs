using System;
using System.Globalization;
using System.Text;

namespace Ghaforv3.Helpers
{
    // نمایش فارسی: رقم‌های فارسی، قیمت با جداکننده، تاریخ شمسی
    public static class Fa
    {
        public static string Digits(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            var sb = new StringBuilder(s.Length);
            foreach (var ch in s)
                sb.Append(ch >= '0' && ch <= '9' ? (char)('\u06F0' + (ch - '0')) : ch);
            return sb.ToString();
        }

        public static string Num(long n) { return Digits(n.ToString("N0", CultureInfo.InvariantCulture).Replace(",", "\u066C")); }

        public static string Money(decimal d)
        {
            return Digits(decimal.Round(d, 0).ToString("N0", CultureInfo.InvariantCulture).Replace(",", "\u066C"));
        }

        private static readonly string[] Ones = { "", "یک", "دو", "سه", "چهار", "پنج", "شش", "هفت", "هشت", "نه", "ده", "یازده", "دوازده", "سیزده", "چهارده", "پانزده", "شانزده", "هفده", "هجده", "نوزده" };
        private static readonly string[] Tens = { "", "", "بیست", "سی", "چهل", "پنجاه", "شصت", "هفتاد", "هشتاد", "نود" };
        private static readonly string[] Hundreds = { "", "صد", "دویست", "سیصد", "چهارصد", "پانصد", "ششصد", "هفتصد", "هشتصد", "نهصد" };
        private static readonly string[] Scales = { "", "هزار", "میلیون", "میلیارد", "تریلیون" };

        private static string Three(int n)
        {
            var parts = new System.Collections.Generic.List<string>();
            if (n >= 100) { parts.Add(Hundreds[n / 100]); n %= 100; }
            if (n >= 20) { parts.Add(Tens[n / 10]); n %= 10; }
            if (n > 0) parts.Add(Ones[n]);
            return string.Join(" و ", parts);
        }

        // مبلغ به حروف: 1250000 → «یک میلیون و دویست و پنجاه هزار»
        public static string Words(long n)
        {
            if (n == 0) return "صفر";
            if (n < 0) return "منفی " + Words(-n);
            var groups = new System.Collections.Generic.List<string>();
            int scale = 0;
            while (n > 0)
            {
                if (scale >= Scales.Length) return "";
                int g = (int)(n % 1000);
                n /= 1000;
                if (g > 0) groups.Insert(0, Three(g) + (scale > 0 ? " " + Scales[scale] : ""));
                scale++;
            }
            return string.Join(" و ", groups);
        }

        public static string Date(DateTime d)
        {
            var pc = new PersianCalendar();
            return Digits(string.Format("{0:0000}/{1:00}/{2:00}", pc.GetYear(d), pc.GetMonth(d), pc.GetDayOfMonth(d)));
        }

        public static string Stamp(DateTime d)
        {
            return Date(d) + " " + Digits(d.ToString("HH:mm", CultureInfo.InvariantCulture));
        }
    }
}
