namespace CI_Platform.Models.ViewModels
{
    public class SavedStoriesVM
    {
        public long MissionId { get; set; }

        public long UserId { get; set; }
        public long StoryId { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public string Status { get; set; }

        public string MissionTheme { get; set; }

        public string AuthorFirstName { get; set; }

        public string AuthorLastName { get; set; }

        public DateTime? PublishedAt { get; set; }
    }
}
