using System.ComponentModel.DataAnnotations;

namespace API.DTOs
{
    public class UserRegister
    {
        [Required]
        public string UserName { get; set; }
        [Required]
        [StringLength(8, MinimumLength = 4)]
        public string Password { get; set; }
    }
}