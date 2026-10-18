using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using CinemaTickets.Models;
using CinemaTickets.Views;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace CinemaTickets;

public partial class MainWindow : Window
{
    private readonly Func<DbContext> _createDb;

    private readonly List<Movie> movies = new();

    private Movie? selectedMovie;

    private int navigationVersion = 0;

    public MainWindow(Func<DbContext> createDb)
    {
        _createDb = createDb;

        InitializeComponent();

        Opened += async (_, _) =>
        {
            await ShowMoviesAsync();
        };
    }

    private static IBrush Pink(string color)
    {
        return new SolidColorBrush(Color.Parse(color));
    }

    private static IBrush PosterBrush(int movieId)
    {
        string color1;
        string color2;

        if (movieId % 3 == 1)
        {
            color1 = "#542251";
            color2 = "#B24B93";
        }
        else if (movieId % 3 == 2)
        {
            color1 = "#782753";
            color2 = "#E07A9E";
        }
        else
        {
            color1 = "#95465E";
            color2 = "#E7A37F";
        }

        return new LinearGradientBrush
        {
            StartPoint = new RelativePoint(
                0, 0, RelativeUnit.Relative),

            EndPoint = new RelativePoint(
                1, 1, RelativeUnit.Relative),

            GradientStops = new GradientStops
            {
                new GradientStop(Color.Parse(color1), 0),
                new GradientStop(Color.Parse(color2), 1)
            }
        };
    }

    private void SetActiveButton(Button active)
    {
        Button[] buttons =
        {
            MoviesButton,
            SessionsButton,
            TicketsButton
        };

        foreach (var button in buttons)
        {
            button.Background = Brushes.Transparent;
            button.Foreground = Pink("#965A7A");
        }

        active.Background = Pink("#9D174D");
        active.Foreground = Brushes.White;
    }

    private void SetPage(
        string title,
        string subtitle,
        Button activeButton)
    {
        SectionTitle.Text = title;
        SectionSubtitle.Text = subtitle;

        SetActiveButton(activeButton);
    }

    private async void Movies_Click(
        object? sender,
        RoutedEventArgs e)
    {
        await ShowMoviesAsync();
    }

    private async void Sessions_Click(
        object? sender,
        RoutedEventArgs e)
    {
        if (selectedMovie == null)
        {
            await ShowMoviesAsync();

            ResultsText.Text =
                "Сначала выберите фильм для просмотра сеансов.";

            return;
        }

        await ShowSessionsAsync(selectedMovie);
    }

    private void Tickets_Click(
        object? sender,
        RoutedEventArgs e)
    {
        ShowTickets();
    }



    private Task ShowMoviesAsync()
    {
        navigationVersion++;

        SetPage(
            "Афиша",
            "Выбирай историю по настроению",
            MoviesButton);

        MoviesPage.IsVisible = false;

        PageHost.IsVisible = true;

        var moviesView = new MoviesView();

        moviesView.MovieSelected += async movie =>
        {
            await ShowSessionsAsync(movie);
        };

        PageHost.Content = moviesView;

        return Task.CompletedTask;
    }


    private void SearchBox_TextChanged(
        object? sender,
        TextChangedEventArgs e)
    {
        if (MoviesPanel != null &&
            ResultsText != null &&
            MoviesPage != null &&
            MoviesPage.IsVisible)
        {
            RenderMovies();
        }
    }

    private void RenderMovies()
    {
        MoviesPanel.Children.Clear();

        string search = SearchBox.Text?.Trim() ?? "";

        var filtered = movies
            .Where(m => m.Title.Contains(
                search,
                StringComparison.OrdinalIgnoreCase))
            .ToList();

        ResultsText.Text =
            $"Фильмов в афише: {filtered.Count}";

        if (filtered.Count == 0)
        {
            MoviesPanel.Children.Add(
                CreateMessage("Фильмы не найдены."));

            return;
        }

        foreach (var movie in filtered)
        {
            MoviesPanel.Children.Add(
                CreateMovieCard(movie));
        }
    }

    private Control CreateMovieCard(Movie movie)
    {
        var card = new Border
        {
            Width = 266,
            Background = Brushes.White,
            CornerRadius = new CornerRadius(18),
            BorderBrush = Pink("#F1DCE7"),
            BorderThickness = new Thickness(1),
            ClipToBounds = true,
            Margin = new Thickness(0, 0, 18, 18)
        };

        var content = new StackPanel();

        var poster = new Grid
        {
            Height = 156,
            Background = PosterBrush(movie.Id)
        };

        poster.Children.Add(new TextBlock
        {
            Text = $"PINK CINEMA / {movie.ReleaseYear}",
            FontSize = 11,
            FontWeight = FontWeight.SemiBold,
            Foreground = Pink("#FFE4F2"),
            Margin = new Thickness(18, 17, 0, 0),
            VerticalAlignment = VerticalAlignment.Top
        });

        poster.Children.Add(new TextBlock
        {
            Text = movie.Title,
            FontSize = 24,
            FontWeight = FontWeight.Bold,
            Foreground = Brushes.White,
            TextWrapping = TextWrapping.NoWrap,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxLines = 1,
            Margin = new Thickness(18),
            VerticalAlignment = VerticalAlignment.Bottom
        });

        content.Children.Add(poster);

        var information = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 11
        };

