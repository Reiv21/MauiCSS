using System.Windows.Input;
using MauiCSS.Models;
using MauiCSS.Services;
using System.Diagnostics;

namespace MauiCSS.ViewModels;

// ViewModel responsible for the "Losowanie" tab
// handles the logic of randomly selecting a student
// and animating the slot machine effect
public class DrawViewModel : BaseViewModel
{
    private readonly FileService _fileService;
    private readonly RandomService _randomService;
    private readonly Random _slotRandom = new Random();

    private SchoolClass? _currentClass;
    private int _luckyNumber;

    private Student? _drawnStudent;
    private string _slotDigit1 = "-";
    private string _slotDigit2 = "-";
    private string _slotDigit3 = "-";
    private bool _isSpinning;
    private readonly RelayCommand _drawStudentCommand;

    // fired when spin finishes View reacts with confetti + sound
    public event EventHandler? SpinCompleted;
    // fired so StudentListViewModel can highlight the drawn student
    public event EventHandler<int?>? StudentDrawn;

    public DrawViewModel(FileService fileService, RandomService randomService)
    {
        _fileService = fileService;
        _randomService = randomService;

        _drawStudentCommand = new RelayCommand(DrawStudent, () => !_isSpinning);
        DrawStudentCommand = _drawStudentCommand;
    }

    public Student? DrawnStudent
    {
        get => _drawnStudent;
        private set
        {
            if (SetProperty(ref _drawnStudent, value))
            {
                OnPropertyChanged(nameof(DrawnStudentDisplay));
                OnPropertyChanged(nameof(DrawnStudentName));
                OnPropertyChanged(nameof(DrawnStudentNumber));
            }
        }
    }

    public string DrawnStudentDisplay
    {
        get
        {
            if (_drawnStudent != null)
                return $"{_drawnStudent.Name}\n(Nr {_drawnStudent.Number})";

            if (_currentClass == null || _currentClass.Students.Count == 0)
                return "Najpierw dodaj uczniów do klasy";

            List<Student> available = _randomService.GetAvailableStudents(_currentClass, _luckyNumber);
            if (available.Count == 0)
                return "Brak dostępnych uczniów!\nWszyscy są nieobecni, odpytani\nlub mają szczęśliwy numerek";

            return "Kliknij przycisk poniżej";
        }
    }

    public string DrawnStudentName
    {
        get
        {
            if (_drawnStudent != null) return _drawnStudent.Name;
            if (_currentClass == null || _currentClass.Students.Count == 0)
                return "Najpierw dodaj uczniów do klasy";
            List<Student> available = _randomService.GetAvailableStudents(_currentClass, _luckyNumber);
            if (available.Count == 0)
                return "Brak dostępnych uczniów!";
            return "Kliknij przycisk poniżej";
        }
    }

    public string DrawnStudentNumber
    {
        get
        {
            if (_drawnStudent != null) return $"(Nr {_drawnStudent.Number})";
            if (_currentClass == null || _currentClass.Students.Count == 0)
                return " ";
            List<Student> available = _randomService.GetAvailableStudents(_currentClass, _luckyNumber);
            if (available.Count == 0)
                return "Wszyscy są nieobecni, odpytani lub mają szczęśliwy numerek";
            return " ";
        }
    }

    public string SlotDigit1
    {
        get => _slotDigit1;
        private set => SetProperty(ref _slotDigit1, value);
    }

    public string SlotDigit2
    {
        get => _slotDigit2;
        private set => SetProperty(ref _slotDigit2, value);
    }

    public string SlotDigit3
    {
        get => _slotDigit3;
        private set => SetProperty(ref _slotDigit3, value);
    }

    public bool IsSpinning
    {
        get => _isSpinning;
        private set
        {
            if (SetProperty(ref _isSpinning, value))
                _drawStudentCommand.RaiseCanExecuteChanged();
        }
    }

    public string StatusMessage { get; private set; } = string.Empty;

    public ICommand DrawStudentCommand { get; }

    // called by MainViewModel when selected class changes
    public void SetClass(SchoolClass? schoolClass)
    {
        _currentClass = schoolClass;
        DrawnStudent = null;
        ResetSlotDigits();
        OnPropertyChanged(nameof(DrawnStudentDisplay));
        OnPropertyChanged(nameof(DrawnStudentName));
        OnPropertyChanged(nameof(DrawnStudentNumber));
    }

    public void SetLuckyNumber(int luckyNumber)
    {
        _luckyNumber = luckyNumber;
        OnPropertyChanged(nameof(DrawnStudentDisplay));
        OnPropertyChanged(nameof(DrawnStudentName));
        OnPropertyChanged(nameof(DrawnStudentNumber));
    }

    private async void DrawStudent()
    {
        if (_isSpinning) return;

        if (_currentClass == null)
        {
            StatusMessage = "Najpierw wybierz klasę!";
            return;
        }

        Student? selected = _randomService.SelectRandomStudent(_currentClass, _luckyNumber);
        if (selected == null)
        {
            StatusMessage = "Brak dostępnych uczniów do losowania!";
            DrawnStudent = null;
            StudentDrawn?.Invoke(this, null);
            ResetSlotDigits();
            return;
        }

        int maxNumber = _currentClass.Students.Count > 0
            ? _currentClass.Students.Max(s => s.Number)
            : selected.Number;

        try
        {
            IsSpinning = true;

            await SpinDigitsAsync(selected.Number, maxNumber);

            _randomService.MarkStudentAsQuestioned(_currentClass, selected);
            _fileService.SaveClass(_currentClass);

            DrawnStudent = selected;
            StudentDrawn?.Invoke(this, selected.Number);
            StatusMessage = $"Wylosowano: {selected.Name} (nr {selected.Number})";

            SpinCompleted?.Invoke(this, EventArgs.Empty);
            _ = SpeakDrawnNumberAsync(selected.Number);
        }
        finally
        {
            IsSpinning = false;
        }
    }

    private async Task SpinDigitsAsync(int finalNumber, int maxNumber)
    {
        string finalText = finalNumber.ToString();
        int max = Math.Max(1, maxNumber);

        // 18 steps  digit1 spins every step, digit2 every 2nd, digit3 every 3rd
        // gives a "slowing down" effect
        for (int i = 0; i < 18; i++)
        {
            SlotDigit1 = _slotRandom.Next(1, max + 1).ToString();
            if (i % 2 == 0) SlotDigit2 = _slotRandom.Next(1, max + 1).ToString();
            if (i % 3 == 0) SlotDigit3 = _slotRandom.Next(1, max + 1).ToString();
            await Task.Delay(55);
        }

        // reveal final number one digit at a time
        SlotDigit1 = finalText;
        await Task.Delay(180);
        SlotDigit2 = finalText;
        await Task.Delay(180);
        SlotDigit3 = finalText;
    }

    private void ResetSlotDigits()
    {
        SlotDigit1 = "-";
        SlotDigit2 = "-";
        SlotDigit3 = "-";
    }

    private static async Task SpeakDrawnNumberAsync(int number)
    {
        try
        {
            await TextToSpeech.SpeakAsync($"Wylosowano numer {number}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
        }
    }
}

