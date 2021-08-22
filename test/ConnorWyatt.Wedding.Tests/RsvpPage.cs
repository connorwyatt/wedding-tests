using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Playwright;

namespace ConnorWyatt.Wedding.Tests
{
    public class RsvpPage
    {
        public static async Task<RsvpPage> CastTo(IPage page)
        {
            await page.WaitForURLAsync(
                "**/invitation/*/rsvp",
                new PageWaitForURLOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

            return new RsvpPage(page);
        }

        private readonly IPage _page;

        private RsvpPage(IPage page)
        {
            _page = page;
        }

        public async Task<IReadOnlyList<InviteeRsvpFormSection>> GetInviteeRsvpFormSections()
        {
            var sections = await _page.QuerySelectorAllAsync("data-test-id=inviteeRsvpFormSection");

            return sections.Select(InviteeRsvpFormSection.CastTo).ToImmutableArray();
        }

        public async Task EnterContactInformation(string contactInformation)
        {
            var input = await _page.QuerySelectorAsync("data-test-id=contactInformationInput");

            if (input is null)
            {
                throw new MissingExpectedElementException();
            }

            await input.FillAsync(contactInformation);
        }

        public async Task<InvitationPage> SubmitForm()
        {
            await Task.WhenAll(
                _page.WaitForNavigationAsync(
                    new PageWaitForNavigationOptions { WaitUntil = WaitUntilState.DOMContentLoaded }),
                _page.ClickAsync("text=Respond"));

            return await InvitationPage.CastTo(_page);
        }
    }
}
