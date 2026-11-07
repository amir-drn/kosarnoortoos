
namespace Ghaforv3.Models
{
    using System;
    using System.Collections.Generic;

    public partial class Product
    {
        public Product()
        {
            this.OrderItems = new HashSet<OrderItem>();
            this.ProductImages = new HashSet<ProductImage>();
            this.Users = new HashSet<User>();
        }
    
        public int Id { get; set; }
        public int CategoryId { get; set; }
        public Nullable<int> BrandId { get; set; }
        public string Title { get; set; }
        public string Slug { get; set; }
        public string Description { get; set; }
        public decimal Price { get; set; }
        public Nullable<decimal> OldPrice { get; set; }
        public string Unit { get; set; }
        public int Stock { get; set; }
        public string ImageUrl { get; set; }
        public bool IsActive { get; set; }
        public bool IsDailyOffer { get; set; }
        public bool IsFlashSale { get; set; }
        public Nullable<System.DateTime> OfferEndsAt { get; set; }
        public System.DateTime CreatedAt { get; set; }
    
        public virtual Brand Brand { get; set; }
        public virtual Category Category { get; set; }
        public virtual ICollection<OrderItem> OrderItems { get; set; }
        public virtual ICollection<ProductImage> ProductImages { get; set; }
        public virtual ICollection<User> Users { get; set; }
    }
}
