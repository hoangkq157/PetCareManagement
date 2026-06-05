using Microsoft.AspNetCore.Mvc;

namespace PetCareManagement.Controllers // Thay PetCareManagement bằng tên project của bạn
{
    public class ChuNuoiController : Controller
    {
        // GET: /ChuNuoi/ hoặc /ChuNuoi/Index
        public IActionResult Index()
        {
            // mẫu thử hiển thị view 
            return View();
        }
    }
}