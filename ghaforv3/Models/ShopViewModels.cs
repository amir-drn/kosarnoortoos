using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Ghaforv3.Models
{
    // ---------- آدرس و پرداخت ----------
    public class AddressForm
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "نام گیرنده را وارد کن"), StringLength(100), Display(Name = "نام و نام خانوادگی گیرنده")]
        public string Receiver { get; set; }
        [Required(ErrorMessage = "شمارهٔ تماس را وارد کن"), RegularExpression("^0[0-9\u06F0-\u06F9]{10}$", ErrorMessage = "شماره معتبر نیست (مثل 09123456789)"), Display(Name = "شمارهٔ تماس گیرنده")]
        public string Phone { get; set; }
        [Required(ErrorMessage = "استان را وارد کن"), StringLength(50), Display(Name = "استان")]
        public string Province { get; set; }
        [Required(ErrorMessage = "شهر را وارد کن"), StringLength(50), Display(Name = "شهر")]
        public string City { get; set; }
        [RegularExpression("^[0-9\u06F0-\u06F9]{10}$", ErrorMessage = "کد پستی باید ۱۰ رقم باشد"), Display(Name = "کد پستی")]
        public string PostalCode { get; set; }
        [Required(ErrorMessage = "نشانی کامل را وارد کن"), StringLength(400), Display(Name = "نشانی کامل")]
        public string Details { get; set; }
    }

    public class CheckoutVm
    {
        public Cart Cart { get; set; }
        public List<Address> Addresses { get; set; }
        public int AddressId { get; set; }
        public AddressForm NewAddress { get; set; }
        public decimal Subtotal { get; set; }
        public decimal Shipping { get; set; }
        public decimal Total { get; set; }
    }

    // ---------- حساب کاربری ----------
    public class ProfileVm
    {
        [Required(ErrorMessage = "نام را وارد کن"), StringLength(100), Display(Name = "نام و نام خانوادگی")]
        public string FullName { get; set; }
        [EmailAddress(ErrorMessage = "ایمیل معتبر نیست"), StringLength(150), Display(Name = "ایمیل")]
        public string Email { get; set; }
        public string Mobile { get; set; }
    }

    public class PasswordVm
    {
        [Required(ErrorMessage = "رمز فعلی را وارد کن"), StringLength(200), DataType(DataType.Password), Display(Name = "رمز فعلی")]
        public string Current { get; set; }
        [Required(ErrorMessage = "رمز جدید را وارد کن"), StringLength(100, MinimumLength = 8, ErrorMessage = "رمز باید بین ۸ تا ۱۰۰ کاراکتر باشد"), DataType(DataType.Password), Display(Name = "رمز جدید")]
        public string New { get; set; }
        [Compare("New", ErrorMessage = "تکرار رمز با رمز جدید یکسان نیست"), DataType(DataType.Password), Display(Name = "تکرار رمز جدید")]
        public string Confirm { get; set; }
    }

    // ---------- مدیریت ----------
    public class BannerFormVm
    {
        public int Id { get; set; }
        [StringLength(100), Display(Name = "برچسب کوچک بالای تیتر")] public string Tag { get; set; }
        [Required(ErrorMessage = "تیتر را وارد کن"), StringLength(200), Display(Name = "تیتر")] public string Title { get; set; }
        [StringLength(200), Display(Name = "تیتر درخشان (خط دوم)")] public string Highlight { get; set; }
        [StringLength(50), Display(Name = "متن دکمه")] public string ButtonText { get; set; }
        [StringLength(300), Display(Name = "لینک دکمه (مثل /shop/led)")] public string LinkUrl { get; set; }
        [Display(Name = "ترتیب نمایش")] public int SortOrder { get; set; }
        [Display(Name = "فعال")] public bool IsActive { get; set; }
        public string ImageUrl { get; set; }
    }

    public class BrandFormVm
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "عنوان را وارد کن"), StringLength(100), Display(Name = "عنوان برند")] public string Title { get; set; }
        [Required(ErrorMessage = "Slug را وارد کن"), StringLength(120), RegularExpression("^[a-z0-9-]+$", ErrorMessage = "فقط حروف کوچک انگلیسی، عدد و خط تیره"), Display(Name = "Slug (آدرس لاتین)")] public string Slug { get; set; }
    }

    public class ProductImagesVm
    {
        public int ProductId { get; set; }
        public string Title { get; set; }
        public string MainImageUrl { get; set; }
        public List<ProductImage> Images { get; set; }
    }
}
