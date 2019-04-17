using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Tests.Integ.Framework;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Xunit;

namespace Telegram.Bot.Tests.Integ.Polls
{
    [Collection(Constants.TestCollections.NativePolls)]
    [TestCaseOrderer(Constants.TestCaseOrderer, Constants.AssemblyName)]
    public class PollMessageTests : IClassFixture<PollTestsFixture>
    {
        private ITelegramBotClient BotClient => _fixture.BotClient;

        private readonly PollTestsFixture _classFixture;
        private readonly TestsFixture _fixture;

        public PollMessageTests(TestsFixture fixture, PollTestsFixture classFixture)
        {
            _fixture = fixture;
            _classFixture = classFixture;
        }

        [OrderedFact(FactTitles.ShouldSendPoll)]
        [Trait(Constants.MethodTraitName, Constants.TelegramBotApiMethods.SendPoll)]
        public async Task Should_Send_Poll()
        {
            Message message = await BotClient.SendPollAsync(
                /* chatId: */ _fixture.SupergroupChat,
                /* question: */ "Who shot first?",
                /* options: */ new[] { "Han Solo", "Greedo", "I don't care" }
            );

            Assert.Equal(MessageType.Poll, message.Type);
            Assert.NotEmpty(message.Poll.Id);
            Assert.False(message.Poll.IsClosed);

            Assert.Equal("Who shot first?", message.Poll.Question);
            Assert.Equal(3, message.Poll.Options.Length);
            Assert.Equal("Han Solo", message.Poll.Options[0].Text);
            Assert.Equal("Greedo", message.Poll.Options[1].Text);
            Assert.Equal("I don't care", message.Poll.Options[2].Text);

            Assert.All(message.Poll.Options, option => Assert.Equal(0, option.VoterCount));

            _classFixture.PollMessage = message;
        }

        [OrderedFact(FactTitles.ShouldReceivePollStateUpdate)]
        public async Task Should_Receive_Poll_State_Update()
        {
            await _fixture.SendTestCaseNotificationAsync(
                "Any member of the test supergroup should vote in the poll"
            );

            Update update = (await _fixture.UpdateReceiver
                .GetUpdatesAsync(allowAnyone: true, updateTypes: UpdateType.Poll))
                .First();

            Poll poll = update.Poll;

            Assert.Equal(_classFixture.PollMessage.Poll.Id, poll.Id);
            Assert.False(poll.IsClosed);
        }

        [OrderedFact(FactTitles.ShouldStopPoll)]
        [Trait(Constants.MethodTraitName, Constants.TelegramBotApiMethods.StopPoll)]
        public async Task Should_Stop_Poll()
        {
            Poll poll = await BotClient.StopPollAsync(
                /* chatId: */ _classFixture.PollMessage.Chat,
                /* messageId: */ _classFixture.PollMessage.MessageId
            );

            Assert.Equal(_classFixture.PollMessage.Poll.Id, poll.Id);
            Assert.True(poll.IsClosed);
        }

        [OrderedFact(FactTitles.ShouldThrowExceptionNotEnoughOptions)]
        [Trait(Constants.MethodTraitName, Constants.TelegramBotApiMethods.SendPoll)]
        public async Task Should_Throw_Exception_Not_Enough_Options()
        {
            ApiRequestException exception = await Assert.ThrowsAnyAsync<ApiRequestException>(() =>
                BotClient.SendPollAsync(
                    /* chatId: */ _fixture.SupergroupChat,
                    /* question: */ "You should never see this poll",
                    /* options: */ new[] { "The only poll option" }
                )
            );

            Assert.Equal("Bad Request: poll must have at least 2 option", exception.Message);
        }

        private static class FactTitles
        {
            public const string ShouldSendPoll = "Should send a poll";

            public const string ShouldReceivePollStateUpdate = "Should poll state update";

            public const string ShouldStopPoll = "Should stop the poll";

            public const string ShouldThrowExceptionNotEnoughOptions = "Should throw exception due to not having enough poll options";
        }
    }
}
