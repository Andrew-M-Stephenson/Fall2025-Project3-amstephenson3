using Fall2025_Project3_amstephenson3.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Fall2025_Project3_amstephenson3.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Movie> Movies => Set<Movie>();
        public DbSet<Actor> Actors => Set<Actor>();
        public DbSet<ActorMovie> ActorMovies => Set<ActorMovie>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            //composite PK for join/no duplicates
            builder.Entity<ActorMovie>()
                .HasKey(am => new { am.ActorId, am.MovieId });

            builder.Entity<ActorMovie>()
                .HasOne(am => am.Actor)
                .WithMany(a => a.ActorMovies)
                .HasForeignKey(am => am.ActorId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ActorMovie>()
                .HasOne(am => am.Movie)
                .WithMany(m => m.ActorMovies)
                .HasForeignKey(am => am.MovieId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
