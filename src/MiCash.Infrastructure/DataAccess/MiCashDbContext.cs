using MiCash.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MiCash.Infrastructure.DataAccess
{
    public class MiCashDbContext : DbContext
    {
        public MiCashDbContext ( DbContextOptions options ) : base( options ) 
        {
            
        }
        public DbSet<Expense> Expenses { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Category> Categories { get; set; }

        protected override void OnModelCreating ( ModelBuilder modelBuilder )
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<Tag>().ToTable("tags");
        }

    }
}
