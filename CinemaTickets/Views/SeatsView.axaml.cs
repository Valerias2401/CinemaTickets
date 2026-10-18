using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using CinemaTickets.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CinemaTickets;

public partial class SeatsView : UserControl
{
    public const int Rows = 5;

    public const int Columns = 8;

    private enum SeatState
    {
        Free,
        Occupied,
        Selected
    }

    private Session session = null!;

    private readonly Dictionary<int, Border> seatCells = new();

    private readonly Dictionary<int, Seat> seatsById = new();

    private readonly HashSet<int> occupiedSeatIds = new();

    private readonly List<Seat> selectedSeats = new();

    public event Action<Session, List<Seat>>? SeatsConfirmed;

    public SeatsView()
    {
        InitializeComponent();
    }

    public SeatsView(
        string movieTitle,
        Session session,
        List<Seat> seats,
        HashSet<int> occupiedSeatIds) : this()
    {
        this.session = session;

        foreach (var seatId in occupiedSeatIds)
        {
            this.occupiedSeatIds.Add(seatId);
        }

        SessionTitle.Text =
            $"{movieTitle} • {session.StartTime:dd.MM.yyyy HH:mm}";

        LoadSeats(seats);
        UpdateSummary();
    }

    private static IBrush Pink(string color)
    {
        return new SolidColorBrush(Color.Parse(color));
    }

    private void LoadSeats(List<Seat> seats)
    {
        SeatsPanel.Children.Clear();
        seatCells.Clear();
        seatsById.Clear();
        selectedSeats.Clear();

        var seatMap = new Dictionary<(int Row, int Number), Seat>();

        foreach (var seat in seats)
        {
            seatMap[(seat.RowNumber, seat.SeatNumber)] = seat;
            seatsById[seat.Id] = seat;
        }

        for (int row = 1; row <= Rows; row++)
        {
            var line = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 8
            };

            line.Children.Add(new TextBlock
            {
                Text = $"Ряд {row}",
                Width = 52,
                FontSize = 13,
                Foreground = Pink("#AC7894"),
                VerticalAlignment = VerticalAlignment.Center
            });

            for (int number = 1; number <= Columns; number++)
            {
                if (!seatMap.TryGetValue((row, number), out var seat))
                {
                    line.Children.Add(new Border
                    {
                        Width = 46,
                        Height = 42
                    });

                    continue;
                }

                line.Children.Add(CreateSeatCell(seat));
            }

            SeatsPanel.Children.Add(line);
        }

        int total = seatCells.Count;
        int free = total - seatCells.Keys.Count(
            id => occupiedSeatIds.Contains(id));

        FreeSeatsText.Text =
            $"Свободно мест: {free} из {total}";
    }

    private Border CreateSeatCell(Seat seat)
    {
        var cell = new Border
        {
            Width = 46,
            Height = 42,
            CornerRadius = new CornerRadius(10),
            BorderThickness = new Thickness(1),

            Child = new TextBlock
            {
                Text = seat.SeatNumber.ToString(),
                FontSize = 14,
                FontWeight = FontWeight.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center
            }
        };

        ToolTip.SetTip(
            cell,
            $"Ряд {seat.RowNumber}, место {seat.SeatNumber}");

        if (!occupiedSeatIds.Contains(seat.Id))
        {
            cell.Cursor = new Cursor(StandardCursorType.Hand);

            cell.Tapped += (_, _) =>
            {
                ToggleSeat(seat);
            };
        }

        seatCells[seat.Id] = cell;

        ApplyState(seat.Id);

        return cell;
    }

    private SeatState GetState(int seatId)
    {
        if (occupiedSeatIds.Contains(seatId))
            return SeatState.Occupied;

        if (selectedSeats.Any(s => s.Id == seatId))
            return SeatState.Selected;

        return SeatState.Free;
    }

    private void ApplyState(int seatId)
    {
        var cell = seatCells[seatId];
        var text = (TextBlock)cell.Child!;

        switch (GetState(seatId))
        {
            case SeatState.Occupied:
                cell.Background = Pink("#E9DCE4");
                cell.BorderBrush = Pink("#E9DCE4");
                text.Foreground = Pink("#B79AAA");
                break;

            case SeatState.Selected:
                cell.Background = Pink("#BE185D");
                cell.BorderBrush = Pink("#9D174D");
                text.Foreground = Brushes.White;
                break;

            default:
                cell.Background = Brushes.White;
                cell.BorderBrush = Pink("#F9A8D4");
                text.Foreground = Pink("#BE185D");
                break;
        }
    }

    private void ToggleSeat(Seat seat)
    {
        if (occupiedSeatIds.Contains(seat.Id))
            return;

        var selected = selectedSeats.FirstOrDefault(
            s => s.Id == seat.Id);

        if (selected != null)
        {
            selectedSeats.Remove(selected);
        }
        else
        {
            selectedSeats.Add(seat);
        }

        ApplyState(seat.Id);
        UpdateSummary();
    }

    private void UpdateSummary()
    {
        if (selectedSeats.Count == 0)
        {
            SelectedText.Text = "Места не выбраны";
            PriceText.Text = "";
            ContinueButton.IsEnabled = false;

            return;
        }

        var ordered = selectedSeats
            .OrderBy(s => s.RowNumber)
            .ThenBy(s => s.SeatNumber)
            .Select(s => $"ряд {s.RowNumber}, место {s.SeatNumber}");

        SelectedText.Text =
            $"Выбрано: {string.Join("; ", ordered)}";

        PriceText.Text =
            $"Итого: {selectedSeats.Count * session.TicketPrice:0.00} ₽";

        ContinueButton.IsEnabled = true;
    }

    private void Continue_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (selectedSeats.Count == 0)
            return;

        var result = selectedSeats
            .OrderBy(s => s.RowNumber)
            .ThenBy(s => s.SeatNumber)
            .ToList();

        SeatsConfirmed?.Invoke(session, result);
    }
}