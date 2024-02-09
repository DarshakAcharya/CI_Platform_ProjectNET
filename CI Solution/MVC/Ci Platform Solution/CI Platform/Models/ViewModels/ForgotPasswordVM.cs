using System.ComponentModel.DataAnnotations;

namespace CI_Platform.Models.ViewModels
{
    public class ForgotPasswordVM
    {
        [Required(ErrorMessage ="Please Enter Email Address!")]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; } 
    }
}
