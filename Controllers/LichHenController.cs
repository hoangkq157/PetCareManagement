using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PetCareManagement.Data;
using PetCareManagement.Models;
using Microsoft.AspNetCore.Http;

namespace PetCareManagement.Controllers
{
    public class LichHenController : Controller
    {
        private readonly PetCareDbContext _context;

        public LichHenController(PetCareDbContext context)
        {
            _context = context;
        }

        // GET: LichHen
        public async Task<IActionResult> Index(string? trangThai, DateOnly? ngay, string? searchTenChuNuoi)
        {
            var query = _context.LichHens
                .Include(l => l.MaTcNavigation)          // load Thú cưng
                    .ThenInclude(t => t.MaCnNavigation)   // load Chủ nuôi qua Thú cưng
                .Include(l => l.MaNvNavigation)          // load Nhân viên
                .Include(l => l.LichHenDichVus)          // load danh sách dịch vụ của lịch hẹn (MỚI THÊM)
                    .ThenInclude(ld => ld.MaDvNavigation) // load chi tiết tên dịch vụ (MỚI THÊM)
                .AsQueryable();

            // Lọc theo trạng thái nếu có
            if (!string.IsNullOrEmpty(trangThai))
                query = query.Where(l => l.TrangThai == trangThai);

            // Lọc theo ngày nếu có
            if (ngay.HasValue)
                query = query.Where(l => l.NgayHen == ngay.Value);

            // Lọc tìm kiếm theo tên chủ nuôi nếu có (MỚI THÊM)
            if (!string.IsNullOrEmpty(searchTenChuNuoi))
            {
                query = query.Where(l => l.MaTcNavigation.MaCnNavigation.HoTen.Contains(searchTenChuNuoi));
            }

            // Gửi danh sách trạng thái cho dropdown lọc
            ViewBag.DanhSachTrangThai = new List<string>
                { "ChoDuyet", "XacNhan", "HoanThanh", "Huy" };

            // Giữ lại từ khóa tìm kiếm để hiển thị lên Form (MỚI THÊM)
            ViewBag.SearchTenChuNuoi = searchTenChuNuoi;

            return View(await query
                .OrderBy(l => l.NgayHen).ThenBy(l => l.GioHen)
                .ToListAsync());
        }

        // GET: LichHen/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var lichHen = await _context.LichHens
                .Include(l => l.MaNvNavigation)
                .Include(l => l.MaTcNavigation)
                .Include(l => l.LichHenDichVus)
                    .ThenInclude(ld => ld.MaDvNavigation)
                .FirstOrDefaultAsync(m => m.MaLh == id);
            if (lichHen == null)
            {
                return NotFound();
            }

            return View(lichHen);
        }

        // GET: LichHen/Create
        public IActionResult Create()
        {
            ViewData["MaNv"] = new SelectList(_context.NhanViens, "MaNv", "HoTen");
            ViewData["MaTc"] = new SelectList(_context.ThuCungs, "MaTc", "TenThuCung");

            ViewBag.DichVus = _context.DichVus.Where(dv => dv.TrangThai == true).ToList();

            return View();
        }

        // POST: LichHen/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LichHen lichHen, int? maDichVu)  // <-- thêm int? maDichVu
        {
            lichHen.TrangThai = "ChoDuyet";
            lichHen.NgayTao = DateTime.Now;

            ModelState.Remove(nameof(LichHen.TrangThai));
            ModelState.Remove(nameof(LichHen.NgayTao));
            ModelState.Remove(nameof(LichHen.MaTcNavigation));
            ModelState.Remove(nameof(LichHen.MaNvNavigation));

            if (ModelState.IsValid)
            {
                bool trungLich = await _context.LichHens.AnyAsync(l =>
                    l.MaNv == lichHen.MaNv &&
                    l.NgayHen == lichHen.NgayHen &&
                    l.GioHen == lichHen.GioHen &&
                    l.TrangThai != "Huy");

                if (trungLich)
                {
                    ModelState.AddModelError("", "Nhân viên này đã có lịch vào khung giờ đó.");
                }
                else
                {
                    _context.Add(lichHen);
                    await _context.SaveChangesAsync();  // lưu LichHen trước để có MaLh

                    // ===== LƯU DỊCH VỤ (MỚI THÊM) =====
                    if (maDichVu.HasValue)
                    {
                        var dichVu = await _context.DichVus.FindAsync(maDichVu.Value);
                        if (dichVu != null)
                        {
                            // Lấy đơn giá (tạm dùng GiaCho; bạn có thể tuỳ chỉnh theo loại thú)
                            var chiTiet = new LichHenDichVu
                            {
                                MaLh   = lichHen.MaLh,
                                MaDv   = maDichVu.Value,
                                SoLuong = 1,
                                DonGia  = dichVu.GiaCho
                            };
                            _context.LichHenDichVus.Add(chiTiet);
                            await _context.SaveChangesAsync();
                        }
                    }
                    // ===== KẾT THÚC LƯU DỊCH VỤ =====

                    TempData["Success"] = "Đặt lịch thành công!";
                    return RedirectToAction(nameof(Index));
                }
            }

            ViewData["MaNv"] = new SelectList(_context.NhanViens, "MaNv", "HoTen", lichHen.MaNv);
            ViewData["MaTc"] = new SelectList(_context.ThuCungs, "MaTc", "TenThuCung", lichHen.MaTc);
            ViewBag.DichVus = _context.DichVus.Where(dv => dv.TrangThai == true).ToList();  // <-- giữ lại list khi lỗi

            return View(lichHen);
        }
        // DoiTrangThai cap nhat trang thai lich hen ma khong can vao trang edit
        [HttpPost]
        public async Task<IActionResult> DoiTrangThai(int id, string trangThai)
        {
            var lichHen = await _context.LichHens.FindAsync(id);
            if (lichHen == null) return NotFound();

            // 1. Cập nhật trạng thái (XacNhan hoặc Huy)
            lichHen.TrangThai = trangThai;

            // 2. Nếu bấm nút "Xác nhận" -> Lấy MaNV từ Session theo dạng String
            if (trangThai == "XacNhan")
            {
                // Lấy chuỗi ID từ Session (Ví dụ lấy ra chuỗi "1" hoặc "5")
                string maNvStr = HttpContext.Session.GetString("NhanVienId");

                // Kiểm tra xem chuỗi có dữ liệu không và tiến hành đổi sang kiểu số (int)
                if (!string.IsNullOrEmpty(maNvStr) && int.TryParse(maNvStr, out int maNvInt))
                {
                    lichHen.MaNv = maNvInt; // Gán số Id vừa đổi vào lịch hẹn
                }
                else
                {
                    // Trường hợp bấm nút nhưng Session bị mất (do lâu quá không bấm hoặc chưa đăng nhập)
                    TempData["Error"] = "Phiên làm việc đã hết hạn. Vui lòng đăng nhập lại để duyệt!";
                    return RedirectToAction(nameof(Index));
                }
            }

            _context.Update(lichHen);
            await _context.SaveChangesAsync();

            TempData["Success"] = trangThai == "XacNhan" ? "Đã duyệt và gán nhân viên thành công!" : "Đã hủy lịch hẹn.";
            return RedirectToAction(nameof(Index));
        }

