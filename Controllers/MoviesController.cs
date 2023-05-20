using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;


namespace lab2.Controllers;
[ApiController]
[Route("[controller]")]
public class MoviesController : ControllerBase
{
    [HttpPost("UploadMovieCsv")]
    public string Post(IFormFile inputFile)
    {
        var strm = inputFile.OpenReadStream();
        byte[] buffer = new byte[inputFile.Length];
        strm.Read(buffer, 0, (int)inputFile.Length);
        string fileContent = System.Text.Encoding.Default.GetString(buffer);
        strm.Close();
        MoviesContext dbContext = new MoviesContext();
        bool skip_header = true;
        foreach (string line in fileContent.Split('\n'))
        {
            if (skip_header)
            {
                skip_header = false;
                continue;

            }
            var tokens = line.Split(",");
            if (tokens.Length != 3) continue;
            string MovieID = tokens[0];
            string MovieName = tokens[1];
            string[] Genres = tokens[2].Split("|");
            List<Genre> movieGenres = new List<Genre>();
            foreach (string genre in Genres)
            {
                Genre g = new Genre();
                g.Name = genre.Trim();
                if (!dbContext.Genres.Any(e => e.Name == g.Name))
                {
                    dbContext.Genres.Add(g);
                    dbContext.SaveChanges();
                }
                IQueryable<Genre> results = dbContext.Genres.Where(e => e.Name == g.Name);
                if (results.Count() > 0)
                    movieGenres.Add(results.First());
            }
            Movie m = new Movie();
            m.MovieID = int.Parse(MovieID);
            m.Title = MovieName;
            m.Genres = movieGenres;
            if (!dbContext.Movies.Any(e => e.MovieID == m.MovieID)) dbContext.Movies.Add(m);
            // dbContext.SaveChanges();
        }
        dbContext.SaveChanges();
        return "OK";
    }

    [HttpPost("UploadRatingsCsv")]
    public string PostRatings(IFormFile inputFile)
    {
        var strm = inputFile.OpenReadStream();
        byte[] buffer = new byte[inputFile.Length];
        strm.Read(buffer, 0, (int)inputFile.Length);
        string fileContent = System.Text.Encoding.Default.GetString(buffer);
        strm.Close();
        MoviesContext dbContext = new MoviesContext();
        bool skip_header = true;
        foreach (string line in fileContent.Split('\n'))
        {
            if (skip_header)
            {
                skip_header = false;
                continue;
            }
            var tokens = line.Split(",");
            if (tokens.Length != 4) continue;
            string RatingUser = tokens[0];
            string RatedMovie = tokens[1];
            string RatingValue = tokens[2].Substring(0, 1);
            string RatingID = tokens[3];

            Rating r = new Rating();
            r.RatingID = int.Parse(RatingID);
            r.RatingValue = int.Parse(RatingValue);

            User user = new User();
            user.UserID = int.Parse(RatingUser);

            if (!dbContext.Users.Any(e => e.UserID == int.Parse(RatingUser)))
            {
                user.Name = $"Users{RatingUser}";
                dbContext.Users.Add(user);
                dbContext.SaveChanges();
            }

            IQueryable<User> usersResults = dbContext.Users.Where(e => e.UserID == int.Parse(RatingUser));
            if (usersResults.Count() > 0)
                r.RatingUser = usersResults.First();

            IQueryable<Movie> MovieResults = dbContext.Movies.Where(e => e.MovieID == int.Parse(RatedMovie));
            if (MovieResults.Count() > 0)
                r.RatedMovie = MovieResults.First();

            if (!dbContext.Ratings.Any(e => e.RatingID == r.RatingID)) dbContext.Ratings.Add(r);
            dbContext.SaveChanges();
        }
        dbContext.SaveChanges();
        return "OK";
    }

    [HttpGet("GetAllGenres")]

    public IEnumerable<Genre> GetAllGenres()
    {
        MoviesContext dbContext = new MoviesContext();
        return dbContext.Genres.AsEnumerable();
    }

    [HttpGet("GetMoviesByName/{search_phrase}")]

    public IEnumerable<Movie> GetMoviesByName(string search_phrase)
    {
        MoviesContext dbContext = new MoviesContext();
        return dbContext.Movies.Where(e => e.Title!.Contains(search_phrase));
    }

    [HttpPost("GetMoviesByGenre")]
    public IEnumerable<Movie> GetMoviesByGenre(string search_phrase)
    {
        MoviesContext dbContext = new MoviesContext();
        return dbContext.Movies.Where(
            m => m.Genres!.Any(p => p.Name!.Contains(search_phrase))
        );
    }

