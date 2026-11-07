using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace Ghaforv3.Models
{
    // ---------- ورود ----------
    public class LoginVm
    {
        [Required(ErrorMessage = "شمارهٔ موبایل را وارد کن"), StringLength(30), Display(Name = "شمارهٔ موبایل")]
        public string Mobile { get; set; }
        [Required(ErrorMessage = "رمز عبور را وارد کن"), StringLength(200), DataType(DataType.Password), Display(Name = "رمز عبور")]
        public string Password { get; set; }
        [Display(Name = "مرا به خاطر بسپار")]
        public bool Remember { get; set; }
    }

    public class SetupVm
    {
        [Required(ErrorMessage = "نام را وارد کن"), StringLength(100), Display(Name = "نام و نام خانوادگی")]
        public string FullName { get; set; }
        [Required(ErrorMessage = "موبایل را وارد کن"), RegularExpression(@"^09\d{9}$", ErrorMessage = "شمارهٔ موبایل معتبر نیست (مثل 09123456789)"), Display(Name = "شمارهٔ موبایل")]
        public string Mobile { get; set; }
        [Required(ErrorMessage = "رمز را وارد کن"), StringLength(100, MinimumLength = 8, ErrorMessage = "رمز باید بین ۸ تا ۱۰۰ کاراکتر باشد"), DataType(DataType.Password), Display(Name = "رمز عبور")]
        public string Password { get; set; }
        [DataType(DataType.Password), Display(Name = "کلید راه‌اندازی")]
        public string SetupKey { get; set; }
    }

    // ---------- لیست‌ها ----------
    public interface IPaged { int Page { get; } int Pages { get; } string Q { get; } int? Status { get; } }

    public class PagedVm<T> : IPaged
    {
        public List<T> Items { get; set; }
        public int Page { get; set; }
        public int Pages { get; set; }
        public int Total { get; set; }
        public string Q { get; set; }
        public int? Status { get; set; }
    }

    public class ProductRow
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Slug { get; set; }
        public string CategoryTitle { get; set; }
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public bool IsActive { get; set; }
        public string ImageUrl { get; set; }
    }

    public class CategoryRow
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Slug { get; set; }
        public string Icon { get; set; }
        public bool IsActive { get; set; }
        public int Count { get; set; }
    }

    public class UserRow
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Mobile { get; set; }
        public byte Role { get; set; }
        public bool IsActive { get; set; }
    }

    public class OrderRow
    {
        public int Id { get; set; }
        public string Customer { get; set; }
        public decimal Total { get; set; }
        public byte Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class OrderItemRow { public string Title { get; set; } public decimal UnitPrice { get; set; } public int Quantity { get; set; } }

    public class OrderDetailsVm
    {
        public System.Collections.Generic.List<string> PaymentLines { get; set; }
        public bool RefundNeeded { get; set; }
        public int Id { get; set; }
        public string Customer { get; set; }
        public string Mobile { get; set; }
        public string Receiver { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public byte Status { get; set; }
        public decimal Total { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<OrderItemRow> Items { get; set; }
    }

    public class DashboardVm
    {
        public int Products { get; set; }
        public int Categories { get; set; }
        public int Customers { get; set; }
        public int Orders { get; set; }
        public int NewOrders { get; set; }
        public decimal Revenue { get; set; }
        public List<ProductRow> LowStock { get; set; }
        public List<OrderRow> Latest { get; set; }
    }

    // ---------- فرم‌ها ----------
    public class ProductFormVm
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "عنوان را وارد کن"), StringLength(200), Display(Name = "عنوان")]
        public string Title { get; set; }
        [Required(ErrorMessage = "Slug را وارد کن"), StringLength(220), RegularExpression("^[a-z0-9-]+$", ErrorMessage = "فقط حروف کوچک انگلیسی، عدد و خط تیره"), Display(Name = "Slug (آدرس لاتین)")]
        public string Slug { get; set; }
        [Required(ErrorMessage = "دسته را انتخاب کن"), Display(Name = "دسته‌بندی")]
        public int? CategoryId { get; set; }
        [Display(Name = "برند")]
        public int? BrandId { get; set; }
        [Required(ErrorMessage = "قیمت را وارد کن"), Range(0, 999999999999, ErrorMessage = "قیمت معتبر نیست"), Display(Name = "قیمت (تومان)")]
        public decimal? Price { get; set; }
        [Range(0, 999999999999, ErrorMessage = "قیمت معتبر نیست"), Display(Name = "قیمت قبل از تخفیف")]
        public decimal? OldPrice { get; set; }
        [StringLength(30), Display(Name = "واحد")]
        public string Unit { get; set; }
        [Range(0, 10000000, ErrorMessage = "موجودی معتبر نیست"), Display(Name = "موجودی")]
        public int Stock { get; set; }
        [StringLength(4000), Display(Name = "توضیحات")]
        public string Description { get; set; }
        [Display(Name = "فعال")] public bool IsActive { get; set; }
        [Display(Name = "پیشنهاد امروز")] public bool IsDailyOffer { get; set; }
        [Display(Name = "حراج ویژه")] public bool IsFlashSale { get; set; }
        [Display(Name = "پایان پیشنهاد")] public DateTime? OfferEndsAt { get; set; }
        public string ImageUrl { get; set; }
        public IEnumerable<SelectListItem> Categories { get; set; }
        public IEnumerable<SelectListItem> Brands { get; set; }
    }

    public class CategoryFormVm
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "عنوان را وارد کن"), StringLength(100), Display(Name = "عنوان")]
        public string Title { get; set; }
        [Required(ErrorMessage = "Slug را وارد کن"), StringLength(120), RegularExpression("^[a-z0-9-]+$", ErrorMessage = "فقط حروف کوچک انگلیسی، عدد و خط تیره"), Display(Name = "Slug (آدرس لاتین)")]
        public string Slug { get; set; }
        [StringLength(200), Display(Name = "آیکن (ایموجی یا آدرس عکس)")] public string Icon { get; set; }
        [Display(Name = "ترتیب نمایش")] public int SortOrder { get; set; }
        [Display(Name = "فعال")] public bool IsActive { get; set; }
        [Display(Name = "دستهٔ والد")] public int? ParentId { get; set; }
        public IEnumerable<SelectListItem> Parents { get; set; }
    }

    public class UserFormVm
    {
        public int Id { get; set; }
        [Required(ErrorMessage = "نام را وارد کن"), StringLength(100), Display(Name = "نام و نام خانوادگی")]
        public string FullName { get; set; }
        [Required(ErrorMessage = "موبایل را وارد کن"), RegularExpression(@"^09\d{9}$", ErrorMessage = "شمارهٔ موبایل معتبر نیست (مثل 09123456789)"), Display(Name = "شمارهٔ موبایل")]
        public string Mobile { get; set; }
        [EmailAddress(ErrorMessage = "ایمیل معتبر نیست"), StringLength(150), Display(Name = "ایمیل")]
        public string Email { get; set; }
        [Display(Name = "نقش")] public byte Role { get; set; }
        [Display(Name = "حساب فعال است")] public bool IsActive { get; set; }
        [DataType(DataType.Password), Display(Name = "رمز عبور (هنگام ویرایش، خالی = بدون تغییر)")]
        public string Password { get; set; }
        public IEnumerable<SelectListItem> Roles { get; set; }
    }
}
