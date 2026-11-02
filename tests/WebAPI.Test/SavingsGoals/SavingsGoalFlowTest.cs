using FluentAssertions;
using MiCash.Communication.Requests.SavingsGoals;
using MiCash.Domain.Services.Clock;
using System.Net;
using System.Text.Json;

namespace WebAPI.Test.SavingsGoals
{
    public class SavingsGoalFlowTest : MiCashClassFixture
    {
        private const string METHOD = "api/savings-goal";

        private readonly string _token;
        private readonly string _otherUserToken;
        private readonly DateOnly _today = new BrazilClock(TimeProvider.System).Today;

        public SavingsGoalFlowTest ( CustomWebApplicationFactory webApplicationFactory ) : base(webApplicationFactory)
        {
            _token = webApplicationFactory.GetToken();
            _otherUserToken = webApplicationFactory.GetTokenWithoutSubscription();
        }

        [Fact]
        public async Task Full_Flow ()
        {
            // No goals yet.
            (await DoGet(METHOD , _token)).StatusCode.Should().Be(HttpStatusCode.NoContent);

            // Deadline = last day of the month two months ahead => 3 months left (current one included).
            var deadlineMonth = new DateOnly(_today.Year , _today.Month , 1).AddMonths(2);
            var deadline = deadlineMonth.AddMonths(1).AddDays(-1);

            var created = await DoPost(METHOD , new RequestRegisterSavingsGoalJson
            {
                Name = "Viagem" ,
                TargetAmount = 5000 ,
                Deadline = deadline ,
                InitialAmount = 3200 ,
                Icon = "plane" ,
                Color = "#F2803A"
            } , _token);

            created.StatusCode.Should().Be(HttpStatusCode.Created);
            var id = (await ReadJson(created)).GetProperty("id").GetInt64();

            // A second goal without deadline, already completed by its initial amount.
            await DoPost(METHOD , new RequestRegisterSavingsGoalJson
            {
                Name = "Reserva" ,
                TargetAmount = 100 ,
                InitialAmount = 100 ,
                Icon = "pig-money" ,
                Color = "#2E7D32"
            } , _token);

            // Detail: "já guardado" is in the history and the prototype-29 numbers come out.
            var detail = await ReadJson(await DoGet($"{METHOD}/{id}" , _token));
            detail.GetProperty("savedAmount").GetDecimal().Should().Be(3200);
            detail.GetProperty("remainingAmount").GetDecimal().Should().Be(1800);
            detail.GetProperty("percentage").GetDecimal().Should().Be(64);
            detail.GetProperty("monthlyAmountNeeded").GetDecimal().Should().Be(600);
            detail.GetProperty("isCompleted").GetBoolean().Should().BeFalse();
            detail.GetProperty("deadline").GetString().Should().Be(deadline.ToString("yyyy-MM-dd"));
            detail.GetProperty("movements").GetArrayLength().Should().Be(1);
            detail.GetProperty("movements")[0].GetProperty("type").GetInt32().Should().Be(0);
            detail.GetProperty("movements")[0].GetProperty("date").GetString().Should().Be(_today.ToString("yyyy-MM-dd"));

            // Withdrawal above the balance is rejected; within the balance it works.
            var tooMuch = await DoPost($"{METHOD}/{id}/withdraw" , new RequestSavingsMovementJson { Amount = 3200.01m , Date = _today } , _token);
            tooMuch.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            (await DoPost($"{METHOD}/{id}/withdraw" , new RequestSavingsMovementJson { Amount = 200 , Date = _today } , _token))
                .StatusCode.Should().Be(HttpStatusCode.Created);

            // Deposit completes the goal (5000).
            var deposit = await DoPost($"{METHOD}/{id}/deposit" , new RequestSavingsMovementJson { Amount = 2000 , Date = _today } , _token);
            deposit.StatusCode.Should().Be(HttpStatusCode.Created);
            (await ReadJson(deposit)).GetProperty("id").GetInt64().Should().BeGreaterThan(0);

            var completed = await ReadJson(await DoGet($"{METHOD}/{id}" , _token));
            completed.GetProperty("savedAmount").GetDecimal().Should().Be(5000);
            completed.GetProperty("isCompleted").GetBoolean().Should().BeTrue();
            completed.GetProperty("monthlyAmountNeeded").ValueKind.Should().Be(JsonValueKind.Null);
            completed.GetProperty("movements").GetArrayLength().Should().Be(3);

            // Future deposit date is rejected.
            (await DoPost($"{METHOD}/{id}/deposit" , new RequestSavingsMovementJson { Amount = 1 , Date = _today.AddDays(1) } , _token))
                .StatusCode.Should().Be(HttpStatusCode.BadRequest);

            // Update: raise the target => in progress again.
            var update = await DoPut($"{METHOD}/{id}" , new RequestUpdateSavingsGoalJson
            {
                Name = "Viagem para Portugal" ,
                TargetAmount = 10000 ,
                Deadline = deadline ,
                Icon = "plane" ,
                Color = "#F2803A"
            } , _token);
            update.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // List: in progress (with deadline) first, completed last, with aggregated balances.
            var list = (await ReadJson(await DoGet(METHOD , _token))).GetProperty("savingsGoals");
            list.GetArrayLength().Should().Be(2);
            list[0].GetProperty("name").GetString().Should().Be("Viagem para Portugal");
            list[0].GetProperty("savedAmount").GetDecimal().Should().Be(5000);
            list[0].GetProperty("isCompleted").GetBoolean().Should().BeFalse();
            list[1].GetProperty("name").GetString().Should().Be("Reserva");
            list[1].GetProperty("isCompleted").GetBoolean().Should().BeTrue();
            list[1].GetProperty("deadline").ValueKind.Should().Be(JsonValueKind.Null);

            // Another user cannot see, change, move or delete it.
            (await DoGet($"{METHOD}/{id}" , _otherUserToken)).StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await DoPost($"{METHOD}/{id}/deposit" , new RequestSavingsMovementJson { Amount = 1 , Date = _today } , _otherUserToken))
                .StatusCode.Should().Be(HttpStatusCode.NotFound);
            (await DoDelete($"{METHOD}/{id}" , _otherUserToken)).StatusCode.Should().Be(HttpStatusCode.NotFound);

            // Delete (movements go in cascade).
            (await DoDelete($"{METHOD}/{id}" , _token)).StatusCode.Should().Be(HttpStatusCode.NoContent);
            (await DoGet($"{METHOD}/{id}" , _token)).StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Register_Validation_Error_In_Portuguese ()
        {
            var result = await DoPost(METHOD , new RequestRegisterSavingsGoalJson
            {
                Name = "" ,
                TargetAmount = 0 ,
                Icon = "plane" ,
                Color = "#F2803A"
            } , _token);

            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var errors = (await ReadJson(result)).GetProperty("errorMessages").EnumerateArray().Select(e => e.GetString()).ToList();
            errors.Should().Contain("O valor da meta deve ser maior que zero, com no máximo 2 casas decimais.");
            errors.Should().HaveCount(2);
        }

        [Fact]
        public async Task Without_Token ()
        {
            (await DoGet(METHOD , token: string.Empty)).StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        private static async Task<JsonElement> ReadJson ( HttpResponseMessage response )
        {
            var body = await response.Content.ReadAsStringAsync();

            return JsonDocument.Parse(body).RootElement.Clone();
        }
    }
}
