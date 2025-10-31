using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Fall2025_Project3_amstephenson3.Data;
using Fall2025_Project3_amstephenson3.Models;

namespace Fall2025_Project3_amstephenson3.Controllers
{
    public class ActorMoviesController : Controller
    {
        private readonly ApplicationDbContext _context;
        public ActorMoviesController(ApplicationDbContext context) => _context = context;

        //GET:
        public async Task<IActionResult> Index()
        {
            var rows = await _context.ActorMovies
                .AsNoTracking()
                .Select(am => new ActorMovieRowVM
                {
                    ActorId = am.ActorId,
                    MovieId = am.MovieId,
                    ActorName = am.Actor.Name,
                    MovieTitle = am.Movie.Title
                })
                .OrderBy(r => r.ActorName)
                .ThenBy(r => r.MovieTitle)
                .ToListAsync();

            return View(rows);
        }

        public async Task<IActionResult> Details(int actorId, int movieId)
        {
            var vm = await _context.ActorMovies
                .AsNoTracking()
                .Where(x => x.ActorId == actorId && x.MovieId == movieId)
                .Select(x => new ActorMovieRowVM
                {
                    ActorId = x.ActorId,
                    MovieId = x.MovieId,
                    ActorName = x.Actor.Name,
                    MovieTitle = x.Movie.Title
                })
                .FirstOrDefaultAsync();

            if (vm == null) return NotFound();
            return View(vm);
        }

        public async Task<IActionResult> Create()
        {
            await LoadDropdowns();
            return View(new ActorMovie());
        }

        //POST:
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] int ActorId, [FromForm] int MovieId)
        {
            var model = new ActorMovie { ActorId = ActorId, MovieId = MovieId };

            await ValidatePair(ActorId, MovieId);
            if (!ModelState.IsValid)
            {
                await LoadDropdowns(ActorId, MovieId);
                return View(model);
            }

            _context.ActorMovies.Add(model);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        //GET:
        public async Task<IActionResult> Edit(int actorId, int movieId)
        {
            var exists = await _context.ActorMovies
                .AsNoTracking()
                .AnyAsync(x => x.ActorId == actorId && x.MovieId == movieId);
            if (!exists) return NotFound();

            await LoadDropdowns(actorId, movieId);
            return View(new ActorMovieEditVM
            {
                OriginalActorId = actorId,
                OriginalMovieId = movieId,
                ActorId = actorId,
                MovieId = movieId
            });
        }

        //POST:
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit([FromForm] ActorMovieEditVM vm)
        {
            await ValidatePair(vm.ActorId, vm.MovieId, vm.OriginalActorId, vm.OriginalMovieId);
            if (!ModelState.IsValid)
            {
                await LoadDropdowns(vm.ActorId, vm.MovieId);
                return View(vm);
            }

            if (vm.ActorId == vm.OriginalActorId && vm.MovieId == vm.OriginalMovieId)
                return RedirectToAction(nameof(Index));

            var old = await _context.ActorMovies.FindAsync(vm.OriginalActorId, vm.OriginalMovieId);
            if (old != null) _context.ActorMovies.Remove(old);

            _context.ActorMovies.Add(new ActorMovie { ActorId = vm.ActorId, MovieId = vm.MovieId });
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        //GET:
        public async Task<IActionResult> Delete(int actorId, int movieId)
        {
            var row = await _context.ActorMovies
                .AsNoTracking()
                .Where(x => x.ActorId == actorId && x.MovieId == movieId)
                .Select(x => new ActorMovieRowVM
                {
                    ActorId = x.ActorId,
                    MovieId = x.MovieId,
                    ActorName = x.Actor.Name,
                    MovieTitle = x.Movie.Title
                })
                .FirstOrDefaultAsync();

            if (row == null) return NotFound();
            return View(row);
        }

        //POST:
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int actorId, int movieId)
        {
            var am = await _context.ActorMovies.FindAsync(actorId, movieId);
            if (am != null)
            {
                _context.ActorMovies.Remove(am);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        //helpers

        private async Task ValidatePair(int actorId, int movieId, int? originalActorId = null, int? originalMovieId = null)
        {
            if (actorId <= 0) ModelState.AddModelError(nameof(ActorMovie.ActorId), "Choose an actor.");
            if (movieId <= 0) ModelState.AddModelError(nameof(ActorMovie.MovieId), "Choose a movie.");

            if (!await _context.Actors.AnyAsync(a => a.Id == actorId))
                ModelState.AddModelError(nameof(ActorMovie.ActorId), "Actor not found.");
            if (!await _context.Movies.AnyAsync(m => m.Id == movieId))
                ModelState.AddModelError(nameof(ActorMovie.MovieId), "Movie not found.");

            var isSameAsOriginal = originalActorId.HasValue && originalMovieId.HasValue &&
                                   actorId == originalActorId.Value && movieId == originalMovieId.Value;

            if (!isSameAsOriginal)
            {
                bool exists = await _context.ActorMovies.AnyAsync(am => am.ActorId == actorId && am.MovieId == movieId);
                if (exists) ModelState.AddModelError(string.Empty, "This relationship already exists.");
            }
        }

        private async Task LoadDropdowns(int? selectedActorId = null, int? selectedMovieId = null)
        {
            var actors = await _context.Actors.AsNoTracking().OrderBy(a => a.Name).ToListAsync();
            var movies = await _context.Movies.AsNoTracking().OrderBy(m => m.Title).ToListAsync();

            ViewData["ActorId"] = new SelectList(actors, "Id", "Name", selectedActorId ?? actors.FirstOrDefault()?.Id);
            ViewData["MovieId"] = new SelectList(movies, "Id", "Title", selectedMovieId ?? movies.FirstOrDefault()?.Id);
        }
    }

    //Row view model for Index/Details/Delete
    public class ActorMovieRowVM
    {
        public int ActorId { get; set; }
        public int MovieId { get; set; }
        public string ActorName { get; set; } = string.Empty;
        public string MovieTitle { get; set; } = string.Empty;
    }

    //Edit view model
    public class ActorMovieEditVM
    {
        public int OriginalActorId { get; set; }
        public int OriginalMovieId { get; set; }

        public int ActorId { get; set; }
        public int MovieId { get; set; }
    }
}
