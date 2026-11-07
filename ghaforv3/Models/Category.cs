
namespace Ghaforv3.Models
{
    using System;
    using System.Collections.Generic;

    public partial class Category
    {
        public Category()
        {
            this.Categories1 = new HashSet<Category>();
            this.Products = new HashSet<Product>();
        }
    
        public int Id { get; set; }
        public Nullable<int> ParentId { get; set; }
        public string Title { get; set; }
        public string Slug { get; set; }
        public string Icon { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
    
        public virtual ICollection<Category> Categories1 { get; set; }
        public virtual Category Category1 { get; set; }
        public virtual ICollection<Product> Products { get; set; }
    }
}
