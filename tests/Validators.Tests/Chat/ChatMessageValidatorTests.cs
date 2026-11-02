using FluentAssertions;
using MiCash.Application.UseCases.FinancialChat;
using MiCash.Communication.Requests.Chat;
using MiCash.Exception;

namespace Validators.Tests.Chat
{
    public class ChatMessageValidatorTests
    {
        [Fact]
        public void Success ()
        {
            var result = new ValidatorChatMessage().Validate(new RequestChatMessageJson { Message = "Como economizar no mercado?" });

            result.IsValid.Should().BeTrue();
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        public void Error_Message_Empty ( string? message )
        {
            var result = new ValidatorChatMessage().Validate(new RequestChatMessageJson { Message = message! });

            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainSingle()
                .And.Contain(e => e.ErrorMessage == ResourceErrorMessages.CHAT_MESSAGE_REQUIRED);
        }

        [Fact]
        public void Error_Message_Too_Long ()
        {
            var message = new string('a' , ValidatorChatMessage.MAX_LENGTH + 1);

            var result = new ValidatorChatMessage().Validate(new RequestChatMessageJson { Message = message });

            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainSingle()
                .And.Contain(e => e.ErrorMessage == ResourceErrorMessages.CHAT_MESSAGE_TOO_LONG);
        }
    }
}
