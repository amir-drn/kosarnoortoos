
namespace Ghaforv3.Models
{
    using System;
    using System.Collections.Generic;

    public partial class Payment
    {
        public int Id { get; set; }
        public int OrderId { get; set; }
        public decimal Amount { get; set; }
        public string Gateway { get; set; }
        public string RefNumber { get; set; }
        public bool IsSuccess { get; set; }
        public System.DateTime CreatedAt { get; set; }
    
        public virtual Order Order { get; set; }
    }
}