    [HttpGet("GetGenresById/{id}")]

    public IEnumerable<Genre> GetGenresById(int id)
    {
        MoviesContext dbContext = new MoviesContext();
        return dbContext.Genres.Where(
            g => g.Movies!.Any(m => m.MovieID.Equals(id))
        );
    }

    [HttpGet("GetVectorOfGenresById/{id}")]

    public List<string> GetVectorOfGenresById(int id)
    {
        MoviesContext dbContext = new MoviesContext();
        var list1 = dbContext.Genres.Where(
            g => g.Movies!.Any(m => m.MovieID.Equals(id))
        )
        .ToList()
        .Select(g => g.Name!)
        .ToList();
        
        return list1;
    }

    private static double getCosine(int Movie1, int Movie2)
    {
        MoviesContext dbContext = new MoviesContext();

        IEnumerable<Genre> genres1 = dbContext.Genres.Where(
            g => g.Movies!.Any(m => m.MovieID.Equals(Movie1))
        );

        IEnumerable<Genre> genres2 = dbContext.Genres.Where(
            g => g.Movies!.Any(m => m.MovieID.Equals(Movie2))
        );

        lab2.Genre[] genreArray1 = genres1.ToArray();
        string[] genreNames1 = genreArray1.Select(g => g.Name!).ToArray();

        lab2.Genre[] genreArray2 = genres2.ToArray();
        string[] genreNames2 = genreArray2.Select(g => g.Name!).ToArray();

        List<string> allGenres = genreNames1.Union(genreNames2).ToList();

        int[] freq1 = new int[allGenres.Count];
        int[] freq2 = new int[allGenres.Count];

        for (int i = 0; i < allGenres.Count; i++)
        {
            freq1[i] = genreNames1.Count(g => g == allGenres[i]);
            freq2[i] = genreNames2.Count(g => g == allGenres[i]);
        }

        double dotProduct = 0;
        double magnitude1 = 0;
        double magnitude2 = 0;

        for (int i = 0; i < allGenres.Count; i++)
        {
            dotProduct += freq1[i] * freq2[i];
            magnitude1 += freq1[i] * freq1[i];
            magnitude2 += freq2[i] * freq2[i];
        }
        double similarity = dotProduct / (Math.Sqrt(magnitude1) * Math.Sqrt(magnitude2));

        return similarity;
    }

    [HttpGet("GetCosineSimilaraty/{Movie1},{Movie2}")]
    public double GetCosineSimilaraty(int Movie1, int Movie2)
    {
        return getCosine(Movie1, Movie2);
    }


    [HttpGet("GetMovieListById/{id}")]

    public IEnumerable<string> GetMovieListById(int id)
    {
        MoviesContext dbContext = new MoviesContext();
        IEnumerable<Genre> genres = dbContext.Genres.Where(
            g => g.Movies!.Any(m => m.MovieID.Equals(id))
        );
        string[] genreNames = genres.ToArray().Select(g => g.Name!).ToArray();

        IQueryable<lab2.Movie> movie = dbContext.Movies.Where(e => e.MovieID == id);
        lab2.Movie[] movieName = movie.ToArray();
        string[] name = movieName.Select(g => g.Title!).ToArray();
        IEnumerable<string> newList = name.Concat(genreNames);
        return newList;
    }

    [HttpGet("GetMovieListById2/{id}")]

    public List<Movie> GetMovieListById2(int id, double threshold)
    {
        MoviesContext dbContext = new MoviesContext();
        return dbContext.Movies
            .Where(m => m.MovieID != id)
            .Select(m => new
            {
                Movie = m,
                Similarity = getCosine(m.MovieID, id)
            }).ToList()
            .Where(x => x.Similarity >= threshold)
            .OrderByDescending(x => x.Similarity)
            .Select(x => x.Movie)
            .ToList();
    }

    [HttpGet("GetMovieListByUserId/{userId}")]
    public List<string?> GetMovieListByUserId(int userId)
    {
        MoviesContext dbContext = new MoviesContext();
        return dbContext.Ratings
            .Where(rating => rating.RatingUser!.UserID == userId)
            .Select(m => m.RatedMovie!.Title)
            .Where(title => title != null)
            .ToList();
    }

    [HttpGet("GetSortedMovieListByUserId/{userId}")]
    public List<string?> GetSortedMovieListByUserId(int userId)
    {
        MoviesContext dbContext = new MoviesContext();
        return dbContext.Ratings
            .Where(rating => rating.RatingUser!.UserID == userId)
            .OrderByDescending(x => x.RatingValue)
            .Select(m => m.RatedMovie!.Title)
            .Where(title => title != null)
            .ToList();
    }

