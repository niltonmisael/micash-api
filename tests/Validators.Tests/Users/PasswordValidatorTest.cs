using FluentAssertions;
using FluentValidation;
using MiCash.Application.UseCases.Users;
using MiCash.Communication.Requests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Validators.Tests.Users
{
    public class PasswordValidatorTest
    {
        [Theory]
        [InlineData("")]
        [InlineData("      ")]
        [InlineData(null)]
        [InlineData("a")]
        [InlineData("aa")]
        [InlineData("aaa")]
        [InlineData("aaaa")]
        [InlineData("aaaaa")]
        [InlineData("aaaaaa")]
        [InlineData("aaaaaaa")]
        [InlineData("aaaaaaaa")]
        [InlineData("Aaaaaaaa")]
        [InlineData("Aaaaaaa1")]
        public void Error_Password_Invalid ( string password )
        {
            var validator = new PasswordValidator<RequestRegisterUserJson>();

            var result = validator
                .IsValid(new ValidationContext<RequestRegisterUserJson>(new RequestRegisterUserJson()) , password);

            result.Should().BeFalse();
        }
    }
}
