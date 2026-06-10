using MiCash.Domain.Entities;
using MiCash.Domain.Enums;
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
        public DbSet<Income> Incomes { get; set; }
        public DbSet<Plan> Plans { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<PaymentLink> PaymentLinks { get; set; }
        public DbSet<ProcessedWebhook> ProcessedWebhooks { get; set; }


        protected override void OnModelCreating ( ModelBuilder modelBuilder )
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(MiCashDbContext).Assembly);
        }

        protected override void ConfigureConventions ( ModelConfigurationBuilder configurationBuilder )
        {
            configurationBuilder.Properties<ExpenseStatus>() // Se quiser configurar para todos enums do projeto, use <Enum>() ao invés de <ExpenseStatus>()
                .HaveConversion<string>();
        }



    }
}
