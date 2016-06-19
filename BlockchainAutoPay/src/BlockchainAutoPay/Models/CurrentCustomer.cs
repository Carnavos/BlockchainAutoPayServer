using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace BlockchainAutoPay.Models
{
    public class CurrentCustomer
    {
        // local id to be used within app tracking
        // public int BCAPId { get; set; }
        // Coinbase id (alphanumeric combo) to be used for API calls
        [Key]
        public string CustomerId { get; set; }
        public string FullName { get; set; }
        public string ProfilePicUrl { get; internal set; }
        public string Data { get; set; }
        public string AccessToken { get; set; }
    }
}
