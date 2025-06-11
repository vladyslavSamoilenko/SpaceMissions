using System.ComponentModel.DataAnnotations;

namespace SpaceMissions.WebAP.DTOs;

public class UserLoginDto
{
    [Required(ErrorMessage = "Имя пользователя обязательно")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Длина имени от 3 до 100 символов")]
    public string Username { get; set; } = null!;

    [Required(ErrorMessage = "Пароль обязателен")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Длина пароля от 6 до 100 символов")]
    public string Password { get; set; } = null!;
}