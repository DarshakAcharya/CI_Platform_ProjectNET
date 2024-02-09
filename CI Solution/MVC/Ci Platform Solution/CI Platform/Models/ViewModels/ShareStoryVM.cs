namespace CI_Platform.Models.ViewModels
{
    public class ShareStoryVM
    {
        public long UserId { get; set; }

        public long StoryId { get; set; }

        public long MissionId { get; set; }

        public string StoryTitle { get; set; }

         

        //public string Status { get; set; }

        public DateTime? PublishedAt { get; set; }

        public string MyStory { get; set; }

        public string URL { get; set; }

        //public string Images { get; set; }
 
    }
}
