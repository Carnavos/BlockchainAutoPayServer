using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BlockchainAutoPay.Models
{
    public class Autopay
    {
        [Key]
        public int AutopayId { get; set; }
        public int CustomerId { get; set; }
        public IQueryable<Transaction> Transactions { get; set; }
        // should not be int; placeholder
        public int Frequency { get; set; }
        public DateTime InitialPaymentDate { get; set; }
    }
}
