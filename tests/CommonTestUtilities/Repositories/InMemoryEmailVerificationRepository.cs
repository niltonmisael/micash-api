using MiCash.Domain.Entities;
using MiCash.Domain.Enums;
using MiCash.Domain.Repositories.EmailVerifications;

namespace CommonTestUtilities.Repositories
{
    /// <summary>List-backed e-mail verification repository: changes made to returned entities are "saved" immediately.</summary>
    public sealed class InMemoryEmailVerificationRepository :
        IEmailVerificationReadOnlyRepository,
        IEmailVerificationWriteOnlyRepository,
        IEmailVerificationUpdateOnlyRepository
    {
        private long _nextId = 1;

        public List<EmailVerificationRequest> Requests { get; } = [];

        /// <summary>Users attached to the requests' navigation property (as EF's Include would).</summary>
        public Dictionary<long , User> Users { get; } = [];

        public Task<int> CountCreatedSinceAsync ( long userId , DateTime since , CancellationToken cancellationToken ) =>
            Task.FromResult(Requests.Count(r => r.UserId == userId && r.CreatedAt >= since));

        public Task<DateTime?> GetLatestCreatedAtAsync ( long userId , EmailVerificationPurpose purpose , CancellationToken cancellationToken ) =>
            Task.FromResult(Requests.Where(r => r.UserId == userId && r.Purpose == purpose).Select(r => (DateTime?)r.CreatedAt).Max());

        public Task AddAsync ( EmailVerificationRequest request , CancellationToken cancellationToken )
        {
            request.Id = _nextId++;

            if ( request.User is not null ) {
                request.UserId = request.User.Id;
            } else if ( Users.TryGetValue(request.UserId , out var user) ) {
                request.User = user;
            }

            Requests.Add(request);
            return Task.CompletedTask;
        }

        public Task<EmailVerificationRequest?> GetByIdAsync ( long id , CancellationToken cancellationToken ) =>
            Task.FromResult(Requests.SingleOrDefault(r => r.Id == id));

        public Task<List<EmailVerificationRequest>> GetOpenByUserAsync ( long userId , CancellationToken cancellationToken ) =>
            Task.FromResult(Requests.Where(r => r.UserId == userId && r.IsOpen).ToList());

        public Task<EmailVerificationRequest?> GetPendingVerificationAsync ( long userId , EmailVerificationPurpose purpose , CancellationToken cancellationToken ) =>
            Task.FromResult(Requests
                .Where(r => r.UserId == userId && r.Purpose == purpose && r.IsOpen && r.CodeHash != null)
                .OrderByDescending(r => r.CreatedAt)
                .FirstOrDefault());
    }
}
