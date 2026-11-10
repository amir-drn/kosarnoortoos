
namespace Ghaforv3.Models
{
    using System;
    using System.Collections.Generic;

    public partial class OrderItem
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public string Title { get; set; }
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public string BodyColor { get; set; }            // رنگ بدنهٔ انتخاب‌شده توسط مشتری
        public string LightColor { get; set; }           // رنگ نور انتخاب‌شده توسط مشتری
    
        public virtual Order Order { get; set; }
        public virtual Product Product { get; set; }
    }
}
