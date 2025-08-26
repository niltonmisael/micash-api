using MiCash.Communication.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiCash.Application.UseCases.Users.ChangePassword
{
    public interface IChangePasswordUseCase
    {
        Task Execute ( RequestChangePasswordJson request );
    }
}
