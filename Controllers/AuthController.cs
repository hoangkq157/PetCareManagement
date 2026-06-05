using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetCareManagement.Data;
using PetCareManagement.Models;

namespace PetCareManagement.Controllers;

public class AuthController : Controller
{
    private readonly PetCareDbContext _context;

    public AuthController(PetCareDbContext context)
    {
        _context = context;
    }

    // ─────────────────────────────────────────────
    // ĐĂNG NHẬP
    // ─────────────────────────────────────────────

    // GET /Auth/Login
    public IActionResult Login()
    {
        if (HttpContext.Session.GetString("NhanVienId") != null)
            return RedirectToAction("Index", "Home");
        return View();
    }

    // POST /Auth/Login
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

    // GET /Auth/Register
    public IActionResult Register()
    {
        if (HttpContext.Session.GetString("NhanVienId") != null)
            return RedirectToAction("Index", "Home");
        return View();
    }

    // POST /Auth/Register
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(
        string hoTen,
        string email,
        string matKhau,
        string xacNhanMatKhau,
        string? soDienThoai)
    {
        // Giữ lại giá trị đã nhập khi có lỗi
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

        // Kiểm tra email trùng
        if (await _context.NhanViens.AnyAsync(n => n.Email == email))
        {
            ViewBag.Error = "Email này đã được sử dụng. Vui lòng chọn email khác.";
            GiuLai(); return View();
        }

        // Lưu vào database
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

        // Đăng nhập luôn sau khi đăng ký
        HttpContext.Session.SetString("NhanVienId",   nv.MaNv.ToString());
        HttpContext.Session.SetString("NhanVienName", nv.HoTen);
        HttpContext.Session.SetString("NhanVienRole", nv.VaiTro);

        TempData["Success"] = $"Chào mừng {nv.HoTen} đã tham gia PetCare!";
        return RedirectToAction("Index", "Home");
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