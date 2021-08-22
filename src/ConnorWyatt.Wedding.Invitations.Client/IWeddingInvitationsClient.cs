using System.Threading.Tasks;
using ConnorWyatt.Wedding.Common.Http;
using ConnorWyatt.Wedding.Invitations.Models;

namespace ConnorWyatt.Wedding.Invitations.Client
{
    public interface IWeddingInvitationsClient
    {
        Task<HttpResult<string>> CreateInvitation(InvitationDefinition definition);

        Task<HttpResult<Invitation>> GetInvitation(string invitationId);
    }
}