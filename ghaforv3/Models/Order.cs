
namespace Ghaforv3.Models
{
    using System;
    using System.Collections.Generic;

    public partial class Order
    {
        public Order()
        {
            this.OrderItems = new HashSet<OrderItem>();
            this.Payments = new HashSet<Payment>();
        }
    
        public int Id { get; set; }
        public int UserId { get; set; }
        public int AddressId { get; set; }
        public byte Status { get; set; }
        public decimal TotalAmount { get; set; }
        public System.DateTime CreatedAt { get; set; }
    
        public virtual Address Address { get; set; }
        public virtual ICollection<OrderItem> OrderItems { get; set; }
        public virtual User User { get; set; }
        public virtual ICollection<Payment> Payments { get; set; }
    }
}
