using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace CarRetalWebsite.Models
{
    public class UserProfileViewModel
    {
        public int UserId { get; set; }
        public string Email { get; set; } = null!;
        public string PhoneNumber { get; set; } = null!;
        public string RoleName { get; set; } = null!;
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }

        public string FullName { get; set; } = null!;
        public string? AvatarUrl { get; set; }
        public string? DriverLicenseNo { get; set; }
        public string? IdCardNo { get; set; }
        public string? Address { get; set; }

        // Statistics for Customer / Owner
        public int TotalBookings { get; set; }
        public decimal TotalSpent { get; set; }
        public int ActiveBookings { get; set; }

        public int TotalCars { get; set; }
        public int TotalOwnerBookings { get; set; }
        public decimal TotalOwnerRevenue { get; set; }
    }

    public class EditProfileViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập Họ và tên.")]
        [StringLength(100, ErrorMessage = "Họ và tên không vượt quá 100 ký tự.")]
        public string FullName { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng nhập Số điện thoại.")]
        [RegularExpression(@"^(03|05|07|08|09)\d{8}$", ErrorMessage = "Số điện thoại phải gồm đúng 10 chữ số hợp lệ của nhà mạng Việt Nam (bắt đầu bằng 03, 05, 07, 08, 09).")]
        public string PhoneNumber { get; set; } = null!;

        [RegularExpression(@"^\d{12}$", ErrorMessage = "Số Giấy phép lái xe (GPLX) phải gồm đúng 12 chữ số.")]
        public string? DriverLicenseNo { get; set; }

        [RegularExpression(@"^(\d{9}|\d{12})$", ErrorMessage = "Số CCCD / CMND phải gồm đúng 9 hoặc 12 chữ số.")]
        public string? IdCardNo { get; set; }

        [StringLength(255, ErrorMessage = "Địa chỉ không vượt quá 255 ký tự.")]
        public string? Address { get; set; }

        public string? CurrentAvatarUrl { get; set; }
        public IFormFile? AvatarFile { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu hiện tại.")]
        [DataType(DataType.Password)]
        public string CurrentPassword { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới.")]
        [MinLength(6, ErrorMessage = "Mật khẩu mới phải chứa ít nhất 6 ký tự.")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = null!;

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu mới.")]
        [Compare("NewPassword", ErrorMessage = "Mật khẩu xác nhận không trùng khớp với mật khẩu mới.")]
        [DataType(DataType.Password)]
        public string ConfirmNewPassword { get; set; } = null!;
    }
}
