using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PetCareManagement.Data;
using PetCareManagement.Models;

namespace PetCareManagement.Controllers;

// GHI CHÚ: File này dùng cho chức năng quản lý dịch vụ.
// Chức năng chính: thêm, sửa, xoá và tìm kiếm dịch vụ.
// File nằm trong thư mục Controllers, được dùng bởi view Dịch vụ.

public class DichVuController : Controller
{
    // Khai báo DbContext để truy cập bảng DichVu trong database.
    private readonly PetCareDbContext _context;

    // Inject DbContext vào controller khi khởi tạo.
    public DichVuController(PetCareDbContext context)
    {
        _context = context;
    }

    // Hàm kiểm tra người dùng đã đăng nhập hay chưa.
    private bool DaDangNhap() => HttpContext.Session.GetString("NhanVienId") != null;

    // Action hiển thị danh sách dịch vụ, có thể lọc theo từ khoá tìm kiếm.
    public async Task<IActionResult> Index(string? search)
    {
        // Nếu chưa đăng nhập thì chuyển về màn hình đăng nhập.
        if (!DaDangNhap()) return RedirectToAction("Login", "Auth");

        // Tạo truy vấn lấy dữ liệu từ bảng DichVus.
        var query = _context.DichVus.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(d =>
                d.TenDichVu.Contains(search) ||
                (d.DanhMuc != null && d.DanhMuc.Contains(search)));
        }

        ViewBag.Search = search;
        return View(await query.OrderBy(d => d.TenDichVu).ToListAsync());
    }

    public IActionResult Create()
    {
        if (!DaDangNhap()) return RedirectToAction("Login", "Auth");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(DichVu dichVu)
    {
        if (!DaDangNhap()) return RedirectToAction("Login", "Auth");

        if (!ModelState.IsValid)
            return View(dichVu);

        _context.Add(dichVu);
        await _context.SaveChangesAsync();
        TempData["Success"] = $"Đã thêm dịch vụ '{dichVu.TenDichVu}'";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        if (!DaDangNhap()) return RedirectToAction("Login", "Auth");

        var dichVu = await _context.DichVus.FindAsync(id);
        if (dichVu == null) return NotFound();

        return View(dichVu);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, DichVu dichVu)
    {
        if (!DaDangNhap()) return RedirectToAction("Login", "Auth");
        if (id != dichVu.MaDv) return BadRequest();

        if (!ModelState.IsValid) return View(dichVu);

        try
        {
            _context.Update(dichVu);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã cập nhật dịch vụ '{dichVu.TenDichVu}'";
            return RedirectToAction(nameof(Index));
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!await _context.DichVus.AnyAsync(d => d.MaDv == id)) return NotFound();
            throw;
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        if (!DaDangNhap()) return RedirectToAction("Login", "Auth");

        var dichVu = await _context.DichVus.FindAsync(id);
        if (dichVu != null)
        {
            _context.Remove(dichVu);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Đã xoá dịch vụ '{dichVu.TenDichVu}'";
        }

        return RedirectToAction(nameof(Index));
    }
}
