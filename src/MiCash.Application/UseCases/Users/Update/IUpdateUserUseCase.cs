using MiCash.Communication.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiCash.Application.UseCases.Users.Update
{
    public interface IUpdateUserUseCase
    {
        Task Execute (RequestUpdateUserJson request);
    }
}
