using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BlockchainAutoPay.Models
{
    public class Customer
    {
        [Key]
        public int CustomerId { get; set; }
        public string CustomerName { get; set; }
        // [DataType(DataType.Date)]
        // public DateTime CreatedDate { get; set; }

        // placeholder; should NOT be type int
        public int Balance { get; set; }
        public string Email { get; set; }

        public IQueryable<Autopay> Autopayments { get; set; }
    }
}
