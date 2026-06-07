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
        // Nếu NhanVien đã đăng nhập → về trang quản lý
        if (HttpContext.Session.GetString("NhanVienId") != null)
            return RedirectToAction("Index", "Home");

        // Nếu ChuNuoi đã đăng nhập → về trang Home (tạm thời)
        if (HttpContext.Session.GetString("ChuNuoiId") != null)
            return RedirectToAction("Index", "ChuNuoi");

        return View();
    }

    // POST /Auth/Login
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string email, string matKhau)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(matKhau))
        {
            ViewBag.Error = "Vui lòng nhập đầy đủ email và mật khẩu.";
            return View();
        }

        // ── Bước 1: Thử đăng nhập NhanVien (bằng Email) ──────────────────
        var nv = await _context.NhanViens
            .FirstOrDefaultAsync(n => n.Email == email
                                   && n.MatKhau == matKhau
                                   && n.TrangThai == true);

        if (nv != null)
        {
            HttpContext.Session.SetString("NhanVienId",   nv.MaNv.ToString());
            HttpContext.Session.SetString("NhanVienName", nv.HoTen);
            HttpContext.Session.SetString("NhanVienRole", nv.VaiTro);
            // Nhân viên → trang quản lý
            return RedirectToAction("Index", "Home");
        }

        // ── Bước 2: Thử đăng nhập ChuNuoi (bằng Email) ──────────
        var cn = await _context.ChuNuois
            .FirstOrDefaultAsync(c => c.Email == email
                                   && c.MatKhau == matKhau);

        if (cn != null)
        {
            HttpContext.Session.SetString("ChuNuoiId",   cn.MaCn.ToString());
            HttpContext.Session.SetString("ChuNuoiName", cn.HoTen);
            // Chủ nuôi → tạm thời dùng trang Home, sau đổi sang "ChuNuoiPortal"
            return RedirectToAction("Index", "ChuNuoi");
        }

        // ── Cả 2 đều thất bại ─────────────────────────────────────────────
        ViewBag.Error = "Tài khoản / mật khẩu không đúng, hoặc tài khoản đã bị khoá.";
        return View();
    }

    // ─────────────────────────────────────────────
    // ĐĂNG KÝ (chỉ dành cho Chủ nuôi)
    // ─────────────────────────────────────────────

    public IActionResult Register()
    {
        // Nếu ChuNuoi đã đăng nhập → về Home (tạm thời)
        if (HttpContext.Session.GetString("ChuNuoiId") != null)
            return RedirectToAction("Index", "ChuNuoi");

        // Nếu NhanVien đã đăng nhập → về trang quản lý
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
        // Giữ lại giá trị đã nhập khi có lỗi
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

        // Kiểm tra email trùng
        if (await _context.NhanViens.AnyAsync(n => n.Email == email))
        {
            ViewBag.Error = "Email này đã được sử dụng. Vui lòng chọn email khác.";
            GiuLai(); return View();
        }

        // Lưu vào database
        var cn = new ChuNuoi
        {
            HoTen        = hoTen.Trim(),
            SoDienThoai  = soDienThoai.Trim(),
            MatKhau      = matKhau,
            Email        = email?.Trim().ToLower(),
            DiaChi       = diaChi?.Trim(),
            NgayDangKy   = DateOnly.FromDateTime(DateTime.Today)
        };

        _context.ChuNuois.Add(cn);
        await _context.SaveChangesAsync();

        // Đăng nhập luôn sau khi đăng ký
        HttpContext.Session.SetString("ChuNuoiId",   cn.MaCn.ToString());
        HttpContext.Session.SetString("ChuNuoiName", cn.HoTen);

        TempData["Success"] = $"Chào mừng {cn.HoTen}! Hãy thêm thú cưng của bạn.";
        // Tạm thời điều hướng sang Home, sau đổi thành "ChuNuoiPortal" khi có giao diện
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