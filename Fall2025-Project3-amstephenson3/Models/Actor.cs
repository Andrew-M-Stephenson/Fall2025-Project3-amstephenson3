using System.ComponentModel.DataAnnotations;

namespace Fall2025_Project3_amstephenson3.Models
{
    public class Actor
    {
        public int Id { get; set; }

        [Required, StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(30)]
        public string? Gender { get; set; }

        [Range(0, 150)]
        public int? Age { get; set; }

        [Display(Name = "IMDB URL"), Url]
        public string? ImdbUrl { get; set; }

        // Photo stored in DB
        public byte[]? Photo { get; set; }
        public string? PhotoContentType { get; set; }

        public ICollection<ActorMovie> ActorMovies { get; set; } = new List<ActorMovie>();
    }
}
