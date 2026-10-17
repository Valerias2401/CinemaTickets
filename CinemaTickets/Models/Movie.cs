using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CinemaTickets.Models
{
    public class Movie
    {
        public int Id { get; set; }

        public string Title { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public int DurationMinutes { get; set; }

        public string Genre { get; set; } = string.Empty;

        public int ReleaseYear { get; set; }

        public ICollection<Session> Sessions { get; set; } = new List<Session>();
    }
}
