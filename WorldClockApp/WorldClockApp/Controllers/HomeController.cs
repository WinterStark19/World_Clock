using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using WorldClockApp.Models;

namespace WorldClockApp.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Index(string location)
        {
            ViewBag.Location = location;
            return View();
        }

    }
}
