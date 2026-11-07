using System;
using System.Globalization;

public static class DisplayHelpers
{
    // تبدیل اعداد انگلیسی به فارسی
    public static string ToPersianDigits(this string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;
        return input.Replace('0', '۰')
                    .Replace('1', '۱')
                    .Replace('2', '۲')
                    .Replace('3', '۳')
                    .Replace('4', '۴')
                    .Replace('5', '۵')
                    .Replace('6', '۶')
                    .Replace('7', '۷')
                    .Replace('8', '۸')
                    .Replace('9', '۹');
    }

    // فرمت استاندارد قیمت تومان
    public static string ToTomanFormat(this decimal price)
    {
        string formatted = price.ToString("N0", CultureInfo.InvariantCulture); // جداکننده سه رقمی
        return formatted.ToPersianDigits() + " تومان";
    }
}