        information.Children.Add(new TextBlock
        {
            Text = movie.Title,
            FontSize = 18,
            FontWeight = FontWeight.Bold,
            Foreground = Pink("#831843"),
            TextWrapping = TextWrapping.NoWrap,
            TextTrimming = TextTrimming.CharacterEllipsis,
            MaxLines = 1
        });

        information.Children.Add(new TextBlock
        {
            Text =
                $"{movie.Genre} • {movie.DurationMinutes} мин",
            FontSize = 12,
            Foreground = Pink("#AD648A")
        });

        if (!string.IsNullOrWhiteSpace(movie.Description))
        {
            information.Children.Add(new TextBlock
            {
                Text = movie.Description,
                FontSize = 12,
                Foreground = Pink("#965A7A"),
                TextWrapping = TextWrapping.NoWrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxLines = 1
            });
        }

        var button = new Button
        {
            Content = "Смотреть сеансы →",
            Height = 42,
            Background = Pink("#BE185D"),
            Foreground = Brushes.White,
            FontSize = 13,
            FontWeight = FontWeight.SemiBold,
            CornerRadius = new CornerRadius(10),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };

        button.Classes.Add("movieAction");

        button.Click += async (_, _) =>
        {
            await ShowSessionsAsync(movie);
        };

        information.Children.Add(button);

        content.Children.Add(information);

        card.Child = content;

        return card;
    }


    public async Task ShowSessionsAsync(Movie movie)
    {
        int request = ++navigationVersion;

        selectedMovie = movie;

        SetPage(
            "Сеансы",
            $"Расписание фильма «{movie.Title}»",
            SessionsButton);

        MoviesPage.IsVisible = false;
        PageHost.IsVisible = true;

        PageHost.Content = CreateMessage(
            "Загружаем расписание...");

        try
        {
            await using var db = _createDb();

            var movieSessions = await db.Set<Session>()
                .AsNoTracking()
                .Where(s => s.MovieId == movie.Id)
                .OrderBy(s => s.StartTime)
                .ToListAsync();

            if (request != navigationVersion)
                return;

            var view = new SessionsView(
                movie,
                movieSessions);

            view.SeatSelectionRequested += ShowSeats;

            PageHost.Content = view;
        }
        catch (Exception ex)
        {
            if (request != navigationVersion)
                return;

            Debug.WriteLine(ex);

            PageHost.Content = CreateMessage(
                "Не удалось загрузить сеансы. " +
                "Проверьте подключение к PostgreSQL.");
        }
    }


    private async void ShowSeats(Session session)
    {
        int request = ++navigationVersion;

        SectionTitle.Text = "Выбор места";

        SectionSubtitle.Text =
            $"Сеанс {session.StartTime:dd.MM.yyyy HH:mm}";

        MoviesPage.IsVisible = false;
        PageHost.IsVisible = true;

        PageHost.Content = CreateMessage(
            "Загружаем схему зала...");

        try
        {
            var service =
                new CinemaTickets.Services.SeatService(_createDb);

            var data = await service.LoadAsync(session.Id);

            if (request != navigationVersion)
                return;

            var view = new SeatsView(
                selectedMovie?.Title ?? "",
                session,
                data.Seats,
                data.OccupiedSeatIds);

            view.SeatsConfirmed += async (confirmedSession, seats) =>
            {
                var window = new PurchaseWindow(_createDb, confirmedSession, seats);

                bool bought = await window.ShowDialog<bool>(this);

                if (bought)
                {
                    ShowTickets();            // куплено: показываем билеты
                }
                else
                {
                    ShowSeats(confirmedSession); // отмена/ошибка: перерисовать схему, свежие занятые места
                }
            };

            PageHost.Content = view;
        }
        catch (Exception ex)
        {
            if (request != navigationVersion)
                return;

            Debug.WriteLine(ex);

            PageHost.Content = CreateMessage(
                "Не удалось загрузить схему зала. " +
                "Проверьте подключение к PostgreSQL.");
        }
    }


    private void ShowTickets()
    {
        navigationVersion++;

        SetPage(
            "Билеты",
            "Все купленные билеты",
            TicketsButton);

        MoviesPage.IsVisible = false;
        PageHost.IsVisible = true;

        PageHost.Content = new TicketsView(_createDb);
    }

    private Control CreateMessage(string message)
    {
        return new Border
        {
            Background = Brushes.White,
            CornerRadius = new CornerRadius(16),
            Padding = new Thickness(25),

            Child = new TextBlock
            {
                Text = message,
                FontSize = 17,
                Foreground = Pink("#9D174D"),
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            }
        };
    }
}