using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Ghaforv3.Models
{
    public class RegisterVm
    {
        [Required(ErrorMessage = "نام را وارد کن"), StringLength(100), Display(Name = "نام و نام خانوادگی")]
        public string FullName { get; set; }

        [Required(ErrorMessage = "شمارهٔ موبایل را وارد کن"), RegularExpression(@"^09\d{9}$", ErrorMessage = "شمارهٔ موبایل معتبر نیست (مثل 09123456789)"), Display(Name = "شمارهٔ موبایل")]
        public string Mobile { get; set; }

        [EmailAddress(ErrorMessage = "ایمیل معتبر نیست"), StringLength(150), Display(Name = "ایمیل (اختیاری)")]
        public string Email { get; set; }

        [Required(ErrorMessage = "رمز را وارد کن"), StringLength(100, MinimumLength = 8, ErrorMessage = "رمز باید بین ۸ تا ۱۰۰ کاراکتر باشد"), DataType(DataType.Password), Display(Name = "رمز عبور")]
        public string Password { get; set; }

        [Compare("Password", ErrorMessage = "تکرار رمز با رمز یکسان نیست"), DataType(DataType.Password), Display(Name = "تکرار رمز عبور")]
        public string ConfirmPassword { get; set; }
    }

    public class AccountVm
    {
        public string FullName { get; set; }
        public string Mobile { get; set; }
        public string Email { get; set; }
        public bool IsStaff { get; set; }
        public List<OrderRow> Orders { get; set; }
    }
}
