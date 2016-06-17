using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlockchainAutoPay.Models
{
    public class BCAPContext : DbContext
    {
        public BCAPContext(DbContextOptions<BCAPContext> options)
            : base(options)
        { }

        public DbSet<Customer> Customer { get; set; }
        public DbSet<CurrentCustomer> CurrentCustomer { get; set; }
        public DbSet<Autopay> Autopay { get; set; }
        public DbSet<Transaction> Transaction { get; set; }
    }
}
