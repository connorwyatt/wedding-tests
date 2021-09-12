using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using ConnorWyatt.Wedding.Invitations.Client;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace ConnorWyatt.Wedding.Scripts
{
    public class SendEmails
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly HttpWeddingInvitationsClient _client;

        public SendEmails(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            _client = new HttpWeddingInvitationsClient(new HttpClient
            {
                BaseAddress = Urls.WeddingInvitations,
            });
        }

        [Fact(Skip = "")]
        public async Task Send()
        {
            var result = await _client.GetInvitations();
            result.StatusCode.Should().Be(HttpStatusCode.OK);

            foreach (var invitation in result.Value)
            {
                if (invitation.EmailSent || invitation.EmailAddress is null)
                {
                    return;
                }

                var sendEmailResponse = await _client.SendEmail(invitation.Id);

                sendEmailResponse.StatusCode.Should().Be(HttpStatusCode.Accepted);

                _testOutputHelper.WriteLine($"Email sent for {invitation.AddressedTo}");
            }
        }
    }
}
