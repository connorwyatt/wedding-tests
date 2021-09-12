using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using ConnorWyatt.Wedding.Invitations.Client;
using ConnorWyatt.Wedding.Invitations.Models;
using FluentAssertions;
using Xunit;
using Xunit.Abstractions;

namespace ConnorWyatt.Wedding.Scripts
{
    public class CreateInvitations
    {
        private readonly ITestOutputHelper _testOutputHelper;
        private readonly HttpWeddingInvitationsClient _client;

        public CreateInvitations(ITestOutputHelper testOutputHelper)
        {
            _testOutputHelper = testOutputHelper;
            _client = new HttpWeddingInvitationsClient(new HttpClient
            {
                BaseAddress = Urls.WeddingInvitations,
            });
        }

        [Fact(Skip = "")]
        public async Task Create()
        {
            var definitions = new[]
            {
                new InvitationDefinition
                {
                    Type = InvitationType.FullDay,
                    AddressedTo = "Mum & Dad",
                    EmailAddress = "jo.goodenough@sky.com",
                    Invitees = new List<InviteeDefinition>
                    {
                        new()
                        {
                            Name = "Jo",
                            RequiresFood = true,
                        },
                        new()
                        {
                            Name = "John",
                            RequiresFood = true,
                        }
                    },
                    Code = "JoAndJohn",
                },
                new InvitationDefinition
                {
                    Type = InvitationType.FullDay,
                    AddressedTo = "Mum & Dad",
                    EmailAddress = "thecushioncorner@hotmail.co.uk",
                    Invitees = new List<InviteeDefinition>
                    {
                        new()
                        {
                            Name = "Sarah",
                            RequiresFood = true,
                        },
                        new()
                        {
                            Name = "Rick",
                            RequiresFood = true,
                        }
                    },
                    Code = "SarahAndRick",
                },
                new InvitationDefinition
                {
                    Type = InvitationType.FullDay,
                    AddressedTo = "Sam & Katie",
                    EmailAddress = "katie_lowe@btinternet.com",
                    Invitees = new List<InviteeDefinition>
                    {
                        new()
                        {
                            Name = "Sam",
                            RequiresFood = true,
                        },
                        new()
                        {
                            Name = "Katie",
                            RequiresFood = true,
                        }
                    },
                    Code = "SamAndKatie",
                },
            };

            foreach (var definition in definitions)
            {
                var result = await _client.CreateInvitation(definition);
                result.StatusCode.Should().BeOneOf(HttpStatusCode.Accepted, HttpStatusCode.OK, HttpStatusCode.Created);
                _testOutputHelper.WriteLine($"ID: {result.Value}, AddressedTo: {definition.AddressedTo}, Code: {definition.Code}");
            }
        }
    }
}
