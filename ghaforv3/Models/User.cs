
namespace Ghaforv3.Models
{
    using System;
    using System.Collections.Generic;

    public partial class User
    {
        public User()
        {
            this.Addresses = new HashSet<Address>();
            this.Orders = new HashSet<Order>();
            this.Products = new HashSet<Product>();
        }
    
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Mobile { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public bool IsAdmin { get; set; }
        public System.DateTime CreatedAt { get; set; }
        public byte Role { get; set; }
        public bool IsActive { get; set; }
    
        public virtual ICollection<Address> Addresses { get; set; }
        public virtual ICollection<Order> Orders { get; set; }
        public virtual ICollection<Product> Products { get; set; }
    }
}
