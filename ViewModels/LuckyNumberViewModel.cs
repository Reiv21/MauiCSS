using System.Windows.Input;
using MauiCSS.Services;

namespace MauiCSS.ViewModels;

public class LuckyNumberViewModel : BaseViewModel
{
    private readonly FileService _fileService;
    private readonly RandomService _randomService;

    private int _luckyNumber;
    private string _luckyNumberString = string.Empty;

    public event EventHandler<int>? LuckyNumberChanged;

    public LuckyNumberViewModel(FileService fileService, RandomService randomService)
    {
        _fileService = fileService;
        _randomService = randomService;

        GenerateLuckyNumberCommand = new RelayCommand(GenerateLuckyNumber);

        // load saved value on startup
        LuckyNumber = _fileService.LoadGlobalLuckyNumber();
    }

    public int LuckyNumber
    {
        get => _luckyNumber;
        set
        {
            if (SetProperty(ref _luckyNumber, value))
            {
                _fileService.SaveGlobalLuckyNumber(value);
                LuckyNumberString = value == 0 ? string.Empty : value.ToString();
                LuckyNumberChanged?.Invoke(this, value);
            }
        }
    }

    // string binding for the Entry converts back to int on change
    public string LuckyNumberString
    {
        get => _luckyNumberString;
        set
        {
            if (SetProperty(ref _luckyNumberString, value))
            {
                if (int.TryParse(value, out int parsed))
                    LuckyNumber = parsed;
                else if (string.IsNullOrWhiteSpace(value))
                    LuckyNumber = 0;
            }
        }
    }

    public string StatusMessage { get; private set; } = string.Empty;

    public ICommand GenerateLuckyNumberCommand { get; }

    private void GenerateLuckyNumber()
    {
        int maxNumber = _fileService.GetMaxStudentNumber();
        if (maxNumber <= 0)
        {
            StatusMessage = "Brak uczniów w klasach";
            LuckyNumber = 0;
            return;
        }

        LuckyNumber = _randomService.GenerateLuckyNumber(maxNumber);
        StatusMessage = $"Szczęliwy numerek: {LuckyNumber}";
    }
}

