using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetCareManagement.Data;
using PetCareManagement.Models;
using PetCareManagement.Services;

namespace PetCareManagement.Controllers;

public class AuthController : Controller
{
    private readonly PetCareDbContext _context;
    private readonly IEmailService    _emailService;

    public AuthController(PetCareDbContext context, IEmailService emailService)
    {
        _context      = context;
        _emailService = emailService;
    }

    // ─────────────────────────────────────────────
    // ĐĂNG NHẬP
    // ─────────────────────────────────────────────

    public IActionResult Login()
    {
        if (HttpContext.Session.GetString("NhanVienId") != null)
            return RedirectToAction("Index", "Home");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string matKhau)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(matKhau))
        {
            ViewBag.Error = "Vui lòng nhập đầy đủ Email và Mật khẩu.";
            return View();
        }

        var nv = await _context.NhanViens
            .FirstOrDefaultAsync(n => n.Email == email
                                   && n.MatKhau == matKhau
                                   && n.TrangThai == true);

        if (nv == null)
        {
            ViewBag.Error = "Email hoặc mật khẩu không đúng, hoặc tài khoản đã bị khoá.";
            return View();
        }

        HttpContext.Session.SetString("NhanVienId",   nv.MaNv.ToString());
        HttpContext.Session.SetString("NhanVienName", nv.HoTen);
        HttpContext.Session.SetString("NhanVienRole", nv.VaiTro);

        return RedirectToAction("Index", "Home");
    }

    // ─────────────────────────────────────────────
    // ĐĂNG KÝ
    // ─────────────────────────────────────────────

    public IActionResult Register()
    {
        if (HttpContext.Session.GetString("NhanVienId") != null)
            return RedirectToAction("Index", "Home");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(
        string hoTen,
        string email,
        string matKhau,
        string xacNhanMatKhau,
        string? soDienThoai)
    {
        void GiuLai() {
            ViewBag.HoTen       = hoTen;
            ViewBag.Email       = email;
            ViewBag.SoDienThoai = soDienThoai;
        }

        if (string.IsNullOrWhiteSpace(hoTen) ||
            string.IsNullOrWhiteSpace(email)  ||
            string.IsNullOrWhiteSpace(matKhau))
        {
            ViewBag.Error = "Vui lòng điền đầy đủ các trường bắt buộc.";
            GiuLai(); return View();
        }

        if (matKhau.Length < 6)
        {
            ViewBag.Error = "Mật khẩu phải có ít nhất 6 ký tự.";
            GiuLai(); return View();
        }

        if (matKhau != xacNhanMatKhau)
        {
            ViewBag.Error = "Mật khẩu xác nhận không khớp.";
            GiuLai(); return View();
        }

        if (await _context.NhanViens.AnyAsync(n => n.Email == email))
        {
            ViewBag.Error = "Email này đã được sử dụng. Vui lòng chọn email khác.";
            GiuLai(); return View();
        }

        var nv = new NhanVien
        {
            HoTen       = hoTen.Trim(),
            Email       = email.Trim().ToLower(),
            MatKhau     = matKhau,
            VaiTro      = "NhanVien",
            SoDienThoai = soDienThoai?.Trim(),
            NgayTao     = DateTime.Now,
            TrangThai   = true
        };

        _context.NhanViens.Add(nv);
        await _context.SaveChangesAsync();

        HttpContext.Session.SetString("NhanVienId",   nv.MaNv.ToString());
        HttpContext.Session.SetString("NhanVienName", nv.HoTen);
        HttpContext.Session.SetString("NhanVienRole", nv.VaiTro);

        TempData["Success"] = $"Chào mừng {nv.HoTen} đã tham gia PetCare!";
        return RedirectToAction("Index", "Home");
    }

    // ─────────────────────────────────────────────
    // QUÊN MẬT KHẨU – Bước 1: Nhập email
    // ─────────────────────────────────────────────

    public IActionResult QuenMatKhau() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuenMatKhau(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            ViewBag.Error = "Vui lòng nhập địa chỉ email.";
            return View();
        }

        var nv = await _context.NhanViens
            .FirstOrDefaultAsync(n => n.Email == email.Trim().ToLower() && n.TrangThai == true);

        ViewBag.Success = "Nếu email tồn tại trong hệ thống, mã OTP đã được gửi. Vui lòng kiểm tra hộp thư.";

        if (nv != null)
        {
            var otp    = new Random().Next(100000, 999999).ToString();
            var hetHan = DateTime.Now.AddMinutes(5);

            HttpContext.Session.SetString("OTP_Code",   otp);
            HttpContext.Session.SetString("OTP_Email",  nv.Email);
            HttpContext.Session.SetString("OTP_HetHan", hetHan.ToString("o"));

            try { await _emailService.SendOtpEmailAsync(nv.Email, nv.HoTen, otp); }
            catch { /* ghi log nếu cần */ }
        }

        return View();
    }

    // ─────────────────────────────────────────────
    // QUÊN MẬT KHẨU – Bước 2: Xác minh OTP
    // ─────────────────────────────────────────────

    public IActionResult XacMinhOtp()
    {
        if (HttpContext.Session.GetString("OTP_Email") == null)
            return RedirectToAction("QuenMatKhau");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult XacMinhOtp(string otp)
    {
        var otpLuu    = HttpContext.Session.GetString("OTP_Code");
        var hetHanStr = HttpContext.Session.GetString("OTP_HetHan");

        if (string.IsNullOrWhiteSpace(otpLuu) || string.IsNullOrWhiteSpace(hetHanStr))
        {
            ViewBag.Error = "Phiên làm việc hết hạn. Vui lòng thử lại.";
            return View();
        }

        if (DateTime.Parse(hetHanStr) < DateTime.Now)
        {
            ViewBag.Error = "Mã OTP đã hết hạn. Vui lòng yêu cầu mã mới.";
            return View();
        }

        if (otp?.Trim() != otpLuu)
        {
            ViewBag.Error = "Mã OTP không đúng. Vui lòng kiểm tra lại.";
            return View();
        }

        HttpContext.Session.SetString("OTP_Verified", "true");
        return RedirectToAction("DatLaiMatKhau");
    }

    // ─────────────────────────────────────────────
    // QUÊN MẬT KHẨU – Bước 3: Đặt lại mật khẩu
    // ─────────────────────────────────────────────

    public IActionResult DatLaiMatKhau()
    {
        if (HttpContext.Session.GetString("OTP_Verified") != "true")
            return RedirectToAction("QuenMatKhau");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DatLaiMatKhau(string matKhauMoi, string xacNhanMatKhau)
    {
        if (HttpContext.Session.GetString("OTP_Verified") != "true")
            return RedirectToAction("QuenMatKhau");

        if (string.IsNullOrWhiteSpace(matKhauMoi) || matKhauMoi.Length < 6)
        {
            ViewBag.Error = "Mật khẩu phải có ít nhất 6 ký tự.";
            return View();
        }

        if (matKhauMoi != xacNhanMatKhau)
        {
            ViewBag.Error = "Mật khẩu xác nhận không khớp.";
            return View();
        }

        var email = HttpContext.Session.GetString("OTP_Email");
        var nv    = await _context.NhanViens
            .FirstOrDefaultAsync(n => n.Email == email && n.TrangThai == true);

        if (nv == null)
        {
            ViewBag.Error = "Không tìm thấy tài khoản. Vui lòng thử lại.";
            return View();
        }

        nv.MatKhau = matKhauMoi;
        await _context.SaveChangesAsync();

        HttpContext.Session.Remove("OTP_Code");
        HttpContext.Session.Remove("OTP_Email");
        HttpContext.Session.Remove("OTP_HetHan");
        HttpContext.Session.Remove("OTP_Verified");

        TempData["Success"] = "Mật khẩu đã được đặt lại thành công. Vui lòng đăng nhập.";
        return RedirectToAction("Login");
    }

    // ─────────────────────────────────────────────
    // ĐĂNG XUẤT
    // ─────────────────────────────────────────────

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }
}