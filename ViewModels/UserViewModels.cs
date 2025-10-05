using System.ComponentModel.DataAnnotations;

namespace TaskManagementApp.ViewModels
{
    public class UserViewModel
    {
        public string Id { get; set; }
        public string Email { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public List<string> Roles { get; set; }
    }

    public class CreateUserViewModel
    {
        [Required]
        [EmailAddress]
        [Display(Name = "E-pasts")]
        public string Email { get; set; }

        [Required]
        [Display(Name = "Vārds")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Uzvārds")]
        public string LastName { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "Parolei jābūt vismaz {2} rakstzīmju garai.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Parole")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Display(Name = "Apstiprināt paroli")]
        [Compare("Password", ErrorMessage = "Paroles nesakrīt.")]
        public string ConfirmPassword { get; set; }

        [Display(Name = "Loma")]
        public string Role { get; set; }
    }

    public class EditUserViewModel
    {
        public string Id { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "E-pasts")]
        public string Email { get; set; }

        [Required]
        [Display(Name = "Vārds")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Uzvārds")]
        public string LastName { get; set; }

        [Display(Name = "Loma")]
        public string Role { get; set; }
    }
}