using FluentAssertions;
using MiCash.Communication.Requests;
using MiCash.Communication.Requests.PasswordReset;
using MiCash.Domain.Services.Jobs;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace WebAPI.Test.PasswordReset
{
    /// <summary>
    /// The whole flow over HTTP: forgot-password → (Hangfire job, run here by hand) → e-mail → verify → reset → login.
    /// E-mails go to the factory's fake; nothing is sent.
    /// </summary>
    public class PasswordResetFlowTest : MiCashClassFixture
    {
        private const string NEW_PASSWORD = "NovaSenha1!";

        private readonly CustomWebApplicationFactory _factory;

        public PasswordResetFlowTest ( CustomWebApplicationFactory factory ) : base(factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Forgot_Verify_Reset_And_Login_With_The_New_Password ()
        {
            var email = _factory.GetEmail();

            // 1. Request: 202 with the flow rules, the e-mail is queued (not sent in the request).
            var forgot = await DoPost("forgot-password" , new RequestForgotPasswordJson { Email = email });
            forgot.StatusCode.Should().Be(HttpStatusCode.Accepted);
            var rules = await ReadJson(forgot);
            rules.GetProperty("resendAvailableInSeconds").GetInt32().Should().Be(60);
            rules.GetProperty("codeExpiresInMinutes").GetInt32().Should().Be(15);
            rules.GetProperty("codeLength").GetInt32().Should().Be(6);

            var requestId = _factory.BackgroundJobs.EnqueuedPasswordResetEmails.Should().ContainSingle().Subject;

            // 2. The job generates the code and sends the recovery template.
            await using ( var scope = _factory.Services.CreateAsyncScope() ) {
                await scope.ServiceProvider.GetRequiredService<IPasswordResetEmailJob>().ExecuteAsync(requestId , CancellationToken.None);
            }

            var sent = _factory.Emails.Sent.Should().ContainSingle(m => m.To == email).Subject;
            sent.Subject.Should().Be("Recuperação de senha · MiCash");
            var code = Regex.Match(sent.TextBody! , @"Seu código: (\d{6})").Groups[1].Value;
            code.Should().HaveLength(6);
            sent.HtmlBody.Should().Contain(code);

            // A wrong code is refused with the generic message.
            var wrong = await DoPost("verify-password-reset-code" , new RequestVerifyPasswordResetCodeJson { Email = email , Code = code == "000000" ? "111111" : "000000" });
            wrong.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            (await ReadJson(wrong)).GetProperty("errorMessages")[0].GetString()
                .Should().Be("Código inválido ou expirado. Confira o código ou solicite um novo.");

            // 3. Right code: a reset token, and no session tokens.
            var verify = await DoPost("verify-password-reset-code" , new RequestVerifyPasswordResetCodeJson { Email = email , Code = code });
            verify.StatusCode.Should().Be(HttpStatusCode.OK);
            var verified = await ReadJson(verify);
            var resetToken = verified.GetProperty("resetToken").GetString()!;
            verified.TryGetProperty("token" , out _).Should().BeFalse("verifying the code must not sign the user in");
            verified.TryGetProperty("refreshToken" , out _).Should().BeFalse();

            // 4. New password.
            var reset = await DoPost("reset-password" , new RequestResetPasswordJson { ResetToken = resetToken , NewPassword = NEW_PASSWORD });
            reset.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // The token is single use.
            var again = await DoPost("reset-password" , new RequestResetPasswordJson { ResetToken = resetToken , NewPassword = "OutraSenha2!" });
            again.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            // 5. Login: new password works, the old one does not.
            (await DoPost("login" , new RequestLoginJson { Email = email , Password = NEW_PASSWORD }))
                .StatusCode.Should().Be(HttpStatusCode.OK);
            (await DoPost("login" , new RequestLoginJson { Email = email , Password = _factory.GetPassword() }))
                .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Unknown_Email_Gets_The_Same_Answer_And_No_Email ()
        {
            var response = await DoPost("forgot-password" , new RequestForgotPasswordJson { Email = "ninguem@micash.com.br" });

            response.StatusCode.Should().Be(HttpStatusCode.Accepted);
            (await ReadJson(response)).GetProperty("resendAvailableInSeconds").GetInt32().Should().Be(60);
            _factory.Emails.Sent.Should().NotContain(m => m.To == "ninguem@micash.com.br");
        }

        [Fact]
        public async Task Invalid_Input_Is_400_In_Portuguese ()
        {
            var forgot = await DoPost("forgot-password" , new RequestForgotPasswordJson { Email = "nao-e-email" });
            forgot.StatusCode.Should().Be(HttpStatusCode.BadRequest);

            var verify = await DoPost("verify-password-reset-code" , new RequestVerifyPasswordResetCodeJson { Email = "a@b.com" , Code = "12" });
            (await ReadJson(verify)).GetProperty("errorMessages")[0].GetString()
                .Should().Be("Informe o código de 6 dígitos enviado por e-mail.");

            var reset = await DoPost("reset-password" , new RequestResetPasswordJson { ResetToken = "x" , NewPassword = "fraca" });
            reset.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        private static async Task<JsonElement> ReadJson ( HttpResponseMessage response ) =>
            JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement.Clone();
    }

    /// <summary>Own class (own factory), so exhausting the per-IP limit does not affect other tests.</summary>
    public class PasswordResetRateLimitTest : MiCashClassFixture
    {
        public PasswordResetRateLimitTest ( CustomWebApplicationFactory factory ) : base(factory) { }

        [Fact]
        public async Task Too_Many_Requests_From_The_Same_Client_Get_429 ()
        {
            HttpResponseMessage last = null!;

            for ( var i = 0; i <= MiCash.API.RateLimiting.PasswordResetRateLimit.PERMIT_LIMIT; i++ ) {
                last = await DoPost("forgot-password" , new RequestForgotPasswordJson { Email = $"pessoa{i}@micash.com.br" });
            }

            last.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
            var body = JsonDocument.Parse(await last.Content.ReadAsStringAsync()).RootElement;
            body.GetProperty("errorMessages")[0].GetString()
                .Should().Be("Muitas tentativas em pouco tempo. Aguarde alguns minutos e tente novamente.");
        }
    }
}