       // GET: LichHen/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var lichHen = await _context.LichHens
                .Include(l => l.LichHenDichVus)   // <-- load dịch vụ hiện tại
                .FirstOrDefaultAsync(l => l.MaLh == id);

            if (lichHen == null) return NotFound();

            ViewData["MaNv"] = new SelectList(_context.NhanViens, "MaNv", "HoTen", lichHen.MaNv);
            ViewData["MaTc"] = new SelectList(_context.ThuCungs, "MaTc", "TenThuCung", lichHen.MaTc);
            ViewBag.DichVus  = _context.DichVus.Where(dv => dv.TrangThai == true).ToList(); // <-- danh sách dịch vụ

            // Lấy MaDv đang được chọn (nếu có) để pre-select dropdown
            ViewBag.MaDichVuHienTai = lichHen.LichHenDichVus.FirstOrDefault()?.MaDv;

            return View(lichHen);
        }

        // POST: LichHen/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id,
            [Bind("MaLh,MaTc,MaNv,NgayHen,GioHen,TrangThai,GhiChu,NgayTao")] LichHen lichHen,
            int? maDichVu)  // <-- thêm tham số dịch vụ
        {
            if (id != lichHen.MaLh) return NotFound();

            ModelState.Remove(nameof(LichHen.MaTcNavigation));
            ModelState.Remove(nameof(LichHen.MaNvNavigation));

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(lichHen);
                    await _context.SaveChangesAsync();

                    // Xoá dịch vụ cũ của lịch hẹn này
                    var dichVuCu = _context.LichHenDichVus.Where(ld => ld.MaLh == id);
                    _context.LichHenDichVus.RemoveRange(dichVuCu);

                    // Thêm dịch vụ mới nếu người dùng có chọn
                    if (maDichVu.HasValue)
                    {
                        var dichVu = await _context.DichVus.FindAsync(maDichVu.Value);
                        if (dichVu != null)
                        {
                            _context.LichHenDichVus.Add(new LichHenDichVu
                            {
                                MaLh    = id,
                                MaDv    = maDichVu.Value,
                                SoLuong = 1,
                                DonGia  = dichVu.GiaCho
                            });
                        }
                    }
                    await _context.SaveChangesAsync();

                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LichHenExists(lichHen.MaLh)) return NotFound();
                    else throw;
                }
            }

            ViewData["MaNv"] = new SelectList(_context.NhanViens, "MaNv", "HoTen", lichHen.MaNv);
            ViewData["MaTc"] = new SelectList(_context.ThuCungs, "MaTc", "TenThuCung", lichHen.MaTc);
            ViewBag.DichVus  = _context.DichVus.Where(dv => dv.TrangThai == true).ToList();
            ViewBag.MaDichVuHienTai = maDichVu;

            return View(lichHen);
        }

        // GET: LichHen/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var lichHen = await _context.LichHens
                .Include(l => l.MaNvNavigation)
                .Include(l => l.MaTcNavigation)
                .FirstOrDefaultAsync(m => m.MaLh == id);
            if (lichHen == null)
            {
                return NotFound();
            }

            return View(lichHen);
        }

        // POST: LichHen/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var lichHen = await _context.LichHens.FindAsync(id);
            if (lichHen != null)
            {
                _context.LichHens.Remove(lichHen);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool LichHenExists(int id)
        {
            return _context.LichHens.Any(e => e.MaLh == id);
        }
    }
}
