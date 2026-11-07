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
