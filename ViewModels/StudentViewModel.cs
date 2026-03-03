using MauiCSS.Models;

namespace MauiCSS.ViewModels;

public class StudentViewModel : BaseViewModel
{
    private readonly Student _student;
    private bool _isLuckyNumber;
    private string _statusText;
    private Color _statusColor;
    private bool _isHighlighted;

    public event EventHandler? PresenceChanged;
    public event EventHandler? NameChanged;

    public StudentViewModel(Student student, int luckyNumber)
    {
        _student = student;
        _statusText = string.Empty;
        // initially set to transparent so the status box does not take up space when empty
        _statusColor = Colors.Transparent;
        UpdateLuckyNumber(luckyNumber);
        UpdateStatus();
    }

    public int Number => _student.Number;
    
    public string Name
    {
        get => _student.Name;
        set
        {
            if (_student.Name != value)
            {
                _student.Name = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DisplayText));
                NameChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public bool IsPresent
    {
        get => _student.IsPresent;
        set
        {
            if (_student.IsPresent != value)
            {
                _student.IsPresent = value;
                OnPropertyChanged();
                UpdateStatus();
                PresenceChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public bool WasQuestioned => _student.WasQuestioned;
    
    public int QuestionedTurnsAgo => _student.QuestionedTurnsAgo;

    public bool IsLuckyNumber
    {
        get => _isLuckyNumber;
        private set => SetProperty(ref _isLuckyNumber, value);
    }

    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    public Color StatusColor
    {
        get => _statusColor;
        private set => SetProperty(ref _statusColor, value);
    }

    public bool IsHighlighted
    {
        get => _isHighlighted;
        set => SetProperty(ref _isHighlighted, value);
    }

    public string DisplayText => $"{Number}. {Name}";

    public bool CanBeDrawn => IsPresent && !IsLuckyNumber && !WasQuestioned;

    public void UpdateLuckyNumber(int luckyNumber)
    {
        IsLuckyNumber = _student.Number == luckyNumber;
        UpdateStatus();
    }

    private void UpdateStatus()
    {
        if (!IsPresent)
        {
            StatusText = "Nieobecny";
            StatusColor = Color.FromArgb("#757575");
        }
        else if (IsLuckyNumber)
        {
            StatusText = "Szczęśliwy numerek!";
            StatusColor = Color.FromArgb("#ff6f00");
        }
        else if (WasQuestioned)
        {
            StatusText = $"Odpytany ({3 - QuestionedTurnsAgo} tur do powrotu)";
            StatusColor = Color.FromArgb("#ff6f00");
        }
        else
        {
            StatusText = "Dostępny";
            StatusColor = Color.FromArgb("#1a237e");
        }

        OnPropertyChanged(nameof(DisplayText));
        OnPropertyChanged(nameof(CanBeDrawn));
        OnPropertyChanged(nameof(WasQuestioned));
        OnPropertyChanged(nameof(QuestionedTurnsAgo));
    }
}
