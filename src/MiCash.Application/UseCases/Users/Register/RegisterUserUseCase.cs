using AutoMapper;
using FluentValidation.Results;
using MiCash.Communication.Requests;
using MiCash.Communication.Responses;
using MiCash.Domain.Repositories;
using MiCash.Domain.Repositories.Users;
using MiCash.Domain.Security.Cryptography;
using MiCash.Domain.Security.Tokens;
using MiCash.Domain.Services.Cache;
using MiCash.Domain.Services.Cache.Models;
using MiCash.Exception;
using MiCash.Exception.ExceptionBase;

namespace MiCash.Application.UseCases.Users.Register
{
    public class RegisterUserUseCase : IRegisterUserUseCase
    {
        private readonly IMapper _mapper;
        private readonly IPasswordEncripter _passwordEncripter;
        private readonly IUserReadOnlyRepository _userReadOnlyRepository;
        private readonly IUserWriteOnlyRepository _userWriteOnlyRepository;
        private readonly IUnitOfWork _unitOfWork; 
        private readonly IAccessTokenGenerator _tokenGenerator;
        private readonly IRefreshTokenGenerator _refreshTokenGenerator;
        private readonly IRedisCacheService _redisCacheService;

        public RegisterUserUseCase (
            IMapper mapper, 
            IPasswordEncripter passwordEncripter, 
            IUserReadOnlyRepository userReadOnlyRepository,
            IUserWriteOnlyRepository userWriteOnlyRepository, 
            IUnitOfWork unitOfWork, 
            IAccessTokenGenerator tokenGenerator,
            IRefreshTokenGenerator refreshTokenGenerator ,
            IRedisCacheService redisCacheService
        )
        {
            _mapper = mapper;
            _passwordEncripter = passwordEncripter;
            _userReadOnlyRepository = userReadOnlyRepository;
            _userWriteOnlyRepository = userWriteOnlyRepository;
            _unitOfWork = unitOfWork;  
            _tokenGenerator = tokenGenerator;
            _refreshTokenGenerator = refreshTokenGenerator;
            _redisCacheService = redisCacheService;
        }

        public async Task<ResponseRegisteredUserJson> Execute ( RequestRegisterUserJson request, CancellationToken cancellationToken )
        {
            await Validate(request);

            var user = _mapper.Map<Domain.Entities.User>(request);
            user.PasswordHash = _passwordEncripter.Encrypt(user.PasswordHash);
            user.UserIdentifier = Guid.NewGuid();

            await _userWriteOnlyRepository.Add(user);

            await _unitOfWork.Commit(cancellationToken);

            var token = _tokenGenerator.Generate(user);

            var refreshToken = _refreshTokenGenerator.Generate();

            var refreshTokenData = new RefreshTokenData
            {
                UserIdentifier = user.UserIdentifier,
                Token = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
            };

            await _redisCacheService.SetAsync(
                key: $"refreshToken:{refreshToken}" ,
                value: refreshTokenData,
                expiration: TimeSpan.FromDays(7)
            );

            return new ResponseRegisteredUserJson
            {
                Name = user.Name ,
                Token = token ,
                RefreshToken = refreshToken ,
            };
        }

        private async Task Validate (RequestRegisterUserJson request)
        {
            var result = new RegisterUserValidator().Validate(request);

            var emailExist = await _userReadOnlyRepository.ExistActiveUserWithEmail(request.Email);

            if ( emailExist ) {
                result.Errors.Add(new ValidationFailure(string.Empty , ResourceErrorMessages.EMAIL_ALREADY_REGISTERED));
            }

            if ( result.IsValid == false ) {
                var errorMessages = result.Errors.Select(f => f.ErrorMessage).ToList();

                throw new ErrorOnValidationException(errorMessages);
            }
        }
    }
}
