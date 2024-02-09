using CI_Platform_Entites.Models;
using System.ComponentModel.DataAnnotations;

namespace CI_Platform.Models.ViewModels
{
    public class AdminUserAddVM
    {
        public long AdminId { get; set; }

        public long UserId { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Email { get; set; }

        public string Password { get; set; }

        public string EmployeeId { get; set; }

        public string Department { get; set; }

        public string Status { get; set; }

        public string ProfileText { get; set; }

        public long BannerId { get; set; }
        [Required(AllowEmptyStrings = false, ErrorMessage = "Please Provide Image")]

        public IFormFile Image { get; set; }
 

        public string Avatar { get; set; }

        public  long? CityId { get; set; }

        public long?  CountryId { get; set; }

    }
}
