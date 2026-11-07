using System;
using System.Security.Cryptography;

namespace Ghaforv3.Security
{
    // رمز هرگز ذخیره نمی‌شود؛ فقط هش با PBKDF2 ذخیره می‌شود.
    // فرمت جدید:  v2$تعداد-تکرار$نمک$هش   (SHA-256)
    // فرمت قدیمی: نمک.هش  (SHA-1 با ۱۰ هزار تکرار) هنوز خوانده می‌شود و بعد از اولین ورود موفق به فرمت جدید تبدیل می‌شود.
    public static class PasswordHasher
    {
        private const int Iterations = 120000;
        private const int LegacyIterations = 10000;
        private const string Prefix = "v2$";

        public static string Hash(string password)
        {
            var salt = new byte[16];
            using (var rng = new RNGCryptoServiceProvider()) rng.GetBytes(salt);
            byte[] hash;
            using (var kdf = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
                hash = kdf.GetBytes(32);
            return Prefix + Iterations + "$" + Convert.ToBase64String(salt) + "$" + Convert.ToBase64String(hash);
        }

        public static bool Verify(string password, string stored)
        {
            if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(stored)) return false;
            try
            {
                if (stored.StartsWith(Prefix, StringComparison.Ordinal))
                {
                    var p = stored.Split('$');
                    int iter;
                    if (p.Length != 4 || !int.TryParse(p[1], out iter) || iter < 10000 || iter > 5000000) return false;
                    var salt = Convert.FromBase64String(p[2]);
                    var expected = Convert.FromBase64String(p[3]);
                    byte[] actual;
                    using (var kdf = new Rfc2898DeriveBytes(password, salt, iter, HashAlgorithmName.SHA256))
                        actual = kdf.GetBytes(expected.Length);
                    return FixedEquals(expected, actual);
                }

                var parts = stored.Split('.');
                if (parts.Length != 2) return false;
                var lsalt = Convert.FromBase64String(parts[0]);
                var lexpected = Convert.FromBase64String(parts[1]);
                byte[] lactual;
                using (var kdf = new Rfc2898DeriveBytes(password, lsalt, LegacyIterations))
                    lactual = kdf.GetBytes(lexpected.Length);
                return FixedEquals(lexpected, lactual);
            }
            catch (FormatException) { return false; }
        }

        // اگر هش قدیمی باشد true برمی‌گرداند تا بعد از ورود موفق دوباره هش شود
        public static bool NeedsRehash(string stored)
        {
            return string.IsNullOrEmpty(stored) || !stored.StartsWith(Prefix + Iterations + "$", StringComparison.Ordinal);
        }

        // مقایسهٔ هم‌زمان (بدون وابستگی به محل اولین تفاوت)
        private static bool FixedEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;
            int diff = 0;
            for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }

        // برای مقایسهٔ رشته‌ها (مثل کلید راه‌اندازی) بدون نشت زمان
        public static bool SecureEquals(string x, string y)
        {
            if (x == null || y == null) return false;
            var a = System.Text.Encoding.UTF8.GetBytes(x);
            var b = System.Text.Encoding.UTF8.GetBytes(y);
            int diff = a.Length ^ b.Length;
            for (int i = 0; i < a.Length && i < b.Length; i++) diff |= a[i] ^ b[i];
            return diff == 0;
        }
    }
}
