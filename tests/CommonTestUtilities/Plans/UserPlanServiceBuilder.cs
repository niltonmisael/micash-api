using MiCash.Domain.Entities;
using MiCash.Domain.Services.Plans;
using Moq;

namespace CommonTestUtilities.Plans
{
    public static class UserPlanServiceBuilder
    {
        /// <summary>Every user on the given plan (Premium by default: no limits besides the chat rate).</summary>
        public static IUserPlanService Build ( PlanLimits? limits = null )
        {
            var mock = new Mock<IUserPlanService>();

            mock.Setup(service => service.GetLimitsAsync(It.IsAny<User>() , It.IsAny<CancellationToken>()))
                .ReturnsAsync(limits ?? PlanLimits.Premium);

            return mock.Object;
        }

        public static IUserPlanService Free () => Build(PlanLimits.Free);
    }
}
