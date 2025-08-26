using CommonTestUtilities.Requests;
using FluentAssertions;
using MiCash.Application.UseCases.Users.ChangePassword;
using MiCash.Exception;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Validators.Tests.Users.ChangePassword
{
    public class ChangePasswordValidatorTest
    {
        [Fact]
        public void Success ()
        {
            var validator = new ChangePasswordValidator();

            var request = RequestChangePasswordJsonBuilder.Build();

            var result = validator.Validate(request);

            result.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData("")]
        [InlineData("    ")]
        [InlineData(null)]
        public void Error_NewPassword_Empty ( string newPassword )
        {
            var validator = new ChangePasswordValidator();

            var request = RequestChangePasswordJsonBuilder.Build();
            request.NewPassword = newPassword;

            var result = validator.Validate(request);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainSingle().And.Contain(e => e.ErrorMessage.Equals(ResourseErrorMessages.INVALID_PASSWORD));
        }
    }
}
