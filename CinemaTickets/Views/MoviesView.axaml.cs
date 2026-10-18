using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;
using CinemaTickets.Data;
using CinemaTickets.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaTickets.Views
{
    public partial class MoviesView : UserControl
    {
        private enum DialogKind { Info, Warning, Error, Question }

        private List<Movie> _allMovies = new();

        public event Action<Movie>? MovieSelected;

        public MoviesView()
        {
            InitializeComponent();
            SearchBox.TextChanged += (_, _) => RenderMovies();
            AddButton.Click += async (_, _) => await OpenEditAsync(null);
            Loaded += async (_, _) => await LoadMoviesAsync();
        }


        public async Task LoadMoviesAsync()
        {
            try
            {
                StatusText.Text = "Загрузка фильмов...";
                using var db = new CinemaDbContextFactory().CreateDbContext(Array.Empty<string>());
                _allMovies = await db.Movies.AsNoTracking().OrderBy(m => m.Title).ToListAsync();
            }
            catch (Exception ex)
            {
                MoviesPanel.Children.Clear();
                StatusText.Text = "Не удалось подключиться к базе данных";
                await ShowDialogAsync("Ошибка подключения",
                    "Не удалось загрузить фильмы. Проверьте, что PostgreSQL запущен.\n\n" + ex.Message,
                    DialogKind.Error);
                return;
            }
            RenderMovies();
        }

        private void RenderMovies()
        {
            MoviesPanel.Children.Clear();

            var query = SearchBox.Text?.Trim() ?? "";
            var movies = _allMovies
                .Where(m => m.Title.Contains(query, StringComparison.OrdinalIgnoreCase))
                .ToList();

            StatusText.Text = movies.Count == 0
                ? "Фильмы не найдены"
                : $"Фильмов в афише: {movies.Count}";

            foreach (var movie in movies)
                MoviesPanel.Children.Add(CreateCard(movie));
        }


        private static IBrush PosterBrush(int movieId)
        {
            string c1, c2;
            if (movieId % 3 == 1) { c1 = "#542251"; c2 = "#B24B93"; }
            else if (movieId % 3 == 2) { c1 = "#782753"; c2 = "#E07A9E"; }
            else { c1 = "#95465E"; c2 = "#E7A37F"; }

            return new LinearGradientBrush
            {
                StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
                EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
                GradientStops = new GradientStops
                {
                    new GradientStop(Color.Parse(c1), 0),
                    new GradientStop(Color.Parse(c2), 1)
                }
            };
        }

        private static string FormatDuration(int minutes)
        {
            int h = minutes / 60, m = minutes % 60;
            if (h == 0) return $"{m} мин";
            return m == 0 ? $"{h} ч" : $"{h} ч {m} мин";
        }

        private static Border MakeChip(string text, double fontSize = 11, Thickness? padding = null)
        {
            return new Border
            {
                Background = Brush.Parse("#FDF2F8"),
                BorderBrush = Brush.Parse("#FBCFE8"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(12),
                Padding = padding ?? new Thickness(10, 4),
                Margin = new Thickness(0, 0, 6, 6),
                Child = new TextBlock
                {
                    Text = text,
                    FontSize = fontSize,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = Brush.Parse("#BE185D"),
                    MaxWidth = 200,
                    TextTrimming = TextTrimming.CharacterEllipsis
                }
            };
        }

        private Control CreateCard(Movie movie)
        {
            var poster = new Grid { Height = 150, Background = PosterBrush(movie.Id) };

            poster.Children.Add(new Border
            {
                Width = 130,
                Height = 130,
                CornerRadius = new CornerRadius(65),
                Background = Brush.Parse("#20FFFFFF"),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, -40, -40)
            });

            poster.Children.Add(new TextBlock
            {
                Text = $"PINK CINEMA / {movie.ReleaseYear}",
                FontSize = 11,
                FontWeight = FontWeight.SemiBold,
                Foreground = Brush.Parse("#FFE4F2"),
                Margin = new Thickness(20, 20, 0, 0),
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left
            });

            poster.Children.Add(new TextBlock
            {
                Text = movie.Title,
                FontSize = 22,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxLines = 2,
                Margin = new Thickness(20, 0, 20, 18),
                VerticalAlignment = VerticalAlignment.Bottom
            });

            var deleteBtn = new Button
            {
                Content = new PathIcon
                {
                    Width = 16,
                    Height = 16,
                    Data = Geometry.Parse("M19,4H15.5L14.5,3H9.5L8.5,4H5V6H19M6,19A2,2 0 0,0 8,21H16A2,2 0 0,0 18,19V7H6V19Z")
                },
                Width = 32,
                Height = 32,
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 12, 12, 0)
            };
            deleteBtn.Classes.Add("ghost");
            ToolTip.SetTip(deleteBtn, "Удалить фильм");
            deleteBtn.Click += async (_, _) => await DeleteMovieAsync(movie);
            poster.Children.Add(deleteBtn);

            var chips = new WrapPanel { Orientation = Orientation.Horizontal };
            chips.Children.Add(MakeChip(movie.Genre));
            chips.Children.Add(MakeChip(FormatDuration(movie.DurationMinutes)));

            var description = new TextBlock
            {
                Text = movie.Description,
                FontSize = 13,
                LineHeight = 18,
                Foreground = Brush.Parse("#965A7A"),
                TextWrapping = TextWrapping.Wrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxLines = 3,
                Height = 54
            };

            var sessionsBtn = new Button
            {
                Content = "Смотреть сеансы →",
                Height = 44,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            sessionsBtn.Classes.Add("pink");
            sessionsBtn.Click += (_, _) => MovieSelected?.Invoke(movie);

            var detailsBtn = MakeOutlineButton("Подробнее");
            detailsBtn.Click += async (_, _) => await ShowDetailsAsync(movie);

            var editBtn = MakeOutlineButton("Изменить");
            editBtn.Click += async (_, _) => await OpenEditAsync(movie);

            var row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*"), ColumnSpacing = 8 };
            Grid.SetColumn(detailsBtn, 0);
            Grid.SetColumn(editBtn, 1);
            row.Children.Add(detailsBtn);
            row.Children.Add(editBtn);

            var info = new StackPanel { Margin = new Thickness(18, 16, 18, 20), Spacing = 10 };
            info.Children.Add(chips);
            info.Children.Add(description);
            info.Children.Add(new Border { Height = 4 });
            info.Children.Add(sessionsBtn);
            info.Children.Add(row);

            var content = new StackPanel();
            content.Children.Add(poster);
            content.Children.Add(info);

            var inner = new Border
            {
                CornerRadius = new CornerRadius(20),
                Background = Brushes.White,
                BorderBrush = Brush.Parse("#F1DCE7"),
                BorderThickness = new Thickness(1),
                ClipToBounds = true,
                Child = content
            };

            return new Border
            {
                Width = 284,
                Margin = new Thickness(0, 0, 22, 24),
                CornerRadius = new CornerRadius(20),
                BoxShadow = BoxShadows.Parse("0 8 22 0 #1F9D174D"),
                Child = inner
            };
        }

        private static Button MakeOutlineButton(string text)
        {
            var btn = new Button
            {
                Content = text,
                Height = 38,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            btn.Classes.Add("outline");
            return btn;
        }


        private async Task OpenEditAsync(Movie? movie)
        {
            if (TopLevel.GetTopLevel(this) is not Window owner) return;

            var saved = await new MovieEditWindow(movie).ShowDialog<bool?>(owner);
            if (saved == true)
                await LoadMoviesAsync();
        }

        private async Task DeleteMovieAsync(Movie movie)
        {
            try
            {
                using var db = new CinemaDbContextFactory().CreateDbContext(Array.Empty<string>());

                if (await db.Sessions.AnyAsync(s => s.MovieId == movie.Id))
                {
                    await ShowDialogAsync("Удаление невозможно",
                        $"У фильма «{movie.Title}» уже есть сеансы. Сначала удалите сеансы, потом фильм.",
                        DialogKind.Warning);
                    return;
                }

                if (!await ShowDialogAsync("Удалить фильм?",
                        $"Фильм «{movie.Title}» будет удалён из афиши. Это действие нельзя отменить.",
                        DialogKind.Question, confirm: true, okText: "Удалить"))
                    return;

                db.Movies.Remove(new Movie { Id = movie.Id });
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                await ShowDialogAsync("Ошибка",
                    "Не удалось удалить фильм.\n\n" + ex.Message, DialogKind.Error);
                return;
            }
            await LoadMoviesAsync();
        }


        private static Button MakeDialogButton(string text, bool primary)
        {
            var btn = new Button
            {
                Content = text,
                Background = primary ? Brush.Parse("#BE185D") : Brushes.White,
                Foreground = primary ? Brushes.White : Brush.Parse("#BE185D"),
                BorderBrush = primary ? Brush.Parse("#BE185D") : Brush.Parse("#FBCFE8"),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(24, 10),
                FontWeight = FontWeight.SemiBold,
                HorizontalContentAlignment = HorizontalAlignment.Center
            };
            btn.Classes.Add(primary ? "dlgPrimary" : "dlgOutline");
            return btn;
        }

        private static Style HoverStyle(string cssClass, string bg)
        {
            var style = new Style(x => x.OfType<Button>().Class(cssClass).Class(":pointerover")
                .Template().OfType<ContentPresenter>().Name("PART_ContentPresenter"));
            style.Setters.Add(new Setter(ContentPresenter.BackgroundProperty, Brush.Parse(bg)));
            style.Setters.Add(new Setter(ContentPresenter.ForegroundProperty, Brush.Parse("#BE185D")));
            style.Setters.Add(new Setter(ContentPresenter.BorderBrushProperty, Brush.Parse("#BE185D")));
            return style;
        }

        private static Window CreateDialogWindow(string title, double width)
        {
            var w = new Window
            {
                Title = title,
                Width = width,
                SizeToContent = SizeToContent.Height,
                CanResize = false,
                ShowInTaskbar = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Background = Brush.Parse("#FFF8FC")
            };
            w.Styles.Add(HoverStyle("dlgPrimary", "#FFFFFF"));
            w.Styles.Add(HoverStyle("dlgOutline", "#FFF0F7"));
            return w;
        }

        private async Task<bool> ShowDialogAsync(string title, string message,
            DialogKind kind = DialogKind.Info, bool confirm = false, string? okText = null)
        {
            if (TopLevel.GetTopLevel(this) is not Window owner) return false;

            var (icon, bg, fg) = kind switch
            {
                DialogKind.Warning => ("!", "#FEF3C7", "#B45309"),
                DialogKind.Error => ("✕", "#FFE4E6", "#BE123C"),
                DialogKind.Question => ("?", "#FBCFE8", "#BE185D"),
                _ => ("i", "#FBCFE8", "#BE185D")
            };

            var dialog = CreateDialogWindow(title, 460);

            var iconCircle = new Border
            {
                Width = 52,
                Height = 52,
                CornerRadius = new CornerRadius(26),
                Background = Brush.Parse(bg),
                VerticalAlignment = VerticalAlignment.Top,
                Child = new TextBlock
                {
                    Text = icon,
                    FontSize = 26,
                    FontWeight = FontWeight.Bold,
                    Foreground = Brush.Parse(fg),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };

            var texts = new StackPanel { Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
            texts.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 20,
                FontWeight = FontWeight.Bold,
                Foreground = Brush.Parse("#831843"),
                TextWrapping = TextWrapping.Wrap
            });
            texts.Children.Add(new TextBlock
            {
                Text = message,
                FontSize = 14,
                LineHeight = 21,
                Foreground = Brush.Parse("#965A7A"),
                TextWrapping = TextWrapping.Wrap
            });

            var top = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*"), ColumnSpacing = 18 };
            Grid.SetColumn(iconCircle, 0);
            Grid.SetColumn(texts, 1);
            top.Children.Add(iconCircle);
            top.Children.Add(texts);

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Spacing = 10
            };
            if (confirm)
            {
                var cancel = MakeDialogButton("Отмена", false);
                cancel.Click += (_, _) => dialog.Close(false);
                buttons.Children.Add(cancel);
            }
            var ok = MakeDialogButton(okText ?? (confirm ? "Да" : "Понятно"), true);
            ok.Click += (_, _) => dialog.Close(true);
            buttons.Children.Add(ok);

            var body = new StackPanel { Margin = new Thickness(28, 24, 28, 22), Spacing = 24 };
            body.Children.Add(top);
            body.Children.Add(buttons);

            var root = new StackPanel();
            root.Children.Add(new Border { Height = 6, Background = Brush.Parse("#BE185D") });
            root.Children.Add(body);
            dialog.Content = root;

            return await dialog.ShowDialog<bool?>(owner) == true;
        }

        private async Task ShowDetailsAsync(Movie movie)
        {
            if (TopLevel.GetTopLevel(this) is not Window owner) return;

            var dialog = CreateDialogWindow("О фильме", 480);

            var poster = new Grid { Height = 150, Background = PosterBrush(movie.Id) };
            poster.Children.Add(new Border
            {
                Width = 150,
                Height = 150,
                CornerRadius = new CornerRadius(75),
                Background = Brush.Parse("#20FFFFFF"),
                HorizontalAlignment = HorizontalAlignment.Right,
                VerticalAlignment = VerticalAlignment.Bottom,
                Margin = new Thickness(0, 0, -45, -45)
            });
            poster.Children.Add(new TextBlock
            {
                Text = $"PINK CINEMA / {movie.ReleaseYear}",
                FontSize = 11,
                FontWeight = FontWeight.SemiBold,
                Foreground = Brush.Parse("#FFE4F2"),
                Margin = new Thickness(28, 22, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            });
            poster.Children.Add(new TextBlock
            {
                Text = movie.Title,
                FontSize = 26,
                FontWeight = FontWeight.Bold,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap,
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxLines = 2,
                Margin = new Thickness(28, 0, 28, 20),
                VerticalAlignment = VerticalAlignment.Bottom
            });

            var chips = new WrapPanel { Orientation = Orientation.Horizontal };
            chips.Children.Add(MakeChip(movie.Genre, 12, new Thickness(12, 5)));
            chips.Children.Add(MakeChip(movie.ReleaseYear + " год", 12, new Thickness(12, 5)));
            chips.Children.Add(MakeChip(FormatDuration(movie.DurationMinutes), 12, new Thickness(12, 5)));

            var close = MakeDialogButton("Закрыть", true);
            close.HorizontalAlignment = HorizontalAlignment.Right;
            close.Margin = new Thickness(0, 6, 0, 0);
            close.Click += (_, _) => dialog.Close();

            var body = new StackPanel { Margin = new Thickness(28, 22, 28, 24), Spacing = 16 };
            body.Children.Add(chips);
            body.Children.Add(new TextBlock
            {
                Text = "О ФИЛЬМЕ",
                FontSize = 11,
                FontWeight = FontWeight.SemiBold,
                Foreground = Brush.Parse("#AD648A")
            });
            body.Children.Add(new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(movie.Description)
                    ? "Описание пока не добавлено."
                    : movie.Description,
                FontSize = 14,
                LineHeight = 22,
                Foreground = Brush.Parse("#831843"),
                TextWrapping = TextWrapping.Wrap
            });
            body.Children.Add(close);

            var root = new StackPanel();
            root.Children.Add(poster);
            root.Children.Add(body);
            dialog.Content = root;

            await dialog.ShowDialog(owner);
        }
    }
}