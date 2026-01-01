using System.ComponentModel.DataAnnotations;

namespace StoreAPI.Models
{
    public class LoginModels
    {
        [Required(ErrorMessage = "Username is required")]
        public required string Username { get; set; }

        [Required(ErrorMessage = "Password is required")]
        public required string Password { get; set; }
    }
}