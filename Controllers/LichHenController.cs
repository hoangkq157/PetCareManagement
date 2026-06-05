using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using PetCareManagement.Data;
using PetCareManagement.Models;

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
        public async Task<IActionResult> Index(string? trangThai, DateOnly? ngay)
        {
            var query = _context.LichHens
                .Include(l => l.MaTcNavigation)          // load Thú cưng
                    .ThenInclude(t => t.MaCnNavigation)   // load Chủ nuôi qua Thú cưng
                .Include(l => l.MaNvNavigation)          // load Nhân viên
                .AsQueryable();

            // Lọc theo trạng thái nếu có
            if (!string.IsNullOrEmpty(trangThai))
                query = query.Where(l => l.TrangThai == trangThai);

            // Lọc theo ngày nếu có
            if (ngay.HasValue)
                query = query.Where(l => l.NgayHen == ngay.Value);

            // Gửi danh sách trạng thái cho dropdown lọc
            ViewBag.DanhSachTrangThai = new List<string>
                { "ChoDuyet", "XacNhan", "HoanThanh", "Huy" };

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
            return View();
        }

        // POST: LichHen/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LichHen lichHen)
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
                    ModelState.AddModelError("",
                        "Nhân viên này đã có lịch vào khung giờ đó.");
                }
                else
                {
                    _context.Add(lichHen);
                    await _context.SaveChangesAsync();

                    TempData["Success"] =
                        "Đặt lịch thành công!";

                    return RedirectToAction(nameof(Index));
                }
            }

            ViewData["MaNv"] = new SelectList(
                _context.NhanViens,
                "MaNv",
                "HoTen",
                lichHen.MaNv);

            ViewData["MaTc"] = new SelectList(
                _context.ThuCungs,
                "MaTc",
                "TenThuCung",
                lichHen.MaTc);

            return View(lichHen);
        }
        // DoiTrangThai cap nhat trang thai lich hen ma khong can vao trang edit
        [HttpPost]
        public async Task<IActionResult> DoiTrangThai(int id, string trangThai)
        {
            var lichHen = await _context.LichHens.FindAsync(id);
            if (lichHen == null) return NotFound();

            lichHen.TrangThai = trangThai;
            _context.Update(lichHen);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        // GET: LichHen/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var lichHen = await _context.LichHens.FindAsync(id);
            if (lichHen == null)
            {
                return NotFound();
            }
            ViewData["MaNv"] = new SelectList(_context.NhanViens, "MaNv", "HoTen", lichHen.MaNv);
            ViewData["MaTc"] = new SelectList(_context.ThuCungs, "MaTc", "TenThuCung", lichHen.MaTc);
            return View(lichHen);
        }

        // POST: LichHen/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("MaLh,MaTc,MaNv,NgayHen,GioHen,TrangThai,GhiChu,NgayTao")] LichHen lichHen)
        {
            if (id != lichHen.MaLh)
            {
                return NotFound();
            }

            ModelState.Remove(nameof(LichHen.MaTcNavigation));
            ModelState.Remove(nameof(LichHen.MaNvNavigation));

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(lichHen);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!LichHenExists(lichHen.MaLh))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            ViewData["MaNv"] = new SelectList(
                                            _context.NhanViens,
                                            "MaNv",
                                            "HoTen",
                                            lichHen.MaNv);

            ViewData["MaTc"] = new SelectList(
                                            _context.ThuCungs,
                                            "MaTc",
                                            "TenThuCung",
                                            lichHen.MaTc);

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
