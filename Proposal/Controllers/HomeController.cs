using Microsoft.AspNetCore.Mvc;
using Proposal.Models;
using System.Diagnostics;

namespace Proposal.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
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


        public IActionResult NotFoundPage()
        {
            return View(); // 你可以自定義一個漂亮的 Error404.cshtml
        }
    }
}
