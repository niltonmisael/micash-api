using FluentAssertions;
using MiCash.Communication.Requests.FinancialGoals;
using System.Net;
using System.Text.Json;

namespace WebAPI.Test.FinancialGoals
{
    public class FinancialGoalFlowTest : MiCashClassFixture
    {
        private const string METHOD = "api/financial-goal";

        private readonly string _token;
        private readonly long _categoryId;
        private readonly DateTime _expenseDate;

        public FinancialGoalFlowTest ( CustomWebApplicationFactory webApplicationFactory ) : base(webApplicationFactory)
        {
            _token = webApplicationFactory.GetToken();
            _categoryId = webApplicationFactory.GetCategoryId();
            _expenseDate = webApplicationFactory.GetDate();
        }

        [Fact]
        public async Task Error_Without_Token ()
        {
            var result = await DoGet($"{METHOD}?month=1&year=2026" , token: string.Empty);

            result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Full_Flow ()
        {
            // The seeded expense (150) belongs to the seeded category in the expense's month.
            var month = _expenseDate.Month;
            var year = _expenseDate.Year;

            // Register: 150 of 180 spent => 83.33% => Warning
            var register = await DoPost(METHOD , new RequestRegisterFinancialGoal
            {
                CategoryId = _categoryId ,
                TargetAmount = 180 ,
                Month = month ,
                Year = year
            } , _token);

            register.StatusCode.Should().Be(HttpStatusCode.Created);
            var goalId = (await ReadJson(register)).GetProperty("id").GetInt64();

            // Duplicate
            var duplicated = await DoPost(METHOD , new RequestRegisterFinancialGoal
            {
                CategoryId = _categoryId ,
                TargetAmount = 50 ,
                Month = month ,
                Year = year
            } , _token);

            duplicated.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            // Get by period
            var byPeriod = await DoGet($"{METHOD}?month={month}&year={year}" , _token);
            byPeriod.StatusCode.Should().Be(HttpStatusCode.OK);
            (await ReadJson(byPeriod)).GetProperty("financialGoals").GetArrayLength().Should().Be(1);

            // Get by id
            var byId = await DoGet($"{METHOD}/{goalId}" , _token);
            byId.StatusCode.Should().Be(HttpStatusCode.OK);
            (await ReadJson(byId)).GetProperty("categoryName").GetString().Should().Be("Moradia");

            // Progress
            var progress = await DoGet($"{METHOD}/progress?month={month}&year={year}" , _token);
            progress.StatusCode.Should().Be(HttpStatusCode.OK);
            var progressGoal = (await ReadJson(progress)).GetProperty("goals")[0];
            progressGoal.GetProperty("currentAmount").GetDecimal().Should().Be(150);
            progressGoal.GetProperty("statusDescription").GetString().Should().Be("Warning");

            // Alerts
            var alerts = await DoGet($"{METHOD}/alerts?month={month}&year={year}" , _token);
            alerts.StatusCode.Should().Be(HttpStatusCode.OK);
            (await ReadJson(alerts)).GetProperty("alerts").GetArrayLength().Should().Be(1);

            // Update => 150 of 1000 => Normal, so the alert must disappear (cache invalidated)
            var update = await DoPut($"{METHOD}/{goalId}" , new RequestUpdateFinancialGoalJson { TargetAmount = 1000 } , _token);
            update.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var alertsAfterUpdate = await DoGet($"{METHOD}/alerts?month={month}&year={year}" , _token);
            (await ReadJson(alertsAfterUpdate)).GetProperty("alerts").GetArrayLength().Should().Be(0);

            // Copy to the next month
            var next = new DateTime(year , month , 1).AddMonths(1);
            var copy = await DoPost($"{METHOD}/copy" , new RequestCopyFinancialGoalsJson
            {
                SourceMonth = month ,
                SourceYear = year ,
                TargetMonth = next.Month ,
                TargetYear = next.Year
            } , _token);
            copy.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var copied = await DoGet($"{METHOD}?month={next.Month}&year={next.Year}" , _token);
            var copiedGoal = (await ReadJson(copied)).GetProperty("financialGoals")[0];
            copiedGoal.GetProperty("targetAmount").GetDecimal().Should().Be(1000);

            // History contains both months
            var history = await DoGet($"{METHOD}/history?page=1&pageSize=10" , _token);
            history.StatusCode.Should().Be(HttpStatusCode.OK);
            (await ReadJson(history)).GetProperty("totalRecords").GetInt32().Should().Be(2);

            // Delete
            var delete = await DoDelete($"{METHOD}/{goalId}" , _token);
            delete.StatusCode.Should().Be(HttpStatusCode.NoContent);

            var afterDelete = await DoGet($"{METHOD}/{goalId}" , _token);
            afterDelete.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task Error_Invalid_Period ()
        {
            var result = await DoGet($"{METHOD}/progress?month=13&year=2026" , _token);

            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        private static async Task<JsonElement> ReadJson ( HttpResponseMessage response )
        {
            var body = await response.Content.ReadAsStringAsync();

            return JsonDocument.Parse(body).RootElement;
        }
    }
}
