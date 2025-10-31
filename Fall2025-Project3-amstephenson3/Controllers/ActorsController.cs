using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authorization;
using VaderSharp2;
using Fall2025_Project3_amstephenson3.Data;
using Fall2025_Project3_amstephenson3.Models;
using Fall2025_Project3_amstephenson3.Services;

namespace Fall2025_Project3_amstephenson3.Controllers
{
    public class ActorsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AiTweetsService _aiTweets;

        public ActorsController(ApplicationDbContext context, AiTweetsService aiTweets)
        {
            _context = context;
            _aiTweets = aiTweets;
        }

        //GET:
        public async Task<IActionResult> Index()
        {
            var actors = await _context.Actors.AsNoTracking().ToListAsync();
            return View(actors);
        }

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var actor = await _context.Actors
                .Include(a => a.ActorMovies)
                    .ThenInclude(am => am.Movie)
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (actor == null) return NotFound();

            //Generate tweets
            var movieTitles = actor.ActorMovies.Select(am => am.Movie.Title);
            var tweets = await _aiTweets.GenerateTweetsAsync(actor.Name, movieTitles);

            var analyzer = new SentimentIntensityAnalyzer();
            var scored = tweets.Select(t =>
            {
                var s = analyzer.PolarityScores(t);
                string label = s.Compound >= 0.05 ? "Positive"
                             : s.Compound <= -0.05 ? "Negative"
                             : "Neutral";
                return new TweetWithSentiment
                {
                    Text = t,
                    Compound = System.Math.Round(s.Compound, 4),
                    Label = label
                };
            }).ToList();

            double average = scored.Any() ? scored.Average(x => x.Compound) : 0.0;
            string overallLabel = average >= 0.05 ? "Positive"
                                  : average <= -0.05 ? "Negative"
                                  : "Neutral";

            var vm = new ActorDetailsViewModel
            {
                Actor = actor,
                Movies = actor.ActorMovies.Select(am => am.Movie).OrderBy(m => m.Title).ToList(),
                Tweets = scored,
                AverageCompound = System.Math.Round(average, 4),
                OverallLabel = overallLabel
            };

            return View(vm);
        }

        //GET:
        public IActionResult Create() => View();

        //POST:
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Gender,Age,ImdbUrl")] Actor actor, IFormFile? photoFile)
        {
            if (photoFile is not null && photoFile.Length > 0)
            {
                using var ms = new System.IO.MemoryStream();
                await photoFile.CopyToAsync(ms);
                actor.Photo = ms.ToArray();
                actor.PhotoContentType = photoFile.ContentType;
            }

            if (!ModelState.IsValid) return View(actor);

            _context.Add(actor);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        //GET:
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();
            var actor = await _context.Actors.FindAsync(id);
            if (actor == null) return NotFound();
            return View(actor);
        }

        //POST:
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Gender,Age,ImdbUrl")] Actor input, IFormFile? photoFile)
        {
            if (id != input.Id) return NotFound();

            var actor = await _context.Actors.FindAsync(id);
            if (actor == null) return NotFound();

            if (!ModelState.IsValid) return View(input);

            actor.Name = input.Name;
            actor.Gender = input.Gender;
            actor.Age = input.Age;
            actor.ImdbUrl = input.ImdbUrl;

            if (photoFile is not null && photoFile.Length > 0)
            {
                using var ms = new System.IO.MemoryStream();
                await photoFile.CopyToAsync(ms);
                actor.Photo = ms.ToArray();
                actor.PhotoContentType = photoFile.ContentType;
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        //GET:
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var actor = await _context.Actors.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
            if (actor == null) return NotFound();

            return View(actor);
        }

        //POST:
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var actor = await _context.Actors.FindAsync(id);
            if (actor != null)
            {
                _context.Actors.Remove(actor);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        //actor photo
        [AllowAnonymous]
        public async Task<IActionResult> Photo(int id)
        {
            var actor = await _context.Actors.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id);
            if (actor?.Photo is null || string.IsNullOrEmpty(actor.PhotoContentType))
                return NotFound();

            return File(actor.Photo, actor.PhotoContentType);
        }
    }

    //ViewModel used by Details
    public class ActorDetailsViewModel
    {
        public Actor Actor { get; set; } = default!;
        public List<Movie> Movies { get; set; } = new();
        public List<TweetWithSentiment> Tweets { get; set; } = new();
        public double AverageCompound { get; set; }
        public string OverallLabel { get; set; } = "Neutral";
    }

    public class TweetWithSentiment
    {
        public string Text { get; set; } = string.Empty;
        public double Compound { get; set; }
        public string Label { get; set; } = "Neutral";
    }
}
