using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BlockchainAutoPay.Models
{
    public class Transaction
    {
        [Key]
        public int TransactionId { get; set; }
        // should not be double; placeholder
        public double Amount { get; set; }
        public DateTime Date { get; set; }
        public int PayerId { get; set; }
        public int PayeeId { get; set; }
        public string Note { get; set; }
        // may not be a string
        public string Confirmation { get; set; }
        public int AutopayId { get; set; }
    }
}
