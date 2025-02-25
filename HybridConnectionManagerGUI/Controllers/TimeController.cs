using HybridConnectionManager.Library;
using Microsoft.AspNetCore.Mvc;


namespace HybridConnectionManagerGUI.Controllers
{
    public class TimeController : Controller
    {
        public IActionResult Index()
        {
            ViewBag.CurrentTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss");
            ViewBag.UserLogin = Environment.UserName;
            return View();
        }

        [HttpGet]
        public IActionResult GetTimeData()
        {
            return Json(new
            {
                CurrentTime = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                UserLogin = Environment.UserName
            });
        }
    }
}
