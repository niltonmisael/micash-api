using MiCash.Domain.Entities;
using MiCash.Domain.Security.Tokens;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CommonTestUtilities.Token
{
    public class JwtTokenGeneratorBuilder
    {
        public static IAccessTokenGenerator Build ()
        {
            var mock = new Mock<IAccessTokenGenerator>();

            mock.Setup(accessTokenGenerator => accessTokenGenerator.Generate(It.IsAny<User>())).Returns("token");

            return mock.Object;
        }

    }
}
