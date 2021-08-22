using System;
using System.Collections.Generic;
using NodaTime;

namespace ConnorWyatt.Wedding.Invitations.Models
{
    public class Invitation
    {
        public string Id { get; set; }
        public string Code { get; set; }
        public InvitationStatus Status { get; set; }
        public InvitationType Type { get; set; }
        public Instant CreatedAt { get; set; }
        public string AddressedTo { get; set; }
        public string? EmailAddress { get; set; }
        public bool EmailSent { get; set; } = false;
        public string? ContactInformation { get; set; }
        public Instant? SentAt { get; set; }
        public Instant? RespondedAt { get; set; }
        public IList<Invitee> Invitees { get; set; } = Array.Empty<Invitee>();
    }
}