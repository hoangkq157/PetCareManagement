using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetCareManagement.Data;
using PetCareManagement.Models;
using PetCareManagement.Services;
using Microsoft.AspNetCore.Authentication;

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
        if (HttpContext.Session.GetString("ChuNuoiId") != null)
            return RedirectToAction("Index", "ChuNuoi");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string matKhau)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(matKhau))
        {
            ViewBag.Error = "Vui lòng nhập đầy đủ email và mật khẩu.";
            return View();
        }

        var nv = await _context.NhanViens
            .FirstOrDefaultAsync(n => n.Email == email
                                   && n.MatKhau == matKhau
                                   && n.TrangThai == true);
        if (nv != null)
        {
            HttpContext.Session.SetString("NhanVienId",   nv.MaNv.ToString());
            HttpContext.Session.SetString("NhanVienName", nv.HoTen);
            HttpContext.Session.SetString("NhanVienRole", nv.VaiTro);
            return RedirectToAction("Index", "Home");
        }

        var cn = await _context.ChuNuois
            .FirstOrDefaultAsync(c => c.Email == email && c.MatKhau == matKhau);
        if (cn != null)
        {
            HttpContext.Session.SetString("ChuNuoiId",   cn.MaCn.ToString());
            HttpContext.Session.SetString("ChuNuoiName", cn.HoTen);
            return RedirectToAction("Index", "ChuNuoi");
        }

        ViewBag.Error = "Tài khoản / mật khẩu không đúng, hoặc tài khoản đã bị khoá.";
        return View();
    }

    // ─────────────────────────────────────────────
    // ĐĂNG KÝ (chỉ dành cho Chủ nuôi)
    // ─────────────────────────────────────────────

    public IActionResult Register()
    {
        if (HttpContext.Session.GetString("ChuNuoiId") != null)
            return RedirectToAction("Index", "ChuNuoi");
        if (HttpContext.Session.GetString("NhanVienId") != null)
            return RedirectToAction("Index", "Home");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(
        string hoTen,
        string soDienThoai,
        string matKhau,
        string xacNhanMatKhau,
        string? email,
        string? diaChi)
    {
        void GiuLai() {
            ViewBag.HoTen       = hoTen;
            ViewBag.Email       = email;
            ViewBag.SoDienThoai = soDienThoai;
        }

        if (string.IsNullOrWhiteSpace(hoTen) ||
            string.IsNullOrWhiteSpace(soDienThoai) ||
            string.IsNullOrWhiteSpace(matKhau))
        { ViewBag.Error = "Vui lòng điền đầy đủ các trường bắt buộc."; GiuLai(); return View(); }

        if (matKhau.Length < 6)
        { ViewBag.Error = "Mật khẩu phải có ít nhất 6 ký tự."; GiuLai(); return View(); }

        if (matKhau != xacNhanMatKhau)
        { ViewBag.Error = "Mật khẩu xác nhận không khớp."; GiuLai(); return View(); }

        if (!string.IsNullOrWhiteSpace(email) &&
            await _context.ChuNuois.AnyAsync(c => c.Email == email.Trim().ToLower()))
        {
            ViewBag.Error = "Email này đã được sử dụng.";
            GiuLai(); return View();
        }

        var cn = new ChuNuoi
        {
            HoTen       = hoTen.Trim(),
            SoDienThoai = soDienThoai.Trim(),
            MatKhau     = matKhau,
            Email       = email?.Trim().ToLower(),
            DiaChi      = diaChi?.Trim(),
            NgayDangKy  = DateOnly.FromDateTime(DateTime.Today)
        };

        _context.ChuNuois.Add(cn);
        await _context.SaveChangesAsync();

        HttpContext.Session.SetString("ChuNuoiId",   cn.MaCn.ToString());
        HttpContext.Session.SetString("ChuNuoiName", cn.HoTen);

        TempData["Success"] = $"Chào mừng {cn.HoTen}! Hãy thêm thú cưng của bạn.";
        return RedirectToAction("Index", "ChuNuoi");
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

        email = email.Trim().ToLower();

        // Tìm trong ChuNuoi trước, sau đó NhanVien
        string? hoTen     = null;
        string? loaiTK    = null;

        var cn = await _context.ChuNuois
            .FirstOrDefaultAsync(c => c.Email == email);
        if (cn != null)
        {
            hoTen  = cn.HoTen;
            loaiTK = "ChuNuoi";
        }
        else
        {
            var nv = await _context.NhanViens
                .FirstOrDefaultAsync(n => n.Email == email && n.TrangThai == true);
            if (nv != null)
            {
                hoTen  = nv.HoTen;
                loaiTK = "NhanVien";
            }
        }

        // Luôn hiện thông báo giống nhau để tránh lộ thông tin
        ViewBag.Success = "Nếu email tồn tại trong hệ thống, mã OTP đã được gửi. Vui lòng kiểm tra hộp thư (kể cả thư mục Spam).";

        if (hoTen != null && loaiTK != null)
        {
            var otp    = new Random().Next(100000, 999999).ToString();
            var hetHan = DateTime.Now.AddMinutes(5);

            HttpContext.Session.SetString("OTP_Code",   otp);
            HttpContext.Session.SetString("OTP_Email",  email);
            HttpContext.Session.SetString("OTP_LoaiTK", loaiTK);
            HttpContext.Session.SetString("OTP_HetHan", hetHan.ToString("o"));

            try
            {
                await _emailService.SendOtpEmailAsync(email, hoTen, otp);
            }
            catch (Exception ex)
            {
                // Hiện lỗi cụ thể khi debug (xóa dòng này khi production)
                ViewBag.Success = null;
                ViewBag.Error   = $"Không thể gửi email: {ex.Message}";
            }
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

        var email  = HttpContext.Session.GetString("OTP_Email");
        var loaiTK = HttpContext.Session.GetString("OTP_LoaiTK");

        bool daCapNhat = false;

        if (loaiTK == "ChuNuoi")
        {
            var cn = await _context.ChuNuois
                .FirstOrDefaultAsync(c => c.Email == email);
            if (cn != null)
            {
                cn.MatKhau = matKhauMoi;
                await _context.SaveChangesAsync();
                daCapNhat = true;
            }
        }
        else if (loaiTK == "NhanVien")
        {
            var nv = await _context.NhanViens
                .FirstOrDefaultAsync(n => n.Email == email && n.TrangThai == true);
            if (nv != null)
            {
                nv.MatKhau = matKhauMoi;
                await _context.SaveChangesAsync();
                daCapNhat = true;
            }
        }

        if (!daCapNhat)
        {
            ViewBag.Error = "Không tìm thấy tài khoản. Vui lòng thử lại.";
            return View();
        }

        // Xóa toàn bộ dữ liệu OTP khỏi session
        HttpContext.Session.Remove("OTP_Code");
        HttpContext.Session.Remove("OTP_Email");
        HttpContext.Session.Remove("OTP_LoaiTK");
        HttpContext.Session.Remove("OTP_HetHan");
        HttpContext.Session.Remove("OTP_Verified");

        TempData["Success"] = "Mật khẩu đã được đặt lại thành công. Vui lòng đăng nhập.";
        return RedirectToAction("Login");
    }
    // ĐĂNG NHẬP BẰNG GOOGLE
    public IActionResult LoginGoogle()
    {
        var redirectUrl = Url.Action("GoogleCallback", "Auth");
        var properties = new AuthenticationProperties
        {
            RedirectUri = redirectUrl
        };
        return Challenge(properties, "Google");
    }

    public async Task<IActionResult> GoogleCallback()
    {
        var result = await HttpContext.AuthenticateAsync("Cookies");
        if (!result.Succeeded)
        {
            ViewBag.Error = "Đăng nhập Google thất bại. Vui lòng thử lại.";
            return View("Login");
        }

        var claims = result.Principal!.Claims.ToList();
        var email  = claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Email)?.Value;
        var hoTen  = claims.FirstOrDefault(c => c.Type == System.Security.Claims.ClaimTypes.Name)?.Value;

        if (string.IsNullOrEmpty(email))
        {
            ViewBag.Error = "Không lấy được email từ Google.";
            return View("Login");
        }

        // Tìm ChuNuoi theo email, nếu chưa có thì tạo mới
        var cn = await _context.ChuNuois
            .FirstOrDefaultAsync(c => c.Email == email.ToLower());

        if (cn == null)
        {
            cn = new ChuNuoi
            {
                HoTen      = hoTen ?? email,
                Email      = email.ToLower(),
                MatKhau    = "",
                NgayDangKy = DateOnly.FromDateTime(DateTime.Today)
            };
            _context.ChuNuois.Add(cn);
            await _context.SaveChangesAsync();
        }

        // Set Session như đăng nhập thường
        HttpContext.Session.SetString("ChuNuoiId",   cn.MaCn.ToString());
        HttpContext.Session.SetString("ChuNuoiName", cn.HoTen);

        await HttpContext.SignOutAsync("Cookies"); // dọn cookie OAuth tạm

        TempData["Success"] = $"Chào mừng {cn.HoTen}!";
        return RedirectToAction("Index", "ChuNuoi");
    }
    // ĐĂNG XUẤT
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return RedirectToAction("Login");
    }
}