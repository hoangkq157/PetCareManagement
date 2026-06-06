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
        // Nếu NhanVien đã đăng nhập → về trang quản lý
        if (HttpContext.Session.GetString("NhanVienId") != null)
            return RedirectToAction("Index", "Home");

        // Nếu ChuNuoi đã đăng nhập → về trang Home (tạm thời)
        if (HttpContext.Session.GetString("ChuNuoiId") != null)
            return RedirectToAction("Index", "ChuNuoi");

        return View();
    }

    // POST /Auth/Login
    // Trường "taiKhoan": NhanVien nhập Email, ChuNuoi nhập Số điện thoại
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(string taiKhoan, string matKhau)
    {
        if (string.IsNullOrWhiteSpace(taiKhoan) || string.IsNullOrWhiteSpace(matKhau))
        {
            ViewBag.Error = "Vui lòng nhập đầy đủ tài khoản và mật khẩu.";
            return View();
        }

        // ── Bước 1: Thử đăng nhập NhanVien (bằng Email) ──────────────────
        var nv = await _context.NhanViens
            .FirstOrDefaultAsync(n => n.Email == taiKhoan
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

        // ── Bước 2: Thử đăng nhập ChuNuoi (bằng Số điện thoại) ──────────
        var cn = await _context.ChuNuois
            .FirstOrDefaultAsync(c => c.SoDienThoai == taiKhoan
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

    // GET /Auth/Register
    public IActionResult Register()
    {
        // Nếu ChuNuoi đã đăng nhập → về Home (tạm thời)
        if (HttpContext.Session.GetString("ChuNuoiId") != null)
            return RedirectToAction("Index", "Home");

        // Nếu NhanVien đã đăng nhập → về trang quản lý
        if (HttpContext.Session.GetString("NhanVienId") != null)
            return RedirectToAction("Index", "Home");

        return View();
    }

    // POST /Auth/Register
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
        void GiuLai()
        {
            ViewBag.HoTen        = hoTen;
            ViewBag.SoDienThoai  = soDienThoai;
            ViewBag.Email        = email;
            ViewBag.DiaChi       = diaChi;
        }

        if (string.IsNullOrWhiteSpace(hoTen) ||
            string.IsNullOrWhiteSpace(soDienThoai) ||
            string.IsNullOrWhiteSpace(matKhau))
        { ViewBag.Error = "Vui lòng điền đầy đủ các trường bắt buộc."; GiuLai(); return View(); }

        if (matKhau.Length < 6)
        { ViewBag.Error = "Mật khẩu phải có ít nhất 6 ký tự."; GiuLai(); return View(); }

        if (matKhau != xacNhanMatKhau)
        { ViewBag.Error = "Mật khẩu xác nhận không khớp."; GiuLai(); return View(); }

        if (await _context.ChuNuois.AnyAsync(c => c.SoDienThoai == soDienThoai))
        { ViewBag.Error = "Số điện thoại này đã được đăng ký."; GiuLai(); return View(); }

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