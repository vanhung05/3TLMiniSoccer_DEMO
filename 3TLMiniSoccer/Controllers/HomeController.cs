using System.Diagnostics;
using _3TLMiniSoccer.Models;
using _3TLMiniSoccer.Services;
using _3TLMiniSoccer.ViewModels;
using _3TLMiniSoccer.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace _3TLMiniSoccer.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly FieldService _fieldService;
        private readonly BookingService _bookingService;
        public HomeController(
            ILogger<HomeController> logger,
            FieldService fieldService,
            BookingService bookingService)
        {
            _logger = logger;
            _fieldService = fieldService;
            _bookingService = bookingService;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = new HomeIndexViewModel
            {
                Fields = await _fieldService.LayTatCaSanAsync(),
                FeaturedProducts = new List<Product>(), // Empty list since ProductService was removed
                TotalBookings = await _bookingService.LayTongSoDatSanAsync(),
                TotalFields = await _fieldService.LayTongSoSanAsync()
            };

            return View(viewModel);
        }


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
