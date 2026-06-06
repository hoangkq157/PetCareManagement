using System.Net;
using System.Net.Mail;

namespace PetCareManagement.Services;

public interface IEmailService
{
    Task SendOtpEmailAsync(string toEmail, string hoTen, string otpCode);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;

    public EmailService(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendOtpEmailAsync(string toEmail, string hoTen, string otpCode)
    {
        var smtp = _config.GetSection("EmailSettings");

        using var client = new SmtpClient(smtp["Host"], int.Parse(smtp["Port"]!))
        {
            EnableSsl   = bool.Parse(smtp["EnableSsl"]!),
            Credentials = new NetworkCredential(smtp["Username"], smtp["Password"])
        };

        var body = $"""
            <div style="font-family:Arial,sans-serif;max-width:520px;margin:auto;border:1px solid #e0e0e0;border-radius:12px;overflow:hidden">
              <div style="background:linear-gradient(135deg,#43a047,#1e88e5);padding:28px 32px;text-align:center">
                <span style="font-size:40px">🐾</span>
                <h2 style="color:#fff;margin:8px 0 0;font-size:20px">PetCare Management</h2>
              </div>
              <div style="padding:32px">
                <p style="color:#333;font-size:15px">Xin chào <strong>{hoTen}</strong>,</p>
                <p style="color:#555;font-size:14px">Chúng tôi nhận được yêu cầu đặt lại mật khẩu của bạn. Mã OTP của bạn là:</p>
                <div style="text-align:center;margin:24px 0">
                  <span style="display:inline-block;background:#f0f7ff;border:2px dashed #1e88e5;border-radius:10px;padding:14px 36px;font-size:36px;font-weight:700;letter-spacing:10px;color:#1e88e5">{otpCode}</span>
                </div>
                <p style="color:#e53935;font-size:13px;text-align:center">⏱ Mã có hiệu lực trong <strong>5 phút</strong></p>
                <p style="color:#999;font-size:12px;margin-top:24px">Nếu bạn không yêu cầu đặt lại mật khẩu, hãy bỏ qua email này. Tài khoản của bạn vẫn an toàn.</p>
              </div>
              <div style="background:#f9f9f9;padding:12px 32px;text-align:center">
                <p style="color:#bbb;font-size:11px;margin:0">© 2026 PetCare Management System</p>
              </div>
            </div>
            """;

        var message = new MailMessage
        {
            From       = new MailAddress(smtp["Username"]!, "PetCare Management"),
            Subject    = $"[PetCare] Mã OTP đặt lại mật khẩu: {otpCode}",
            Body       = body,
            IsBodyHtml = true
        };
        message.To.Add(toEmail);

        await client.SendMailAsync(message);
    }
}