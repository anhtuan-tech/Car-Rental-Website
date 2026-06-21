using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace CarRetalWebsite.Services
{
    public class EmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task<bool> SendResetPasswordOtpAsync(string toEmail, string userName, string otp)
        {
            var smtpServer = _configuration["SmtpSettings:Server"];
            var smtpPortStr = _configuration["SmtpSettings:Port"];
            var senderName = _configuration["SmtpSettings:SenderName"];
            var senderEmail = _configuration["SmtpSettings:SenderEmail"];
            var username = _configuration["SmtpSettings:Username"];
            var password = _configuration["SmtpSettings:Password"];

            int smtpPort = 587;
            if (!string.IsNullOrEmpty(smtpPortStr))
            {
                int.TryParse(smtpPortStr, out smtpPort);
            }

            string subject = "CarRental - Yêu cầu đặt lại mật khẩu";
            string body = $@"
                <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #e2e8f0; border-radius: 12px;'>
                    <h2 style='color: #0284c7; text-align: center;'>Đặt Lại Mật Khẩu CarRental</h2>
                    <p>Xin chào <strong>{userName}</strong>,</p>
                    <p>Chúng tôi nhận được yêu cầu đặt lại mật khẩu cho tài khoản của bạn. Vui lòng sử dụng mã OTP dưới đây để hoàn tất quá trình:</p>
                    <div style='text-align: center; margin: 30px 0;'>
                        <span style='font-size: 24px; font-weight: bold; letter-spacing: 5px; color: #0284c7; padding: 10px 20px; background-color: #f0f9ff; border-radius: 8px; border: 1px dashed #0284c7;'>{otp}</span>
                    </div>
                    <p style='color: #ef4444; font-weight: bold;'>Mã OTP này có hiệu lực trong vòng 5 phút.</p>
                    <p>Nếu bạn không thực hiện yêu cầu này, vui lòng bỏ qua email này hoặc liên hệ hỗ trợ để bảo mật tài khoản.</p>
                    <hr style='border: none; border-top: 1px solid #cbd5e1; margin: 20px 0;' />
                    <p style='font-size: 12px; color: #64748b; text-align: center;'>Đây là email tự động từ hệ thống CarRental. Vui lòng không trả lời thư này.</p>
                </div>";

            // Luôn ghi log OTP ra console để hỗ trợ kiểm thử / debug
            Console.WriteLine($"[EMAIL SERVICE] Gửi OTP đặt lại mật khẩu: Email={toEmail}, OTP={otp}");
            System.Diagnostics.Debug.WriteLine($"[EMAIL SERVICE] Gửi OTP đặt lại mật khẩu: Email={toEmail}, OTP={otp}");

            // Nếu chưa cấu hình hoặc để mặc định, chỉ ghi log và trả về true để cho phép chạy thử nghiệm
            if (string.IsNullOrEmpty(senderEmail) || senderEmail == "your-gmail@gmail.com" || string.IsNullOrEmpty(password) || password == "your-app-password")
            {
                Console.WriteLine("[EMAIL SERVICE] Thông tin SMTP chưa được cấu hình. Chỉ ghi log OTP lên console để test.");
                return true;
            }

            try
            {
                using (var mailMessage = new MailMessage())
                {
                    mailMessage.From = new MailAddress(senderEmail, senderName);
                    mailMessage.To.Add(toEmail);
                    mailMessage.Subject = subject;
                    mailMessage.Body = body;
                    mailMessage.IsBodyHtml = true;

                    using (var smtpClient = new SmtpClient(smtpServer, smtpPort))
                    {
                        smtpClient.Credentials = new NetworkCredential(username, password);
                        smtpClient.EnableSsl = true;
                        await smtpClient.SendMailAsync(mailMessage);
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EMAIL SERVICE ERROR] Lỗi khi gửi mail: {ex.Message}");
                // Trả về false để thông báo gửi thất bại
                return false;
            }
        }
    }
}
