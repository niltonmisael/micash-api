using MiCash.Domain.Entities;
using MiCash.Domain.Repositories.PasswordResets;

namespace CommonTestUtilities.Repositories
{
    /// <summary>List-backed password reset repository: changes made to returned entities are "saved" immediately.</summary>
    public sealed class InMemoryPasswordResetRepository :
        IPasswordResetReadOnlyRepository,
        IPasswordResetWriteOnlyRepository,
        IPasswordResetUpdateOnlyRepository
    {
        private long _nextId = 1;

        public List<PasswordResetRequest> Requests { get; } = [];

        /// <summary>Users attached to the requests' navigation property (as EF's Include would).</summary>
        public Dictionary<long , User> Users { get; } = [];

        public Task<int> CountCreatedSinceAsync ( long userId , DateTime since , CancellationToken cancellationToken ) =>
            Task.FromResult(Requests.Count(r => r.UserId == userId && r.CreatedAt >= since));

        public Task<DateTime?> GetLatestCreatedAtAsync ( long userId , CancellationToken cancellationToken ) =>
            Task.FromResult(Requests.Where(r => r.UserId == userId).Select(r => (DateTime?)r.CreatedAt).Max());

        public Task AddAsync ( PasswordResetRequest request , CancellationToken cancellationToken )
        {
            request.Id = _nextId++;
            Attach(request);
            Requests.Add(request);
            return Task.CompletedTask;
        }

        public Task<PasswordResetRequest?> GetByIdAsync ( long id , CancellationToken cancellationToken ) =>
            Task.FromResult(Requests.SingleOrDefault(r => r.Id == id));

        public Task<List<PasswordResetRequest>> GetOpenByUserAsync ( long userId , CancellationToken cancellationToken ) =>
            Task.FromResult(Requests.Where(r => r.UserId == userId && r.IsOpen).ToList());

        public Task<PasswordResetRequest?> GetPendingVerificationAsync ( long userId , CancellationToken cancellationToken ) =>
            Task.FromResult(Requests
                .Where(r => r.UserId == userId && r.IsOpen && r.VerifiedAt == null && r.CodeHash != null)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefault());

        public Task<PasswordResetRequest?> GetByResetTokenHashAsync ( string resetTokenHash , CancellationToken cancellationToken ) =>
            Task.FromResult(Requests.SingleOrDefault(r => r.ResetTokenHash == resetTokenHash));

        private void Attach ( PasswordResetRequest request )
        {
            if ( Users.TryGetValue(request.UserId , out var user) ) {
                request.User = user;
            }
        }
    }
}
