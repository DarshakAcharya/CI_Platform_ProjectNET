namespace CI_Platform.Models.ViewModels
{
    public class ContactUsVM
    {
        //properties of Contact Us VM

        public long UserId { get; set; }

        public string? FirstName { get; set; }

        public string? LastName { get; set; }

        public string? Email { get; set; }

        public string? Subject { get; set; }

        public string? Message { get; set; }

        public DateTime? QcreatedAt { get; set; }

    }  
}
