using System.Collections.Generic;

namespace Ghaforv3.Models
{
    public class CategoryCard
    {
        public string Title { get; set; }
        public string Slug { get; set; }
        public string Icon { get; set; }
        public int Count { get; set; }
    }

    public class HomeViewModel
    {
        public Banner Banner { get; set; }
        public List<CategoryCard> Categories { get; set; }
        public Product ProductOfDay { get; set; }
        public List<Product> FlashSale { get; set; }
    }
}
