using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Fall2025_Project3_amstephenson3.Data;
using Fall2025_Project3_amstephenson3.Models;
using Fall2025_Project3_amstephenson3.Models.ViewModels;
using Fall2025_Project3_amstephenson3.Services;
using VaderSharp2;

namespace Fall2025_Project3_amstephenson3.Controllers
{
    public class MoviesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly AiReviewsService _aiReviews;

        public MoviesController(ApplicationDbContext context, AiReviewsService aiReviews)
        {
            _context = context;
            _aiReviews = aiReviews;
        }

        // GET: Movies
        public async Task<IActionResult> Index()
        {
            var movies = await _context.Movies.AsNoTracking().ToListAsync();
            return View(movies);
        }

        // GET: Movies/Details/5
        // Builds MovieDetailsViewModel, calls Azure OpenAI once for 10 reviews, and scores with VaderSharp2
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var movie = await _context.Movies
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (movie == null) return NotFound();

            // Cast (actor names) via join table
            var cast = await _context.ActorMovies
                .Where(am => am.MovieId == movie.Id)
                .Include(am => am.Actor)
                .Select(am => am.Actor.Name)
                .ToListAsync();

            // Call AI once to get 10 mini-reviews (robust service already handles JSON parsing)
            var reviews = await _aiReviews.GenerateReviewsAsync(movie.Title, cast);

            // Score with VaderSharp2
            var analyzer = new SentimentIntensityAnalyzer();
            var scored = reviews
                .Select(text =>
                {
                    var s = analyzer.PolarityScores(text);
                    var label = s.Compound switch
                    {
                        > 0.05 => "Positive",
                        < -0.05 => "Negative",
                        _ => "Neutral"
                    };
                    return new ReviewWithSentiment
                    {
                        Text = text,
                        Compound = Math.Round(s.Compound, 4),
                        Label = label
                    };
                })
                .ToList();

            var avg = scored.Any() ? scored.Average(x => x.Compound) : 0.0;
            var overall = avg switch
            {
                > 0.05 => "Positive",
                < -0.05 => "Negative",
                _ => "Neutral"
            };

            var vm = new MovieDetailsViewModel
            {
                Movie = movie,
                Actors = cast,
                Reviews = scored,
                AverageCompound = Math.Round(avg, 4),
                OverallLabel = overall
            };

            return View(vm); // View expects MovieDetailsViewModel (your updated Details.cshtml)
        }

        // GET: Movies/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Movies/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            [Bind("Title,ImdbUrl,Genre,Year")] Movie movie,
            IFormFile? posterFile)
        {
            // Handle poster upload
            if (posterFile is not null && posterFile.Length > 0)
            {
                using var ms = new System.IO.MemoryStream();
                await posterFile.CopyToAsync(ms);
                movie.Poster = ms.ToArray();
                movie.PosterContentType = posterFile.ContentType;
            }

            if (!ModelState.IsValid)
            {
                return View(movie);
            }

            _context.Add(movie);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Movies/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var movie = await _context.Movies.FindAsync(id);
            if (movie == null) return NotFound();

            return View(movie);
        }

        // POST: Movies/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            int id,
            [Bind("Id,Title,ImdbUrl,Genre,Year")] Movie input,
            IFormFile? posterFile)
        {
            if (id != input.Id) return NotFound();

            var movie = await _context.Movies.FindAsync(id);
            if (movie is null) return NotFound();

            if (!ModelState.IsValid)
            {
                return View(input);
            }

            // Update scalar fields
            movie.Title = input.Title;
            movie.ImdbUrl = input.ImdbUrl;
            movie.Genre = input.Genre;
            movie.Year = input.Year;

            // Optional new poster
            if (posterFile is not null && posterFile.Length > 0)
            {
                using var ms = new System.IO.MemoryStream();
                await posterFile.CopyToAsync(ms);
                movie.Poster = ms.ToArray();
                movie.PosterContentType = posterFile.ContentType;
            }

            try
            {
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!await _context.Movies.AnyAsync(e => e.Id == id))
                    return NotFound();
                throw;
            }
        }

        // GET: Movies/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var movie = await _context.Movies
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == id);

            if (movie == null) return NotFound();

            return View(movie);
        }

        // POST: Movies/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var movie = await _context.Movies.FindAsync(id);
            if (movie != null)
            {
                _context.Movies.Remove(movie);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // Stream the poster bytes
        [AllowAnonymous]
        public async Task<IActionResult> Poster(int id)
        {
            var movie = await _context.Movies.AsNoTracking().FirstOrDefaultAsync(m => m.Id == id);
            if (movie?.Poster is null || string.IsNullOrEmpty(movie.PosterContentType))
                return NotFound();

            return File(movie.Poster, movie.PosterContentType);
        }
    }
}