    [HttpGet("GetMovieListWithHighestRatedByUserId/{userId}")]
    public List<string?> GetMovieListWithHighestRatedByUserId(int userId)
    {
        MoviesContext dbContext = new MoviesContext();
        double threshold = 0.75;

        int highestRatedMovieId = dbContext.Ratings
            .Where(rating => rating.RatingUser!.UserID == userId)
            .OrderByDescending(x => x.RatingValue)
            .Select(x => x.RatedMovie!.MovieID)
            .FirstOrDefault();

        return dbContext.Movies
            .Where(m => m.MovieID != highestRatedMovieId)
            .Select(m => new
            {
                Movie = m,
                Similarity = getCosine(m.MovieID, highestRatedMovieId)
            }).ToList()
            .Where(x => x.Similarity >= threshold)
            .OrderByDescending(x => x.Similarity)
            .Select(x => x.Movie.Title)
            .ToList();
    }

    // [HttpGet("GetSetOfRecommendations/{userId}, {size}")]
    // public List<string?> GetSetOfRecommendations(int userId, int size)
    // {
    //     MoviesContext dbContext = new MoviesContext();
    //     double threshold = 0.75;

    //     var highestRatedMovies = dbContext.Ratings
    //         .Where(rating => rating.RatingUser!.UserID == userId)
    //         .OrderByDescending(x => x.RatingValue)
    //         .Select(x => x.RatedMovie!.MovieID);

        

    //     foreach(int highestRatedMovieId in highestRatedMovies){
    //         var list = dbContext.Movies
    //         .Where(m => m.MovieID != highestRatedMovieId)
    //         .Select(m => new
    //         {
    //             Movie = m,
    //             Similarity = getCosine(m.MovieID, highestRatedMovieId)
    //         }).ToList()
    //         .Where(x => x.Similarity >= threshold)
    //         .OrderByDescending(x => x.Similarity);

    //         if (list.Count() <= size)
    //         {
    //             return list
    //                 .Select(x => x.Movie.Title)
    //                 .Take(size)
    //                 .ToList();
    //         }
    //         return list
    //                 .Select(x => x.Movie.Title)
    //                 .Take(size)
    //                 .ToList();
    //     }

    //     // var list = dbContext.Movies
    //     //     .Where(m => m.MovieID != highestRatedMovieId)
    //     //     .Select(m => new
    //     //     {
    //     //         Movie = m,
    //     //         Similarity = getCosine(m.MovieID, highestRatedMovieId)
    //     //     }).ToList()
    //     //     .Where(x => x.Similarity >= threshold)
    //     //     .OrderByDescending(x => x.Similarity);

    //     // if (list.Count() <= size)
    //     // {
    //     //     return list
    //     //         .Select(x => x.Movie.Title)
    //     //         .Take(size)
    //     //         .ToList();
    //     // }
    //     // return list
    //     //         .Select(x => x.Movie.Title)
    //     //         .Take(size)
    //     //         .ToList();
    // }

       [HttpGet("GetSetOfRecommendationsByComparingUsers/{userId}")]
    public List<string?> GetSetOfRecommendationsByComparingUsers(int userId)
    {
        MoviesContext dbContext = new MoviesContext();

        var highestRatedMovie = getHighestRatedMovie(userId);

        var movieID = dbContext.Ratings
            .Where(rating => rating.RatingUser!.UserID == userId)
            .OrderByDescending(x => x.RatingValue)
            .Select(x => x.RatedMovie!.MovieID)
            .FirstOrDefault();

        var list = dbContext.Ratings
            .Where(rating => rating.RatedMovie!.MovieID == movieID && rating.RatingValue == highestRatedMovie!.RatingValue 
            && rating.RatingUser!.UserID != userId)
            .Select(x => new {
                Movie = getHighestRatedMovieTitle(x.RatingUser!.UserID)
            })
            .ToList()
            .Select(x => x.Movie)
            .Where(movie => movie != null)
            .ToList();

        return list;
    }

    private Rating? getHighestRatedMovie(int userId){
        MoviesContext dbContext = new MoviesContext();
        return dbContext.Ratings
            .Where(rating => rating.RatingUser!.UserID == userId)
            .OrderByDescending(x => x.RatingValue)
            .FirstOrDefault();
    }

    private static string? getHighestRatedMovieTitle(int userId){
        MoviesContext dbContext = new MoviesContext();
        return dbContext.Ratings
            .Where(rating => rating.RatingUser!.UserID == userId)
            .OrderByDescending(x => x.RatingValue)
            .Select(x => x.RatedMovie!.Title)
            .FirstOrDefault();
    }
}
