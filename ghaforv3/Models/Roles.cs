namespace Ghaforv3.Models
{
    public enum UserRole : byte { User = 0, Admin = 1, SuperAdmin = 2 }

    public static class AdminLabels
    {
        public static string Role(byte r) { return r == 2 ? "سوپر ادمین" : r == 1 ? "ادمین" : "کاربر عادی"; }
        public static string Order(byte s)
        {
            switch (s)
            {
                case 0: return "در انتظار پرداخت";
                case 1: return "پرداخت‌شده";
                case 2: return "ارسال‌شده";
                case 3: return "تحویل‌شده";
                case 4: return "لغوشده";
                default: return "نامشخص";
            }
        }
    }
}
