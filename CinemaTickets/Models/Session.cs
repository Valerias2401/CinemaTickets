using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CinemaTickets.Models
{
    public class Session
    {
        public int Id { get; set; }

        public int MovieId { get; set; }

        public Movie Movie { get; set; } = null!;

        public DateTime StartTime { get; set; }

        public decimal TicketPrice { get; set; }

        public ICollection<Ticket> Tickets { get; set; } = new List<Ticket>();
    }
}
