using MiCash.Communication.Requests;
using MiCash.Communication.Responses;
using MiCash.Domain.Repositories.Users;
using MiCash.Domain.Security.Cryptography;
using MiCash.Domain.Security.Tokens;
using MiCash.Exception.ExceptionBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MiCash.Application.UseCases.Login
{
    public class DoLoginUseCase : IDoLoginUseCase
    {
        private readonly IUserReadOnlyRepository _repository;
        private readonly IPasswordEncripter _passwordEncryption;
        private readonly IAccessTokenGenerator _accessTokenGenerator;

        public DoLoginUseCase (IUserReadOnlyRepository repository, IPasswordEncripter passwordEncripter, IAccessTokenGenerator accessTokenGenerator)
        {
            _repository = repository;
            _passwordEncryption = passwordEncripter;
            _accessTokenGenerator = accessTokenGenerator;
        }
        public async Task<ResponseRegisteredUserJson> Execute ( RequestLoginJson request )
        {
            
            var user = await _repository.GetUserByEmail ( request.Email );

            if ( user == null ) {

                throw new InvalidLoginException();
            }

            var passwordMatch = _passwordEncryption.Verify(request.Password, user.Password);

            if ( passwordMatch == false ) {
                throw new InvalidLoginException();
            }

            return new ResponseRegisteredUserJson
            {
                Name = user.Name ,
                Token = _accessTokenGenerator.Generate(user)
            };
             
        }
    }
}
