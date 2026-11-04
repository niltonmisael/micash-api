namespace MiCash.Domain.Repositories
{
    public interface IUnitOfWork
    {
        Task Commit(CancellationToken cancellationToken);

        /// <summary>Drops every pending (not yet committed) change of this unit of work.</summary>
        void DiscardChanges ();
    }
}
