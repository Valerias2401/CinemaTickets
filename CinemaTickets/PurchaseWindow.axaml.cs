using System;
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
    private readonly Seat _seat;

    private bool purchased;

    public PurchaseWindow(
        Func<DbContext> createDb,
        Session session,
        Seat seat)
    {
        _createDb = createDb;
        _session = session;
        _seat = seat;

        InitializeComponent();

        TimeText.Text = $"Сеанс: {session.StartTime:dd.MM.yyyy HH:mm}";
        SeatText.Text = $"Ряд {seat.RowNumber}, место {seat.SeatNumber}";
        PriceText.Text = $"{session.TicketPrice:0.00} ₽";
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

            bool taken = await db.Set<Ticket>().AnyAsync(t =>
                t.SessionId == _session.Id &&
                t.SeatId == _seat.Id);

            if (taken)
            {
                ShowStatus(
                    "Это место уже занято. Закройте окно и выберите другое.",
                    "#B91C1C");
                CloseButton.Content = "Закрыть";
                return;
            }

            db.Set<Ticket>().Add(new Ticket
            {
                SessionId = _session.Id,
                SeatId = _seat.Id,
                PurchaseTime = DateTime.UtcNow
            });

            await db.SaveChangesAsync();

            purchased = true;

            ShowStatus("Билет успешно куплен! 🎉", "#15803D");
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
            // кто-то купил это место за секунду до нас
            ShowStatus(
                "Это место только что купил другой пользователь. " +
                "Выберите другое.",
                "#B91C1C");
            CloseButton.Content = "Закрыть";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex);

            ShowStatus(
                "Не удалось сохранить билет. " +
                "Проверьте подключение к PostgreSQL.",
                "#B91C1C");
            ConfirmButton.IsEnabled = true;
        }
    }

    // true = билет куплен, можно обновлять схему зала
    private void Close_Click(object? sender, RoutedEventArgs e)
    {
        Close(purchased);
    }
}
