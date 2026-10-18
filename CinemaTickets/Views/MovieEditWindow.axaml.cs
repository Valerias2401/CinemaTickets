using System;
using System.Linq;
using Avalonia.Controls;
using CinemaTickets.Data;
using CinemaTickets.Models;
using Microsoft.EntityFrameworkCore;

namespace CinemaTickets.Views
{
    public partial class MovieEditWindow : Window
    {
        private readonly Movie? _movie; 

        public MovieEditWindow() : this(null) { }

        public MovieEditWindow(Movie? movie)
        {
            InitializeComponent();
            _movie = movie;

            if (movie == null)
            {
                Title = "Новый фильм";
                HeaderText.Text = "Новый фильм";
            }
            else
            {
                Title = "Редактирование фильма";
                HeaderText.Text = "Редактирование";
                TitleBox.Text = movie.Title;
                GenreBox.Text = movie.Genre;
                YearBox.Text = movie.ReleaseYear.ToString();
                DurationBox.Text = movie.DurationMinutes.ToString();
                DescriptionBox.Text = movie.Description;
            }

            CancelButton.Click += (_, _) => Close(false);
            SaveButton.Click += async (_, _) => await SaveAsync();
        }

        private static void SetError(TextBlock block, string? text)
        {
            block.Text = text ?? "";
            block.IsVisible = text != null;
        }

        private bool Validate(out string title, out string genre, out int year,
                              out int duration, out string description)
        {
            title = (TitleBox.Text ?? "").Trim();
            genre = (GenreBox.Text ?? "").Trim();
            description = (DescriptionBox.Text ?? "").Trim();
            year = 0;
            duration = 0;
            bool ok = true;

            SetError(ErrGeneral, null);

            if (title.Length == 0) { SetError(ErrTitle, "Введите название фильма"); ok = false; }
            else SetError(ErrTitle, null);

            if (genre.Length == 0) { SetError(ErrGenre, "Введите жанр"); ok = false; }
            else SetError(ErrGenre, null);

            int maxYear = DateTime.Now.Year + 5;
            if (!int.TryParse(YearBox.Text?.Trim(), out year))
            { SetError(ErrYear, "Введите год цифрами"); ok = false; }
            else if (year < 1888 || year > maxYear)
            { SetError(ErrYear, $"Год от 1888 до {maxYear}"); ok = false; }
            else SetError(ErrYear, null);

            if (!int.TryParse(DurationBox.Text?.Trim(), out duration))
            { SetError(ErrDuration, "Введите минуты цифрами"); ok = false; }
            else if (duration < 1 || duration > 600)
            { SetError(ErrDuration, "От 1 до 600 минут"); ok = false; }
            else SetError(ErrDuration, null);

            if (description.Length > 500)
            { SetError(ErrDescription, "Описание не длиннее 500 символов"); ok = false; }
            else SetError(ErrDescription, null);

            return ok;
        }

        private async System.Threading.Tasks.Task SaveAsync()
        {
            if (!Validate(out var title, out var genre, out var year,
                          out var duration, out var description))
                return;

            SaveButton.IsEnabled = false;
            try
            {
                using var db = new CinemaDbContextFactory().CreateDbContext(Array.Empty<string>());

                int currentId = _movie?.Id ?? 0;
                bool duplicate = await db.Movies.AnyAsync(m =>
                    m.Id != currentId && m.Title == title && m.ReleaseYear == year);
                if (duplicate)
                {
                    SetError(ErrTitle, "Фильм с таким названием и годом уже есть в афише");
                    return;
                }

                if (_movie == null)
                {
                    db.Movies.Add(new Movie
                    {
                        Title = title,
                        Genre = genre,
                        ReleaseYear = year,
                        DurationMinutes = duration,
                        Description = description
                    });
                }
                else
                {
                    var entity = await db.Movies.FirstOrDefaultAsync(m => m.Id == _movie.Id);
                    if (entity == null)
                    {
                        SetError(ErrGeneral, "Этот фильм уже удалён из базы.");
                        return;
                    }
                    entity.Title = title;
                    entity.Genre = genre;
                    entity.ReleaseYear = year;
                    entity.DurationMinutes = duration;
                    entity.Description = description;
                }

                await db.SaveChangesAsync();
                Close(true);
            }
            catch (Exception ex)
            {
                SetError(ErrGeneral, "Не удалось сохранить: " + ex.Message);
            }
            finally
            {
                SaveButton.IsEnabled = true;
            }
        }
    }
}