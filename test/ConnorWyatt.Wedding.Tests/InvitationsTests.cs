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
            var httpClient = new HttpClient { BaseAddress = Urls.WeddingInvitations };
            _invitationsClient = new HttpWeddingInvitationsClient(httpClient);
            _playwright = Playwright.CreateAsync().Result;
            _browser = _playwright.Chromium.LaunchAsync(
                    new BrowserTypeLaunchOptions
                    {
                        Headless = false,
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
                    .Contain($"Would like to invite {invitation.AddressedTo} to join them in celebration of their marriage".ToUpper());

                var date = await (await invitationPage.GetDate()).InnerTextAsync();
                date.Should().Be("19.03.2022".ToUpper());

                var dayTimings = await (await invitationPage.GetDayTimings()).InnerTextAsync();
                if (invitationType == InvitationType.FullDay)
                {
                    dayTimings.Should().Be("4pm Ceremony, 7pm Reception".ToUpper());
                }
                else
                {
                    dayTimings.Should().Be("7pm Reception".ToUpper());
                    dayTimings.Should().NotContain("4pm Ceremony".ToUpper());
                }

                var location = await (await invitationPage.GetLocation()).InnerTextAsync();
                location.Should()
                    .Contain("Wootton Park".ToUpper())
                    .And.Contain("Wootton Wawen".ToUpper())
                    .And.Contain("Henley-in-Arden".ToUpper())
                    .And.Contain("B95 6HJ".ToUpper());

                var rsvpDate = await (await invitationPage.GetRsvpDate()).InnerTextAsync();
                rsvpDate.Should().Contain("by 01.01.2022".ToUpper());
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

            (await invitationBanner.InnerTextAsync()).Should().Be("Thanks for your RSVP".ToUpper());
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

        [Fact]
        public async Task InvitationsCanBeRespondedToForAPersonWhoDoesNotRequireFood()
        {
            var invitationId = await CreateInvitationForPersonWhoDoesNotRequireFood();
            invitationId.Should().NotBeNullOrEmpty();

            var invitation = await GetInvitation(invitationId);
            invitation.Should().NotBeNull();

            var invitee = invitation.Invitees.Single();

            var page = await _browser.NewPageAsync();

            var invitationPage = await InvitationPage.NavigateTo(page, invitation.Code);

            var rsvpPage = await invitationPage.ClickRsvpLink();

            var inviteeRsvpFormSections = await rsvpPage.GetInviteeRsvpFormSections();

            inviteeRsvpFormSections.Should().HaveCount(1);

            var inviteeRsvpFormSection = inviteeRsvpFormSections.Single();

            await inviteeRsvpFormSection.SelectAttendingRadioOption(true);
            var requiresNoFoodContent = await inviteeRsvpFormSection.GetRequiresNoFoodContent();
            var requiresNoFoodContentText = await requiresNoFoodContent.InnerTextAsync();
            requiresNoFoodContentText.Should()
                .Contain($"We have {invitee.Name} down as not requiring any food, let us know if this isn't the case and we'll correct it.".ToUpper());

            invitationPage = await rsvpPage.SubmitForm();

            var invitationBanner = await invitationPage.GetRsvpBanner();

            (await invitationBanner.InnerTextAsync()).Should().Be("Thanks for your RSVP".ToUpper());
            (await invitationPage.HasRsvpLink()).Should().BeFalse();

            invitation = await GetInvitation(invitationId);

            using (new AssertionScope())
            {
            invitation.Status.Should().Be(InvitationStatus.ResponseReceived);
            invitation.RespondedAt.Should().NotBeNull();
            invitation.ContactInformation.Should().BeNull();
            invitee = invitation.Invitees.Single();
            invitee.Status.Should().Be(InviteeStatus.Attending);
            invitee.FoodOption.Should().BeNull();
            invitee.DietaryNotes.Should().BeNull();
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
                        new() { Name = personName, RequiresFood = true, },
                    },
                });

            createInvitationHttpResult.StatusCode.Should().Be(HttpStatusCode.Accepted);

            return createInvitationHttpResult.Value;
        }

        private async Task<string> CreateInvitationForTwoPeople()
        {
            var faker = new Faker();
            var firstPersonName = faker.Person.FullName;
            faker = new Faker();
            var secondPersonName = faker.Person.FullName;
            var createInvitationHttpResult = await _invitationsClient.CreateInvitation(
                new InvitationDefinition
                {
                    Code = Guid.NewGuid().ToString(),
                    Type = InvitationType.FullDay,
                    AddressedTo = $"{firstPersonName} and {secondPersonName}",
                    EmailAddress = faker.Person.Email.ToLower(),
                    Invitees = new List<InviteeDefinition>
                    {
                        new() { Name = firstPersonName, RequiresFood = true, },
                        new() { Name = secondPersonName, RequiresFood = true, },
                    },
                });

            createInvitationHttpResult.StatusCode.Should().Be(HttpStatusCode.Accepted);

            return createInvitationHttpResult.Value;
        }

        private async Task<string> CreateInvitationForPersonWhoDoesNotRequireFood()
        {
            var faker = new Faker();
            var personName = faker.Person.FullName;
            var createInvitationHttpResult = await _invitationsClient.CreateInvitation(
                new InvitationDefinition
                {
                    Code = Guid.NewGuid().ToString(),
                    Type = InvitationType.FullDay,
                    AddressedTo = personName,
                    EmailAddress = faker.Person.Email.ToLower(),
                    Invitees = new List<InviteeDefinition>
                    {
                        new() { Name = personName, RequiresFood = false, },
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
