using System.Collections.Generic;

namespace ConnorWyatt.Wedding.Invitations.Models
{
    public class InvitationDefinition
    {
        public string Code { get; set; }
        public InvitationType Type { get; set; }
        public string AddressedTo { get; set; }
        public string? EmailAddress { get; set; }
        public IList<InviteeDefinition> Invitees { get; set; }
    }
}