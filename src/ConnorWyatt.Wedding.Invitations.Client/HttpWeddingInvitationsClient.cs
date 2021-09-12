using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ConnorWyatt.Wedding.Common.Http;
using ConnorWyatt.Wedding.Common.Models;
using ConnorWyatt.Wedding.Invitations.Models;
using NodaTime;
using NodaTime.Serialization.SystemTextJson;

namespace ConnorWyatt.Wedding.Invitations.Client
{
    public class HttpWeddingInvitationsClient : IWeddingInvitationsClient
    {
        private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
            }
            .ConfigureForNodaTime(DateTimeZoneProviders.Tzdb);

        private readonly HttpClient _client;

        public HttpWeddingInvitationsClient(HttpClient client)
        {
            _client = client;
        }

        public async Task<HttpResult<string>> CreateInvitation(InvitationDefinition definition)
        {
            var response = await _client.PostAsJsonAsync("/invitations", definition, SerializerOptions);

            if (!response.IsSuccessStatusCode) return HttpResult<string>.Error(response.StatusCode);

            var reference = await response.Content.ReadFromJsonAsync<Reference>();

            if (reference is null) throw new InvalidOperationException("Cannot deserialise body to Reference.");

            return HttpResult<string>.Success(response.StatusCode, reference.Id);
        }

        public async Task<HttpResult<Invitation>> GetInvitation(string invitationId)
        {
            var response = await _client.GetAsync($"/invitations/{invitationId}");

            if (!response.IsSuccessStatusCode) return HttpResult<Invitation>.Error(response.StatusCode);

            var invitation = await response.Content.ReadFromJsonAsync<Invitation>(SerializerOptions);

            if (invitation is null) throw new InvalidOperationException("Cannot deserialise body to Invitation.");

            return HttpResult<Invitation>.Success(response.StatusCode, invitation);
        }


        public async Task<HttpResult<IList<Invitation>>> GetInvitations()
        {
            var response = await _client.GetAsync("/invitations");

            if (!response.IsSuccessStatusCode) return HttpResult<IList<Invitation>>.Error(response.StatusCode);

            var invitations = await response.Content.ReadFromJsonAsync<IList<Invitation>>(SerializerOptions);

            if (invitations is null) throw new InvalidOperationException("Cannot deserialise body to IList<Invitation>.");

            return HttpResult<IList<Invitation>>.Success(response.StatusCode, invitations);
        }

        public async Task<HttpResult<object>> SendEmail(string invitationId)
        {
            var response = await _client.PostAsJsonAsync($"/invitations/{invitationId}/actions/send-invitation-email", new {});

            if (!response.IsSuccessStatusCode) return HttpResult<object>.Error(response.StatusCode);

            return HttpResult<object>.Success(response.StatusCode, new {});
        }
    }
}
