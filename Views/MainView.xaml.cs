using System.IO;
using Microsoft.Maui.Storage;

namespace MauiCSS.Views;

public partial class MainView : ContentPage, IDisposable
{
    private readonly Random _rng = new();
    private bool _confettiRunning;
    private Windows.Media.Playback.MediaPlayer? _player;

    public MainView()
    {
        InitializeComponent();
    }

    // subscribe to SpinCompleted when BindingContext is set
    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        if (BindingContext is ViewModels.MainViewModel vm)
        {
            vm.SpinCompleted += OnSpinCompleted;
        }
    }

    private void OnSpinCompleted(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await PlaySoundAsync();
            await RunConfettiAsync();
        });
    }

    // Copies confetti.mp3 from MauiAsset to cache on first play
    // because MediaPlayer needs a file/URI not a stream
    private async Task PlaySoundAsync()
    {
        try
        {
            string path = Path.Combine(FileSystem.CacheDirectory, "confetti.mp3");

            if (!File.Exists(path))
            {
                using var src = await FileSystem.OpenAppPackageFileAsync("confetti.mp3");
                using var dst = File.Create(path);
                await src.CopyToAsync(dst);
            }

            _player ??= new Windows.Media.Playback.MediaPlayer();
            _player.Source = Windows.Media.Core.MediaSource.CreateFromUri(new Uri(path));
            _player.Volume = 0.8;
            _player.Play();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Sound] {ex.Message}");
        }
    }

    // spawns confetti
    private async Task RunConfettiAsync()
    {
        if (_confettiRunning || ConfettiLayer == null) return;

        _confettiRunning = true;
        ConfettiLayer.Children.Clear();

        await Task.Delay(16);

        double w = ConfettiLayer.Width;
        double h = ConfettiLayer.Height;

        if (w <= 0 || h <= 0)
        {
            _confettiRunning = false;
            return;
        }

        Color[] colors =
        {
            Color.FromArgb("#1a237e"),
            Color.FromArgb("#ff6f00"),
            Color.FromArgb("#f5f5f5"),
            Color.FromArgb("#4caf50"),
            Color.FromArgb("#e53935"),
        };

        var tasks = new List<Task>(30);

        for (int i = 0; i < 30; i++)
        {
            double size   = _rng.Next(6, 14);
            double startX = _rng.Next(0, (int)w);
            double drift  = _rng.Next(-60, 60);
            uint   time   = (uint)_rng.Next(700, 1400);

            var piece = new BoxView
            {
                WidthRequest  = size,
                HeightRequest = size * 0.6,
                Color         = colors[_rng.Next(colors.Length)],
                Opacity       = 0,
                Rotation      = _rng.Next(0, 360)
            };

            ConfettiLayer.Children.Add(piece);
            AbsoluteLayout.SetLayoutBounds(piece, new Rect(startX, -14, size, size * 0.6));

            tasks.Add(Task.WhenAll(
                piece.FadeTo(1, 100),
                piece.TranslateTo(drift, h + 20, time, Easing.CubicIn),
                piece.RotateTo(_rng.Next(-360, 360), time)));
        }

        await Task.WhenAll(tasks);
        ConfettiLayer.Children.Clear();
        _confettiRunning = false;
    }
    
    public void Dispose()
    {
        _player?.Dispose();
        _player = null;
    }
}
