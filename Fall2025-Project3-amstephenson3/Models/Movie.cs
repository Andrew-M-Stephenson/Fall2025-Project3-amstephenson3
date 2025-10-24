using System.ComponentModel.DataAnnotations;

namespace Fall2025_Project3_amstephenson3.Models
{
    public class Movie
    {
        public int Id { get; set; }

        [Required, StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Display(Name = "IMDB URL"), Url]
        public string? ImdbUrl { get; set; }

        [StringLength(100)]
        public string? Genre { get; set; }

        [Range(1888, 2100)]
        [Display(Name = "Year of Release")]
        public int? Year { get; set; }

        // Poster stored in DB as bytes
        public byte[]? Poster { get; set; }

        // Optional: store content type so you can serve it back correctly
        public string? PosterContentType { get; set; }

        public ICollection<ActorMovie> ActorMovies { get; set; } = new List<ActorMovie>();

    }
}
