using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using CinemaTickets.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CinemaTickets;

public partial class PurchaseWindow : Window
{
    private readonly Func<DbContext> _createDb;
    private readonly Session _session;
    private readonly List<Seat> _seats;

    private bool purchased;

    public PurchaseWindow(
        Func<DbContext> createDb,
        Session session,
        IEnumerable<Seat> seats)
    {
        _createDb = createDb;
        _session = session;
        _seats = seats
            .OrderBy(s => s.RowNumber)
            .ThenBy(s => s.SeatNumber)
            .ToList();

        InitializeComponent();

        TimeText.Text = $"Сеанс: {session.StartTime:dd.MM.yyyy HH:mm}";

        SeatText.Text = "Места: " + string.Join("; ",
            _seats.Select(s => $"ряд {s.RowNumber}, место {s.SeatNumber}"));

        PriceText.Text =
            $"Итого: {_seats.Count * session.TicketPrice:0.00} ₽";

        MovieText.Text = session.Movie?.Title ?? "Загрузка...";

        Opened += async (_, _) => await LoadMovieTitleAsync();
    }

    private async System.Threading.Tasks.Task LoadMovieTitleAsync()
    {
        if (_session.Movie != null)
            return;

        try
        {
            await using var db = _createDb();

            var title = await db.Set<Movie>()
                .AsNoTracking()
                .Where(m => m.Id == _session.MovieId)
                .Select(m => m.Title)
                .FirstOrDefaultAsync();

            MovieText.Text = title ?? "Фильм";
        }
        catch
        {
            MovieText.Text = "Фильм";
        }
    }

    private void ShowStatus(string text, string color)
    {
        StatusText.Text = text;
        StatusText.Foreground = new SolidColorBrush(Color.Parse(color));
        StatusText.IsVisible = true;
    }

    private async void Confirm_Click(object? sender, RoutedEventArgs e)
    {
        ConfirmButton.IsEnabled = false;

        try
        {
            await using var db = _createDb();

            var seatIds = _seats.Select(s => s.Id).ToList();

            bool taken = await db.Set<Ticket>().AnyAsync(t =>
                t.SessionId == _session.Id &&
                seatIds.Contains(t.SeatId));

            if (taken)
            {
                ShowStatus(
                    "Одно из мест уже занято. Закройте окно и выберите другие.",
                    "#B91C1C");
                CloseButton.Content = "Закрыть";
                return;
            }

            var now = DateTime.UtcNow;

            db.Set<Ticket>().AddRange(_seats.Select(s => new Ticket
            {
                SessionId = _session.Id,
                SeatId = s.Id,
                PurchaseTime = now
            }));

            await db.SaveChangesAsync();

            purchased = true;

            ShowStatus(
                _seats.Count == 1
                    ? "Билет успешно куплен! 🎉"
                    : $"Билеты успешно куплены ({_seats.Count})! 🎉",
                "#15803D");

            ConfirmButton.IsVisible = false;
            CloseButton.Content = "Готово";
            CloseButton.Classes.Remove("secondary");
            CloseButton.Classes.Add("primary");
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation
            })
        {
            ShowStatus(
                "Кто-то только что купил одно из этих мест. " +
                "Закройте окно и выберите другие.",
                "#B91C1C");
            CloseButton.Content = "Закрыть";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);

            ShowStatus(
                "Не удалось сохранить билеты. " +
                "Проверьте подключение к PostgreSQL.",
                "#B91C1C");
            ConfirmButton.IsEnabled = true;
        }
    }

    // true = билеты куплены
    private void Close_Click(object? sender, RoutedEventArgs e)
    {
        Close(purchased);
    }
}