namespace ConnorWyatt.Wedding.Invitations.Models
{
    public class Invitee
    {
        public string Id { get; set; }
        public string? Name { get; set; }
        public InviteeStatus Status { get; set; }
        public FoodOption? FoodOption { get; set; } = null;
        public string? DietaryNotes { get; set; } = null;
    }
}