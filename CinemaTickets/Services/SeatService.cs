using CinemaTickets.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CinemaTickets.Services
{

    public class SeatData
    {
        public List<Seat> Seats { get; set; } = new();

        public HashSet<int> OccupiedSeatIds { get; set; } = new();
    }

    public class SeatService
    {
        private readonly Func<DbContext> _createDb;

        public SeatService(Func<DbContext> createDb)
        {
            _createDb = createDb;
        }

        public async Task<SeatData> LoadAsync(int sessionId)
        {
            await using var db = _createDb();

            var seats = await db.Set<Seat>()
                .OrderBy(s => s.RowNumber)
                .ThenBy(s => s.SeatNumber)
                .ToListAsync();

            bool added = false;

            for (int row = 1; row <= SeatsView.Rows; row++)
            {
                for (int number = 1;
                     number <= SeatsView.Columns;
                     number++)
                {
                    if (seats.Any(s =>
                            s.RowNumber == row &&
                            s.SeatNumber == number))
                    {
                        continue;
                    }

                    var seat = new Seat
                    {
                        RowNumber = row,
                        SeatNumber = number
                    };

                    db.Set<Seat>().Add(seat);
                    seats.Add(seat);

                    added = true;
                }
            }

            if (added)
                await db.SaveChangesAsync();

            var occupied = await db.Set<Ticket>()
                .AsNoTracking()
                .Where(t => t.SessionId == sessionId)
                .Select(t => t.SeatId)
                .ToListAsync();

            return new SeatData
            {
                Seats = seats,
                OccupiedSeatIds = occupied.ToHashSet()
            };
        }
    }
}