using MiCash.Domain.Security.Tokens;
using Moq;

namespace CommonTestUtilities.Token
{
    public static class RefreshTokenGeneratorBuilder
    {
        public static IRefreshTokenGenerator Build ()
        {
            var mock = new Mock<IRefreshTokenGenerator>();
            mock.Setup(refreshTokenGenerator => refreshTokenGenerator.Generate()).Returns("d290f1ee-6c54-4b01-90e6-d701748f0851");
            return mock.Object;
        }
    }
}
