
namespace Ghaforv3.Models
{
    using System;
    using System.Collections.Generic;

    public partial class Address
    {
        public Address()
        {
            this.Orders = new HashSet<Order>();
        }
    
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Receiver { get; set; }
        public string Phone { get; set; }
        public string Province { get; set; }
        public string City { get; set; }
        public string PostalCode { get; set; }
        public string Details { get; set; }
    
        public virtual User User { get; set; }
        public virtual ICollection<Order> Orders { get; set; }
    }
}
