using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Bogus;
using ConnorWyatt.Wedding.Invitations.Client;
using ConnorWyatt.Wedding.Invitations.Models;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Playwright;
using Xunit;

namespace ConnorWyatt.Wedding.Tests
{
    public class InvitationsTests : IAsyncDisposable
    {
        private readonly HttpWeddingInvitationsClient _invitationsClient;
        private readonly IPlaywright _playwright;
        private readonly IBrowser _browser;

        public InvitationsTests()
        {
            var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:8001") };
            _invitationsClient = new HttpWeddingInvitationsClient(httpClient);
            _playwright = Playwright.CreateAsync().Result;
            _browser = _playwright.Chromium.LaunchAsync(
                    new BrowserTypeLaunchOptions
                    {
                        // Headless = false,
                    })
                .Result;
        }

        [Theory]
        [InlineData(InvitationType.FullDay)]
        [InlineData(InvitationType.ReceptionOnly)]
        public async Task InvitationsCanBeCreatedForOnePerson(InvitationType invitationType)
        {
            var invitationId = await CreateInvitation(invitationType);
            invitationId.Should().NotBeNullOrEmpty();

            var invitation = await GetInvitation(invitationId);
            invitation.Should().NotBeNull();

            var invitationPage = await InvitationPage.NavigateTo(await _browser.NewPageAsync(), invitation.Code);

            using (new AssertionScope())
            {
                var invitationDescription = await (await invitationPage.GetInvitationDescription()).InnerTextAsync();
                invitationDescription.Should()
                    .Contain($"Would like to invite {invitation.AddressedTo} to join them to celebrate their marriage");

                var date = await (await invitationPage.GetDate()).InnerTextAsync();
                date.Should().Be("19.03.2022");

                var dayTimings = await (await invitationPage.GetDayTimings()).InnerTextAsync();
                if (invitationType == InvitationType.FullDay)
                {
                    dayTimings.Should().Be("4pm Ceremony, 7pm Reception");
                }
                else
                {
                    dayTimings.Should().Be("7pm Reception");
                    dayTimings.Should().NotContain("4pm Ceremony");
                }

                var location = await (await invitationPage.GetLocation()).InnerTextAsync();
                location.Should()
                    .Contain("Wootton Park")
                    .And.Contain("Wootton Wawen")
                    .And.Contain("Henley-in-Arden")
                    .And.Contain("B95 6HJ");

                var rsvpDate = await (await invitationPage.GetRsvpDate()).InnerTextAsync();
                rsvpDate.Should().Contain("RSVP by 01.02.2022");
            }
        }

        [Theory]
        [InlineData(InvitationType.FullDay, true, FoodOption.Standard, null, "My number is 0123456789")]
        [InlineData(InvitationType.FullDay, true, FoodOption.Vegetarian, "I'm Vegan.", null)]
        [InlineData(InvitationType.FullDay, false, null, null, null)]
        [InlineData(InvitationType.ReceptionOnly, true, FoodOption.Standard, null, "My number is 0123456789")]
        [InlineData(InvitationType.ReceptionOnly, true, FoodOption.Vegetarian, "I'm Vegan.", null)]
        [InlineData(InvitationType.ReceptionOnly, false, null, null, null)]
        public async Task InvitationsCanBeRespondedToForOnePerson(
            InvitationType invitationType,
            bool attending,
            FoodOption? foodOption,
            string? dietaryInformation,
            string? contactInformation)
        {
            var invitationId = await CreateInvitation(invitationType);
            invitationId.Should().NotBeNullOrEmpty();

            var invitation = await GetInvitation(invitationId);
            invitation.Should().NotBeNull();

            var page = await _browser.NewPageAsync();

            var invitationPage = await InvitationPage.NavigateTo(page, invitation.Code);

            var rsvpPage = await invitationPage.ClickRsvpLink();

            var inviteeRsvpFormSections = await rsvpPage.GetInviteeRsvpFormSections();

            inviteeRsvpFormSections.Should().HaveCount(1);

            var inviteeRsvpFormSection = inviteeRsvpFormSections.Single();

            await inviteeRsvpFormSection.SelectAttendingRadioOption(attending);
            if (foodOption.HasValue) await inviteeRsvpFormSection.SelectFoodOptionRadioOption(foodOption.Value);
            if (dietaryInformation is not null)
                await inviteeRsvpFormSection.EnterDietaryInformation(dietaryInformation);

            if (contactInformation is not null) await rsvpPage.EnterContactInformation(contactInformation);

            invitationPage = await rsvpPage.SubmitForm();

            var invitationBanner = await invitationPage.GetRsvpBanner();

            (await invitationBanner.InnerTextAsync()).Should().Be("Thanks for your RSVP");
            (await invitationPage.HasRsvpLink()).Should().BeFalse();

            invitation = await GetInvitation(invitationId);

            using (new AssertionScope())
            {
                invitation.Status.Should().Be(InvitationStatus.ResponseReceived);
                invitation.RespondedAt.Should().NotBeNull();
                invitation.ContactInformation.Should().Be(contactInformation);
                var invitee = invitation.Invitees.Single();
                invitee.Status.Should().Be(attending ? InviteeStatus.Attending : InviteeStatus.NotAttending);
                invitee.FoodOption.Should().Be(foodOption);
                invitee.DietaryNotes.Should().Be(dietaryInformation);
            }
        }

        private async Task<string> CreateInvitation(InvitationType invitationType)
        {
            var faker = new Faker();
            var personName = faker.Person.FullName;
            var createInvitationHttpResult = await _invitationsClient.CreateInvitation(
                new InvitationDefinition
                {
                    Code = Guid.NewGuid().ToString(),
                    Type = invitationType,
                    AddressedTo = personName,
                    EmailAddress = faker.Person.Email.ToLower(),
                    Invitees = new List<InviteeDefinition>
                    {
                        new() { Name = personName },
                    },
                });

            createInvitationHttpResult.StatusCode.Should().Be(HttpStatusCode.Accepted);

            return createInvitationHttpResult.Value;
        }

        private async Task<Invitation> GetInvitation(string invitationId)
        {
            var getInvitationResult = await _invitationsClient.GetInvitation(invitationId);

            getInvitationResult.StatusCode.Should().Be(HttpStatusCode.OK);

            return getInvitationResult.Value;
        }

        public async ValueTask DisposeAsync()
        {
            await _browser.DisposeAsync();
            _playwright.Dispose();
        }
    }
}
