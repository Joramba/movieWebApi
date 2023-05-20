using Microsoft.EntityFrameworkCore;
using MySql.EntityFrameworkCore.Extensions;

namespace lab2
{
    public class Movie
    {
        public int MovieID { get; set; }
        public string? Title { get; set; }
        public virtual ICollection<Genre>? Genres { get; set; }
    }
    public class Genre
    {
        public int GenreID { get; set; }
        public string? Name { get; set; }
        public virtual ICollection<Movie>? Movies { get; set; }
    }
    public class User
    {
        public int UserID { get; set; }
        public string? Name { get; set; }
    }
    public class Rating
    {
        public int RatingID { get; set; }
        public int RatingValue { get; set; }
        public virtual Movie? RatedMovie { get; set; }
        public virtual User? RatingUser { get; set; }
    }
}
