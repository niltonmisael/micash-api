using MiCash.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiCash.Domain.Repositories.Users
{
    public interface IUserUpdateOnlyRepository
    {
        Task<User> GetById(long id);
        void Update(User user);
    }
}
