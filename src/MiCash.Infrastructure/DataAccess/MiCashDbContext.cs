using MiCash.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Npgsql.Replication.PgOutput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiCash.Infrastructure.DataAccess
{
    public class MiCashDbContext : DbContext
    {
        public MiCashDbContext ( DbContextOptions options ) : base( options ) 
        {
            
        }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating ( ModelBuilder modelBuilder )
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Tag>().ToTable("Tags");
        }

    }
}
