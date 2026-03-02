namespace LosowankoPytanko.Views;

public partial class MainView : ContentPage
{
    private LosowankoPytanko.ViewModels.MainViewModel? _viewModel;
    private readonly Random _confettiRandom = new Random();
    private bool _isConfettiRunning;
    private Windows.Media.Playback.MediaPlayer? _mediaPlayer;

    public MainView()
    {
        InitializeComponent();
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        if (_viewModel != null)
        {
            _viewModel.SpinCompleted -= OnSpinCompleted;
        }

        _viewModel = BindingContext as LosowankoPytanko.ViewModels.MainViewModel;

        if (_viewModel != null)
        {
            _viewModel.SpinCompleted += OnSpinCompleted;
        }
    }

    private void OnSpinCompleted(object? sender, EventArgs e)
    {
        Microsoft.Maui.ApplicationModel.MainThread.BeginInvokeOnMainThread(async () =>
        {
            PlayConfettiSound();
            await RunConfettiAsync();
        });
    }

    private void PlayConfettiSound()
    {
        try
        {
            _mediaPlayer ??= new Windows.Media.Playback.MediaPlayer();
            _mediaPlayer.Source = Windows.Media.Core.MediaSource.CreateFromUri(new Uri("ms-appx:///confetti.mp3"));
            _mediaPlayer.Volume = 0.6;
            _mediaPlayer.Play();
        }
        catch
        {
        }
    }

    private async Task RunConfettiAsync()
    {
        if (_isConfettiRunning || ConfettiLayer == null)
        {
            return;
        }

        _isConfettiRunning = true;
        ConfettiLayer.Children.Clear();

        await Task.Delay(16);

        double width = ConfettiLayer.Width;
        double height = ConfettiLayer.Height;
        if (width <= 0 || height <= 0)
        {
            _isConfettiRunning = false;
            return;
        }

        ResourceDictionary? resources = Application.Current?.Resources;
        Color[] palette =
        {
            resources != null && resources.TryGetValue("Primary", out object p) ? (Color)p : Colors.DarkSlateBlue,
            resources != null && resources.TryGetValue("PrimaryDark", out object pd) ? (Color)pd : Colors.SlateBlue,
            resources != null && resources.TryGetValue("Secondary", out object s) ? (Color)s : Colors.LightGray
        };

        int count = 24;
        List<Task> animations = new List<Task>(count);

        for (int i = 0; i < count; i++)
        {
            double size = _confettiRandom.Next(6, 12);
            double startX = _confettiRandom.Next(0, Math.Max(1, (int)width));
            double drift = _confettiRandom.Next(-40, 40);
            uint duration = (uint)_confettiRandom.Next(800, 1300);
            double rotation = _confettiRandom.Next(-180, 180);

            BoxView piece = new BoxView
            {
                WidthRequest = size,
                HeightRequest = size * 0.7,
                Color = palette[_confettiRandom.Next(0, palette.Length)],
                Opacity = 0
            };

            ConfettiLayer.Children.Add(piece);
            AbsoluteLayout.SetLayoutBounds(piece, new Rect(startX, -10, size, size * 0.7));

            Task anim = Task.WhenAll(
                piece.FadeTo(1, 120),
                piece.TranslateTo(drift, height + 20, duration, Easing.CubicIn),
                piece.RotateTo(rotation, duration, Easing.CubicIn));

            animations.Add(anim);
        }

        await Task.WhenAll(animations);
        ConfettiLayer.Children.Clear();
        _isConfettiRunning = false;
    }
}
