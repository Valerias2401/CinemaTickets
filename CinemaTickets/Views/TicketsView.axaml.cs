using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using CinemaTickets.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaTickets;

public partial class TicketsView : UserControl
{
    private readonly Func<DbContext> _createDb;

    // сообщить главному окну, что места освободились
    public event Action? TicketsChanged;

    public TicketsView(Func<DbContext> createDb)
    {
        _createDb = createDb;

        InitializeComponent();

        Loaded += async (_, _) => await LoadTicketsAsync();
    }

    private static IBrush Pink(string color) =>
        new SolidColorBrush(Color.Parse(color));

    public async Task LoadTicketsAsync()
    {
        CountText.Text = "Загрузка билетов...";
        TicketsPanel.Children.Clear();

        try
        {
            await using var db = _createDb();

            var tickets = await db.Set<Ticket>()
                .AsNoTracking()
                .Include(t => t.Session)
                    .ThenInclude(s => s.Movie)
                .Include(t => t.Seat)
                .OrderByDescending(t => t.PurchaseTime)
                .ToListAsync();

            CountText.Text = $"Куплено билетов: {tickets.Count}";

            if (tickets.Count == 0)
            {
                TicketsPanel.Children.Add(CreateMessage(
                    "Билетов пока нет. Выберите фильм и сеанс!"));
                return;
            }

            foreach (var ticket in tickets)
                TicketsPanel.Children.Add(CreateTicketCard(ticket));
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);

            CountText.Text = "Ошибка загрузки";
            TicketsPanel.Children.Add(CreateMessage(
                "Не удалось загрузить билеты. " +
                "Проверьте подключение к PostgreSQL."));
        }
    }

    private Control CreateTicketCard(Ticket ticket)
    {
        var grid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            ColumnSpacing = 16
        };

        var info = new StackPanel { Spacing = 5 };

        info.Children.Add(new TextBlock
        {
            Text = ticket.Session.Movie.Title,
            FontSize = 18,
            FontWeight = FontWeight.Bold,
            Foreground = Pink("#831843"),
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxLines = 1
        });

        info.Children.Add(new TextBlock
        {
            Text = $"{ticket.Session.StartTime:dd.MM.yyyy HH:mm}",
            FontSize = 13,
            Foreground = Pink("#AD648A")
        });

        info.Children.Add(new TextBlock
        {
            Text = $"Ряд {ticket.Seat.RowNumber}, место {ticket.Seat.SeatNumber}",
            FontSize = 13,
            Foreground = Pink("#AD648A")
        });

        info.Children.Add(new TextBlock
        {
            Text = $"{ticket.Session.TicketPrice:0.00} ₽",
            FontSize = 16,
            FontWeight = FontWeight.SemiBold,
            Foreground = Pink("#BE185D")
        });

        grid.Children.Add(info);

        var cancel = new Button
        {
            Content = "Отменить билет",
            VerticalAlignment = VerticalAlignment.Center
        };
        cancel.Classes.Add("danger");
        Grid.SetColumn(cancel, 1);

        bool confirm = false;

        cancel.Click += async (_, _) =>
        {
            // первый клик просит подтверждение, второй удаляет
            if (!confirm)
            {
                confirm = true;
                cancel.Content = "Точно отменить?";
                return;
            }

            cancel.IsEnabled = false;
            await CancelTicketAsync(ticket.Id);
        };

        grid.Children.Add(cancel);

        return new Border
        {
            Background = Brushes.White,
            CornerRadius = new CornerRadius(18),
            BorderBrush = Pink("#F1DCE7"),
            BorderThickness = new Thickness(1),
            Padding = new Thickness(20),
            Child = grid
        };
    }

    private async Task CancelTicketAsync(int ticketId)
    {
        try
        {
            await using var db = _createDb();

            var ticket = await db.Set<Ticket>()
                .FirstOrDefaultAsync(t => t.Id == ticketId);

            if (ticket != null)
            {
                db.Set<Ticket>().Remove(ticket);
                await db.SaveChangesAsync();
            }

            await LoadTicketsAsync();
            TicketsChanged?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            CountText.Text = "Не удалось отменить билет. Попробуйте ещё раз.";
            await LoadTicketsAsync();
        }
    }

    private Control CreateMessage(string message) => new Border
    {
        Background = Brushes.White,
        CornerRadius = new CornerRadius(16),
        Padding = new Thickness(25),
        Child = new TextBlock
        {
            Text = message,
            FontSize = 16,
            Foreground = Pink("#9D174D"),
            TextWrapping = TextWrapping.Wrap,
            TextAlignment = TextAlignment.Center
        }
    };
}
