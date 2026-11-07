
namespace Ghaforv3.Models
{
    using System;
    using System.Collections.Generic;

    public partial class Banner
    {
        public int Id { get; set; }
        public string Tag { get; set; }
        public string Title { get; set; }
        public string Highlight { get; set; }
        public string ButtonText { get; set; }
        public string LinkUrl { get; set; }
        public string ImageUrl { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
    }
}
