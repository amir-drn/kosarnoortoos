using System.Collections.Generic;

namespace Ghaforv3.Models
{
    public class ProductDetailsViewModel
    {
        public Product Product { get; set; }
        public List<Product> Related { get; set; }   // محصولات هم‌دسته
    }
}
