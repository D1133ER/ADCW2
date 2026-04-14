using Microsoft.AspNetCore.Mvc;

namespace WeblogApplication.Controllers
{
    public class HomeController : Controller
    {
        // Redirect to Blog Index since that is the default landing page
        public IActionResult Index()
        {
            return RedirectToAction("Index", "Blog");
        }
    }
}
