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
    public class UserUpdateOnlyRepositoryBuilder
    {
        public static IUserUpdateOnlyRepository Build ( User user )
        {
            var mock = new Mock<IUserUpdateOnlyRepository>();

            mock.Setup(repository => repository.GetById(user.Id)).ReturnsAsync(user);

            return mock.Object;
        }
    }
}
