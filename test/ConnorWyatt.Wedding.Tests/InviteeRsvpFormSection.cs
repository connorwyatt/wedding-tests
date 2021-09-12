using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;
using ConnorWyatt.Wedding.Invitations.Models;
using Microsoft.Playwright;

namespace ConnorWyatt.Wedding.Tests
{
    public class InviteeRsvpFormSection
    {
        private static readonly IReadOnlyDictionary<bool, string> AttendingRadioOptionsText =
            new Dictionary<bool, string>
            {
                { true, "Yes" },
                { false, "No" },
            }.ToImmutableDictionary();

        private static readonly IReadOnlyDictionary<FoodOption, string> FoodOptionRadioOptionsText =
            new Dictionary<FoodOption, string>
            {
                { FoodOption.Standard, "Standard" },
                { FoodOption.Vegetarian, "Vegetarian" },
            }.ToImmutableDictionary();

        public static InviteeRsvpFormSection CastTo(IElementHandle elementHandle)
        {
            return new InviteeRsvpFormSection(elementHandle);
        }

        private readonly IElementHandle _elementHandle;

        private InviteeRsvpFormSection(IElementHandle elementHandle)
        {
            _elementHandle = elementHandle;
        }

        public async Task SelectAttendingRadioOption(bool attending)
        {
            var options = await _elementHandle.QuerySelectorAllAsync("data-test-id=attendingRadioOption");
            var textToSelect = AttendingRadioOptionsText[attending]
                ?? throw new NullReferenceException($"No AttendingRadioOptionsText for {attending}");

            foreach (var option in options)
            {
                var text = await option.WaitForSelectorAsync($"text={textToSelect}");
                if (text is not null)
                {
                    await text.ClickAsync();
                }
            }
        }

        public async Task SelectFoodOptionRadioOption(FoodOption foodOption)
        {
            var options = await _elementHandle.QuerySelectorAllAsync("data-test-id=foodOptionRadioOption");
            var textToSelect = FoodOptionRadioOptionsText[foodOption]
                ?? throw new NullReferenceException($"No FoodOptionRadioOptionsText for {foodOption}");

            foreach (var option in options)
            {
                var text = await option.WaitForSelectorAsync($"text={textToSelect}");
                if (text is not null)
                {
                    await text.ClickAsync();
                }
            }
        }

        public async Task EnterDietaryInformation(string dietaryInformation)
        {
            var input = await _elementHandle.QuerySelectorAsync("data-test-id=dietaryInformationInput");

            if (input is null)
            {
                throw new MissingExpectedElementException();
            }

            await input.FillAsync(dietaryInformation);
        }

        public async Task<IElementHandle> GetRequiresNoFoodContent()
        {
            var content = await _elementHandle.QuerySelectorAsync("data-test-id=requiresNoFoodContent");

            if (content is null)
            {
                throw new MissingExpectedElementException();
            }

            return content;
        }
    }
}
