using Microsoft.AspNetCore.Mvc;

namespace _3TLMiniSoccer.Controllers
{
    public class AboutController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
