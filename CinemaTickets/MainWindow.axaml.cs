
using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using CinemaTickets.Models;

namespace CinemaTickets;

public partial class MainWindow : Window
{
    private Movie? selectedMovie;

    private readonly List<Movie> movies = new()
    {
        new Movie
        {
            Id = 1,
            Title = "Интерстеллар",
            Genre = "Фантастика",
            DurationMinutes = 169,
            ReleaseYear = 2014
        },
        new Movie
        {
            Id = 2,
            Title = "Начало",
            Genre = "Триллер",
            DurationMinutes = 148,
            ReleaseYear = 2010
        },
        new Movie
        {
            Id = 3,
            Title = "Дюна",
            Genre = "Фантастика",
            DurationMinutes = 155,
            ReleaseYear = 2021
        }
    };
    private readonly List<Session> sessions = new()
    {
        new Session
        {
            Id = 1,
            MovieId = 1,
            StartTime = DateTime.Today.AddDays(1).AddHours(15),
            TicketPrice = 350
        },
        new Session
        {
            Id = 2,
            MovieId = 1,
            StartTime = DateTime.Today.AddDays(1).AddHours(19),
            TicketPrice = 450
        },
        new Session
        {
            Id = 3,
            MovieId = 2,
            StartTime = DateTime.Today.AddDays(1).AddHours(17),
            TicketPrice = 400
        },
        new Session
        {
            Id = 4,
            MovieId = 3,
            StartTime = DateTime.Today.AddDays(2).AddHours(20),
            TicketPrice = 500
        }
    };

    public MainWindow()
    {
        InitializeComponent();
        ShowMovies();
    }

    private static IBrush Brush(string hex)
    {
        return new SolidColorBrush(Color.Parse(hex));
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
        var buttons = new[]
        {
            MoviesButton,
            SessionsButton,
            TicketsButton
        };

        foreach (var button in buttons)
        {
            button.Background = Brushes.Transparent;
            button.Foreground = Brush("#965A7A");
        }

        active.Background = Brush("#9D174D");
        active.Foreground = Brushes.White;
    }

    private void SetPage(
        string title,
        string subtitle,
        Button activeButton)
    {
        SectionTitle.Text = title;

        SetActiveButton(activeButton);
    }

    private void Movies_Click(
        object? sender, RoutedEventArgs e)
    {
        ShowMovies();
    }

    private void Sessions_Click(
        object? sender, RoutedEventArgs e)
    {
        if (selectedMovie == null)
        {
            ShowMovies();
            return;
        }

        ShowSessions(selectedMovie);
    }

    private void Tickets_Click(
        object? sender, RoutedEventArgs e)
    {
        ShowTickets();
    }

    private void ShowMovies()
    {
        SetPage(
            "Афиша",
            "Выбирай историю по настроению",
            MoviesButton);

        MoviesPage.IsVisible = true;
        PageHost.IsVisible = false;

        RenderMovies();
    }

    private void SearchBox_TextChanged(
        object? sender,
        TextChangedEventArgs e)
    {
        if (MoviesPanel != null)
            RenderMovies();
    }

    private void RenderMovies()
    {
        MoviesPanel.Children.Clear();

        string search = SearchBox.Text?
            .Trim() ?? "";

        var filtered = movies
            .Where(m => m.Title.Contains(
                search,
                StringComparison.OrdinalIgnoreCase))
            .ToList();

        ResultsText.Text =
            $"Фильмов в афише: {filtered.Count}";

        if (filtered.Count == 0)
        {
            MoviesPanel.Children.Add(new TextBlock
            {
                Text = "По вашему запросу фильмы не найдены.",
                FontSize = 15,
                Foreground = Brush("#965A7A"),
                Margin = new Thickness(0, 20)
            });

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
            BorderBrush = Brush("#F1DCE7"),
            BorderThickness = new Thickness(1),
            ClipToBounds = true,
            Margin = new Thickness(0, 0, 18, 18)
        };

        var content = new StackPanel
        {
            Spacing = 0
        };

        var poster = new Grid
        {
            Height = 156,
            Background = PosterBrush(movie.Id),
            ClipToBounds = true
        };

        poster.Children.Add(new Border
        {
            Width = 145,
            Height = 145,
            CornerRadius = new CornerRadius(75),
            Background = Brush("#FFFFFF"),
            Opacity = 0.10,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, -35, -30, 0)
        });

