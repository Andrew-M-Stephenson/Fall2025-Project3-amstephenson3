using Microsoft.AspNetCore.Mvc;
using Fall2025_Project3_amstephenson3.Services;

namespace Fall2025_Project3_amstephenson3.Controllers
{
    public class HomeController : Controller
    {
        private readonly AiTweetsService _ai;
        public HomeController(AiTweetsService ai) => _ai = ai;

        // GET /Home/TestAi?name=Emma%20Stone
        [HttpGet]
        public async Task<IActionResult> TestAi(string name = "Tom Hanks")
        {
            var tweets = await _ai.GenerateTweetsAsync(name);
            return Content(string.Join("\n- ", tweets.Select(t => "- " + t)));
        }

        public IActionResult Index() => View();
    }
}
