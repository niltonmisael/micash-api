namespace MiCash.Domain.Repositories.Users
{
    public interface IUserWriteOnlyRepository
    {
        Task Add(Entities.User user);
        Task Delete(Entities.User user);

        /// <summary>
        /// Marks for deletion up to <paramref name="batchSize"/> accounts never verified and created before
        /// <paramref name="createdBefore"/>; saved by <see cref="IUnitOfWork.Commit"/>. Returns how many.
        /// </summary>
        Task<int> RemoveUnverifiedCreatedBeforeAsync ( DateTime createdBefore , int batchSize , CancellationToken cancellationToken );
    }
}
