using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CinemaTickets.Models
{
    public class Ticket
    {
        public int Id { get; set; }

        public int SessionId { get; set; }

        public Session Session { get; set; } = null!;

        public int SeatId { get; set; }

        public Seat Seat { get; set; } = null!;

        public DateTime PurchaseTime { get; set; } = DateTime.UtcNow;
    }
}
