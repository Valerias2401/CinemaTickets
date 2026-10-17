
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using CinemaTickets.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CinemaTickets;

public partial class SessionsView : UserControl
{
    private Session? selectedSession;

    private readonly List<Button> sessionButtons = new();

    public event Action<Session>? SeatSelectionRequested;

    public SessionsView()
    {
        InitializeComponent();
    }

    public SessionsView(
        Movie movie,
        List<Session> sessions) : this()
    {
        MovieTitle.Text = movie.Title;

        LoadSessions(sessions);
    }

    private static IBrush Pink(string color)
    {
        return new SolidColorBrush(Color.Parse(color));
    }

    private void LoadSessions(List<Session> sessions)
    {
        SessionsPanel.Children.Clear();
        sessionButtons.Clear();

        selectedSession = null;
        ChooseSeatButton.IsEnabled = false;
        SelectedText.Text = "Сеанс не выбран";

        if (sessions.Count == 0)
        {
            SessionsPanel.Children.Add(new TextBlock
            {
                Text = "Для этого фильма пока нет сеансов.",
                Foreground = Pink("#9D174D")
            });

            return;
        }

        foreach (var session in
                 sessions.OrderBy(s => s.StartTime))
        {
            var row = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Spacing = 25
            };

            row.Children.Add(new TextBlock
            {
                Text = session.StartTime.ToString("dd.MM.yyyy"),
                Width = 110,
                FontSize = 16,
                Foreground = Pink("#831843")
            });

            row.Children.Add(new TextBlock
            {
                Text = session.StartTime.ToString("HH:mm"),
                Width = 65,
                FontSize = 16,
                Foreground = Pink("#831843")
            });

            row.Children.Add(new TextBlock
            {
                Text = $"{session.TicketPrice:0.00} ₽",
                FontSize = 16,
                Foreground = Pink("#BE185D")
            });

            var button = new Button
            {
                Content = row,
                Background = Pink("#FFF1F7"),
                BorderBrush = Pink("#F9A8D4"),
                BorderThickness = new Thickness(1),
                Padding = new Thickness(15),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left
            };

            button.Click += (_, _) =>
            {
                SelectSession(session, button);
            };

            sessionButtons.Add(button);
            SessionsPanel.Children.Add(button);
        }
    }

    private void SelectSession(
        Session session,
        Button selectedButton)
    {
        selectedSession = session;

        foreach (var button in sessionButtons)
        {
            button.Background = Pink("#FFF1F7");
        }

        selectedButton.Background = Pink("#F9A8D4");

        SelectedText.Text =
            $"Выбран сеанс: {session.StartTime:dd.MM.yyyy HH:mm}";

        ChooseSeatButton.IsEnabled = true;
    }

    private void ChooseSeat_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (selectedSession == null)
            return;

        SeatSelectionRequested?.Invoke(selectedSession);
    }
}
