using System.Text;

namespace Ghaforv3.Security
{
    public static class Digits
    {
        // ارقام فارسی و عربی را به انگلیسی تبدیل می‌کند و فاصله‌های اضافه را حذف می‌کند
        public static string ToEnglish(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            var sb = new StringBuilder(s.Length);
            foreach (var ch in s.Trim())
            {
                if (ch >= '\u06F0' && ch <= '\u06F9') sb.Append((char)('0' + (ch - '\u06F0')));
                else if (ch >= '\u0660' && ch <= '\u0669') sb.Append((char)('0' + (ch - '\u0660')));
                else sb.Append(ch);
            }
            return sb.ToString();
        }
    }
}
