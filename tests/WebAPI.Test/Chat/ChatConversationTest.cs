using FluentAssertions;
using MiCash.Communication.Requests.Chat;
using System.Net;
using System.Text.Json;

namespace WebAPI.Test.Chat
{
    public class ChatConversationTest : MiCashClassFixture
    {
        private const string MESSAGE = "api/Chat/message";
        private const string MESSAGES = "api/Chat/messages";
        private const string CONVERSATIONS = "api/Chat/conversations";

        private readonly string _token;
        private readonly string _tokenWithoutSubscription;

        public ChatConversationTest ( CustomWebApplicationFactory webApplicationFactory ) : base(webApplicationFactory)
        {
            _token = webApplicationFactory.GetToken();
            _tokenWithoutSubscription = webApplicationFactory.GetTokenWithoutSubscription();
        }

        [Fact]
        public async Task History_New_Conversation_And_Pagination ()
        {
            // Never chatted: empty history, no conversation is created just by opening the screen.
            var empty = await ReadJson(await DoGet(MESSAGES , _token));
            empty.GetProperty("conversationId").ValueKind.Should().Be(JsonValueKind.Null);
            empty.GetProperty("messages").GetArrayLength().Should().Be(0);
            empty.GetProperty("hasMore").GetBoolean().Should().BeFalse();

            // Two exchanges => 4 messages (user, assistant, user, assistant).
            var first = await ReadJson(await DoPost(MESSAGE , new RequestChatMessageJson { Message = "Primeira pergunta" } , _token));
            await DoPost(MESSAGE , new RequestChatMessageJson { Message = "Segunda pergunta" } , _token);

            first.GetProperty("id").GetInt64().Should().BeGreaterThan(0);
            first.GetProperty("userMessageId").GetInt64().Should().BeGreaterThan(0);
            var conversationId = first.GetProperty("conversationId").GetInt64();

            var all = await ReadJson(await DoGet(MESSAGES , _token));
            all.GetProperty("conversationId").GetInt64().Should().Be(conversationId);

            var messages = all.GetProperty("messages").EnumerateArray().ToList();
            messages.Select(m => m.GetProperty("role").GetString())
                .Should().Equal("user" , "assistant" , "user" , "assistant");
            messages[0].GetProperty("content").GetString().Should().Be("Primeira pergunta");
            messages.Select(m => m.GetProperty("id").GetInt64()).Should().BeInAscendingOrder();

            // Cursor pagination: newest 3, then the remaining older one.
            var page1 = await ReadJson(await DoGet($"{MESSAGES}?limit=3" , _token));
            page1.GetProperty("messages").GetArrayLength().Should().Be(3);
            page1.GetProperty("hasMore").GetBoolean().Should().BeTrue();

            var oldestId = page1.GetProperty("messages")[0].GetProperty("id").GetInt64();
            var page2 = await ReadJson(await DoGet($"{MESSAGES}?limit=3&before={oldestId}" , _token));
            page2.GetProperty("messages").GetArrayLength().Should().Be(1);
            page2.GetProperty("hasMore").GetBoolean().Should().BeFalse();
            page2.GetProperty("messages")[0].GetProperty("content").GetString().Should().Be("Primeira pergunta");

            // New conversation: previous one is closed and the history starts empty.
            var started = await DoPost(CONVERSATIONS , new { } , _token);
            started.StatusCode.Should().Be(HttpStatusCode.Created);
            var newConversationId = (await ReadJson(started)).GetProperty("conversationId").GetInt64();
            newConversationId.Should().NotBe(conversationId);

            var afterNew = await ReadJson(await DoGet(MESSAGES , _token));
            afterNew.GetProperty("conversationId").GetInt64().Should().Be(newConversationId);
            afterNew.GetProperty("messages").GetArrayLength().Should().Be(0);

            // Next message goes to the new conversation.
            var next = await ReadJson(await DoPost(MESSAGE , new RequestChatMessageJson { Message = "Oi de novo" } , _token));
            next.GetProperty("conversationId").GetInt64().Should().Be(newConversationId);
        }

        [Fact]
        public async Task Without_Subscription_Cannot_Send_Or_Start_But_Can_Read ()
        {
            var send = await DoPost(MESSAGE , new RequestChatMessageJson { Message = "Oi" } , _tokenWithoutSubscription);
            send.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            (await ReadJson(send)).GetProperty("errorMessages").GetArrayLength().Should().Be(1);

            var start = await DoPost(CONVERSATIONS , new { } , _tokenWithoutSubscription);
            start.StatusCode.Should().Be(HttpStatusCode.Forbidden);

            var read = await DoGet(MESSAGES , _tokenWithoutSubscription);
            read.StatusCode.Should().Be(HttpStatusCode.OK);
        }

        [Fact]
        public async Task History_Without_Token ()
        {
            var result = await DoGet(MESSAGES , token: string.Empty);

            result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        private static async Task<JsonElement> ReadJson ( HttpResponseMessage response )
        {
            var body = await response.Content.ReadAsStringAsync();

            return JsonDocument.Parse(body).RootElement.Clone();
        }
    }
}
