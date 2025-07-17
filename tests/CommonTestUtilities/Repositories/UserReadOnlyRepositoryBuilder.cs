using MiCash.Domain.Entities;
using MiCash.Domain.Repositories.Users;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonTestUtilities.Repositories
{
    public class UserReadOnlyRepositoryBuilder
    {
        private readonly Mock<IUserReadOnlyRepository> _repository;

        public UserReadOnlyRepositoryBuilder ()
        {
            _repository = new Mock<IUserReadOnlyRepository> ();
        }

        public void ExistActiveUserWithEmail (string email)
        {
            _repository.Setup(userReadOnly => userReadOnly.ExistActiveUserWithEmail(email)).ReturnsAsync(true);
        }

        public UserReadOnlyRepositoryBuilder GetUserByEmail ( User user )
        {
            _repository.Setup(userRepository => userRepository.GetUserByEmail(user.Email)).ReturnsAsync(user);

            return this;

        }

        public IUserReadOnlyRepository Build() => _repository.Object;
    }
}
