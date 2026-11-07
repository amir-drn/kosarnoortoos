using System.Collections.Generic;

namespace Ghaforv3.Models
{
    public class ProductListViewModel
    {
        public List<Product> Products { get; set; }
        public List<CategoryCard> Categories { get; set; }
        public string Category { get; set; }        // slug دستهٔ انتخاب‌شده
        public string CategoryTitle { get; set; }
        public string Q { get; set; }               // عبارت جستجو
        public string Sort { get; set; }            // newest | cheap | expensive
        public int Page { get; set; }
        public int TotalPages { get; set; }
        public int TotalCount { get; set; }
        public List<Brand> Brands { get; set; }
        public string Brand { get; set; }           // slug برند انتخاب‌شده
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public bool InStock { get; set; }
    }
}
