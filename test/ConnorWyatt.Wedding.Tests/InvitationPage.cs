using System.Threading.Tasks;
using Microsoft.Playwright;

namespace ConnorWyatt.Wedding.Tests
{
    public class InvitationPage
    {
        public static async Task<InvitationPage> NavigateTo(IPage page, string code)
        {
            await page.GotoAsync($"{Urls.Wedding}invitation/{code}");
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            return new InvitationPage(page);
        }

        public static async Task<InvitationPage> CastTo(IPage page)
        {
            await page.WaitForURLAsync(
                "**/invitation/*",
                new PageWaitForURLOptions { WaitUntil = WaitUntilState.DOMContentLoaded });

            return new InvitationPage(page);
        }

        private readonly IPage _page;

        private InvitationPage(IPage page)
        {
            _page = page;
        }

        public async Task<IElementHandle> GetInvitationDescription() =>
            await _page.WaitForSelectorAsync("data-test-id=invitationDescription")
            ?? throw new MissingExpectedElementException();

        public async Task<IElementHandle> GetDate() =>
            await _page.WaitForSelectorAsync("data-test-id=date")
            ?? throw new MissingExpectedElementException();

        public async Task<IElementHandle> GetDayTimings() =>
            await _page.WaitForSelectorAsync("data-test-id=dayTimings")
            ?? throw new MissingExpectedElementException();

        public async Task<IElementHandle> GetLocation() =>
            await _page.WaitForSelectorAsync("data-test-id=location")
            ?? throw new MissingExpectedElementException();

        public async Task<IElementHandle> GetRsvpDate() =>
            await _page.WaitForSelectorAsync("data-test-id=rsvpDate")
            ?? throw new MissingExpectedElementException();

        public async Task<IElementHandle> GetRsvpBanner() =>
            await _page.WaitForSelectorAsync("data-test-id=rsvpBanner")
            ?? throw new MissingExpectedElementException();

        public async Task<bool> HasRsvpLink()
        {
            return await _page.QuerySelectorAsync("data-test-id=rsvpLink") is not null;
        }

        public async Task<RsvpPage> ClickRsvpLink()
        {
            await Task.WhenAll(
                _page.WaitForNavigationAsync(
                    new PageWaitForNavigationOptions { WaitUntil = WaitUntilState.DOMContentLoaded }),
                _page.ClickAsync("data-test-id=rsvpLink"));

            return await RsvpPage.CastTo(_page);
        }
    }
}
