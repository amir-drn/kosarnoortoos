
namespace Ghaforv3.Models
{
    using System;
    using System.Collections.Generic;

    public partial class Brand
    {
        public Brand()
        {
            this.Products = new HashSet<Product>();
        }
    
        public int Id { get; set; }
        public string Title { get; set; }
        public string Slug { get; set; }
    
        public virtual ICollection<Product> Products { get; set; }
    }
}
