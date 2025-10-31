//Models/ViewModels/MovieDetailsViewModel.cs
using Fall2025_Project3_amstephenson3.Models;

namespace Fall2025_Project3_amstephenson3.Models.ViewModels
{
    public class ReviewWithSentiment
    {
        public string Text { get; set; } = string.Empty;
        public double Compound { get; set; }
        public string Label { get; set; } = "Neutral";
    }

    public class MovieDetailsViewModel
    {
        public Movie Movie { get; set; } = default!;
        public List<string> Actors { get; set; } = new();
        public List<ReviewWithSentiment> Reviews { get; set; } = new();
        public double AverageCompound { get; set; }
        public string OverallLabel { get; set; } = "Neutral";
    }
}