        poster.Children.Add(new TextBlock
        {
            Text = $"PINK CINEMA  /  {movie.ReleaseYear}",
            FontSize = 11,
            FontWeight = FontWeight.SemiBold,
            Foreground = Brush("#FFE4F2"),
            Margin = new Thickness(18, 17, 0, 0),
            VerticalAlignment = VerticalAlignment.Top
        });

        poster.Children.Add(new TextBlock
        {
            Text = movie.Title,
            FontSize = 27,
            FontWeight = FontWeight.Bold,
            Foreground = Brushes.White,
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(18),
            VerticalAlignment = VerticalAlignment.Bottom
        });

        content.Children.Add(poster);

        var information = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 12
        };

        information.Children.Add(new TextBlock
        {
            Text = movie.Title,
            FontSize = 18,
            FontWeight = FontWeight.Bold,
            Foreground = Brush("#831843"),
            TextWrapping = TextWrapping.Wrap
        });

        information.Children.Add(new TextBlock
        {
            Text =
                $"{movie.Genre}  •  {movie.DurationMinutes} мин",
            FontSize = 12,
            Foreground = Brush("#AD648A"),
            TextWrapping = TextWrapping.Wrap
        });

        var actionButton = new Button
        {
            Content = "Смотреть сеансы  →",
            Height = 42,
            Background = Brush("#BE185D"),
            Foreground = Brushes.White,
            FontSize = 13,
            FontWeight = FontWeight.SemiBold,
            CornerRadius = new CornerRadius(10),
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center
        };

        actionButton.Classes.Add("movieAction");

        actionButton.Click += (_, _) =>
        {
            ShowSessions(movie);
        };

        information.Children.Add(actionButton);

        content.Children.Add(information);
        card.Child = content;

        return card;
    }

    public void ShowSessions(Movie movie)
    {
        selectedMovie = movie;

        SetPage(
            "Сеансы",
            $"Расписание фильма «{movie.Title}»",
            SessionsButton);

        MoviesPage.IsVisible = false;
        PageHost.IsVisible = true;

        var movieSessions = sessions
            .Where(s => s.MovieId == movie.Id)
            .OrderBy(s => s.StartTime)
            .ToList();

        var view = new SessionsView(movie, movieSessions);

        view.SeatSelectionRequested += ShowSeats;

        PageHost.Content = view;
    }

    private void ShowSeats(Session session)
    {
        SectionTitle.Text = "Выбор места";
        SectionSubtitle.Text =
            $"Сеанс {session.StartTime:dd.MM.yyyy HH:mm}";

        MoviesPage.IsVisible = false;
        PageHost.IsVisible = true;

        // Позже здесь будет SeatsView Вики
        PageHost.Content = CreatePlaceholder(
            "Выбор места",
            "Здесь появится схема зала 5×8.\n" +
            $"Стоимость билета: {session.TicketPrice:0.00} ₽");
    }

    private void ShowTickets()
    {
        SetPage(
            "Мои билеты",
            "История покупок",
            TicketsButton);

        MoviesPage.IsVisible = false;
        PageHost.IsVisible = true;

        // Позже здесь будет TicketsView Алсу
        PageHost.Content = CreatePlaceholder(
            "Пока нет билетов",
            "Здесь будут отображаться купленные билеты.");
    }

    private Control CreatePlaceholder(
        string title,
        string description)
    {
        var panel = new StackPanel
        {
            Spacing = 12,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        panel.Children.Add(new TextBlock
        {
            Text = title,
            FontSize = 25,
            FontWeight = FontWeight.Bold,
            Foreground = Brush("#831843"),
            TextAlignment = TextAlignment.Center
        });

        panel.Children.Add(new TextBlock
        {
            Text = description,
            FontSize = 15,
            Foreground = Brush("#AD648A"),
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        });

        return new Border
        {
            Background = Brushes.White,
            CornerRadius = new CornerRadius(20),
            Padding = new Thickness(35),
            Child = panel
        };
    }
}
