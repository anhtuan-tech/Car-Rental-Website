namespace CarRetalWebsite.Models
{
    public class CarRemovalRequest
    {
        public int CarId { get; set; }
        public int OwnerId { get; set; }
        public string Reason { get; set; } = null!;
        public DateTime RequestDate { get; set; } = DateTime.Now;
        public string Status { get; set; } = "Pending";
    }
}
