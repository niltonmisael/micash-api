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
        public DbSet<Income> Incomes { get; set; }
        public DbSet<Plan> Plans { get; set; }
        public DbSet<Subscription> Subscriptions { get; set; }
        public DbSet<PaymentLink> PaymentLinks { get; set; }
        public DbSet<ProcessedWebhook> ProcessedWebhooks { get; set; }
        public DbSet<FinancialGoal> FinancialGoals { get; set; }
        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<ConversationMessage> ConversationMessages { get; set; }
        public DbSet<SavingsGoal> SavingsGoals { get; set; }
        public DbSet<SavingsGoalMovement> SavingsGoalMovements { get; set; }


        protected override void OnModelCreating ( ModelBuilder modelBuilder )
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(MiCashDbContext).Assembly);
        }

        protected override void ConfigureConventions ( ModelConfigurationBuilder configurationBuilder )
        {
            configurationBuilder.Properties<Enum>()
                .HaveConversion<string>();
        }
    }
}
