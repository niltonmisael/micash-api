using MiCash.Domain.Entities;

namespace MiCash.Domain.Repositories.SavingsGoals
{
    /// <summary>A savings goal with its saved amount already aggregated by the database.</summary>
    public sealed record SavingsGoalWithBalance ( SavingsGoal Goal , decimal SavedAmount );

    public interface ISavingsGoalReadOnlyRepository
    {
        /// <summary>All goals of the user with their balances (no movements loaded).</summary>
        Task<IReadOnlyList<SavingsGoalWithBalance>> GetAllWithBalanceAsync (
            long userId ,
            CancellationToken cancellationToken );

        /// <summary>The goal with all its movements; null when missing or owned by another user.</summary>
        Task<SavingsGoal?> GetByIdWithMovementsAsync (
            long userId ,
            long id ,
            CancellationToken cancellationToken );

        /// <summary>Σ deposits − Σ withdrawals of the goal.</summary>
        Task<decimal> GetSavedAmountAsync (
            long savingsGoalId ,
            CancellationToken cancellationToken );
    }

    public interface ISavingsGoalWriteOnlyRepository
    {
        Task AddAsync ( SavingsGoal goal , CancellationToken cancellationToken );

        Task AddMovementAsync ( SavingsGoalMovement movement , CancellationToken cancellationToken );

        void Delete ( SavingsGoal goal );
    }

    public interface ISavingsGoalUpdateOnlyRepository
    {
        /// <summary>Tracked goal owned by the user (no movements); null when missing or owned by another user.</summary>
        Task<SavingsGoal?> GetByIdAsync (
            long userId ,
            long id ,
            CancellationToken cancellationToken );
    }
}
