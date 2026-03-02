using System.Collections.ObjectModel;
using System.Windows.Input;
using LosowankoPytanko.Models;
using LosowankoPytanko.Services;
using Microsoft.Maui.Storage;
using System.Linq;
#if WINDOWS
using WinRT.Interop;
using Windows.Storage;
using Windows.Storage.Pickers;
using Microsoft.Maui.Platform;
#endif
using System.IO;
using System;
using System.Diagnostics;

namespace LosowankoPytanko.ViewModels;

public class MainViewModel : BaseViewModel
{
    private readonly FileService _fileService;
    private readonly RandomService _randomService;
    private readonly Random _slotRandom = new Random();

    private ObservableCollection<string> _classNames;
    private string _selectedClassName;
    private SchoolClass? _currentClass;
    private ObservableCollection<StudentViewModel> _students;
    private Student? _drawnStudent;
    private string _newClassName;
    private string _newStudentName;
    private int _luckyNumber;
    private string _statusMessage;
    private bool _isClassSelected;
    private bool _isEditing;
    private string _luckyNumberString;
    private int? _highlightedStudentNumber;
    private string _slotDigit1;
    private string _slotDigit2;
    private string _slotDigit3;
    private bool _isSpinning;
    private readonly RelayCommand _drawStudentCommand;

    public event EventHandler? SpinCompleted;

    public MainViewModel()
    {
        _fileService = new FileService();
        _randomService = new RandomService();
        _classNames = new ObservableCollection<string>();
        _students = new ObservableCollection<StudentViewModel>();
        _selectedClassName = string.Empty;
        _newClassName = string.Empty;
        _newStudentName = string.Empty;
        _statusMessage = "Wybierz lub utwórz klasę";
        _isClassSelected = false;
        _isEditing = false;
        _luckyNumberString = string.Empty;
        _slotDigit1 = "-";
        _slotDigit2 = "-";
        _slotDigit3 = "-";
        _isSpinning = false;
        _newSelectedClassName = string.Empty;

        CreateClassCommand = new RelayCommand(CreateClass);
        DeleteClassCommand = new RelayCommand(DeleteClass);
        RenameClassCommand = new RelayCommand(RenameClass);
        AddStudentCommand = new RelayCommand(AddStudent);
        RemoveStudentCommand = new RelayCommand<StudentViewModel>(RemoveStudent);
        _drawStudentCommand = new RelayCommand(DrawStudent, () => !_isSpinning);
        DrawStudentCommand = _drawStudentCommand;
        GenerateLuckyNumberCommand = new RelayCommand(GenerateLuckyNumber);
        ToggleEditModeCommand = new RelayCommand(ToggleEditMode);
        ResetQuestionedCommand = new RelayCommand(ResetQuestioned);
        ImportClassCommand = new RelayCommand(ImportClass);
        ExportClassCommand = new RelayCommand(ExportClass);

        LuckyNumber = _fileService.LoadGlobalLuckyNumber();
        LoadClassNames();
    }

    public ObservableCollection<string> ClassNames
    {
        get => _classNames;
        set => SetProperty(ref _classNames, value);
    }

    public string SelectedClassName
    {
        get => _selectedClassName;
        set
        {
            if (SetProperty(ref _selectedClassName, value))
            {
                LoadClass();
            }
        }
    }

    public ObservableCollection<StudentViewModel> Students
    {
        get => _students;
        set => SetProperty(ref _students, value);
    }

    public Student? DrawnStudent
    {
        get => _drawnStudent;
        set
        {
            if (SetProperty(ref _drawnStudent, value))
            {
                OnPropertyChanged(nameof(DrawnStudentDisplay));
            }
        }
    }

    public string DrawnStudentDisplay
    {
        get
        {
            if (_drawnStudent != null)
            {
                return $"{_drawnStudent.Name}\n(Nr {_drawnStudent.Number})";
            }
            if (_currentClass == null || _currentClass.Students.Count == 0)
            {
                return "Najpierw dodaj uczniów do klasy";
            }
            List<Student> available = _randomService.GetAvailableStudents(_currentClass, _luckyNumber);
            if (available.Count == 0)
            {
                return "Brak dostępnych uczniów!\nWszyscy są nieobecni, odpytani\nlub mają szczęśliwy numerek";
            }
            return "Kliknij przycisk poniżej";
        }
    }

    public string NewClassName
    {
        get => _newClassName;
        set => SetProperty(ref _newClassName, value);
    }

    public string NewStudentName
    {
        get => _newStudentName;
        set => SetProperty(ref _newStudentName, value);
    }

    public int LuckyNumber
    {
        get => _luckyNumber;
        set
        {
            if (SetProperty(ref _luckyNumber, value))
            {
                UpdateStudentStatuses();
                _fileService.SaveGlobalLuckyNumber(value);
                LuckyNumberString = value == 0 ? string.Empty : value.ToString();
            }
        }
    }

    public string LuckyNumberString
    {
        get => _luckyNumberString;
        set
        {
            if (SetProperty(ref _luckyNumberString, value))
            {
                if (int.TryParse(value, out int parsed))
                {
                    LuckyNumber = parsed;
                }
                else if (string.IsNullOrWhiteSpace(value))
                {
                    LuckyNumber = 0;
                }
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public bool IsClassSelected
    {
        get => _isClassSelected;
        set => SetProperty(ref _isClassSelected, value);
    }

    public bool IsEditing
    {
        get => _isEditing;
        set => SetProperty(ref _isEditing, value);
    }

    public string SlotDigit1
    {
        get => _slotDigit1;
        set => SetProperty(ref _slotDigit1, value);
    }

    public string SlotDigit2
    {
        get => _slotDigit2;
        set => SetProperty(ref _slotDigit2, value);
    }

    public string SlotDigit3
    {
        get => _slotDigit3;
        set => SetProperty(ref _slotDigit3, value);
    }

    public bool IsSpinning
    {
        get => _isSpinning;
        private set
        {
            if (SetProperty(ref _isSpinning, value))
            {
                _drawStudentCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public ICommand CreateClassCommand { get; }
    public ICommand DeleteClassCommand { get; }
    public ICommand RenameClassCommand { get; }
    public ICommand AddStudentCommand { get; }
    public ICommand RemoveStudentCommand { get; }
    public ICommand DrawStudentCommand { get; }
    public ICommand GenerateLuckyNumberCommand { get; }
    public ICommand ToggleEditModeCommand { get; }
    public ICommand ResetQuestionedCommand { get; }
    public ICommand ImportClassCommand { get; }
    public ICommand ExportClassCommand { get; }

    private string _newSelectedClassName; // pomocnicze pole dla zmiany nazwy
    public string NewSelectedClassName
    {
        get => _newSelectedClassName;
        set => SetProperty(ref _newSelectedClassName, value);
    }

    private void LoadClassNames()
    {
        ClassNames.Clear();
        List<string> names = _fileService.GetAllClassNames();
        foreach (string name in names)
        {
            ClassNames.Add(name);
        }
    }

    private void CreateClass()
    {
        if (string.IsNullOrWhiteSpace(NewClassName))
        {
            StatusMessage = "Podaj nazwę klasy!";
            return;
        }

        if (_fileService.ClassExists(NewClassName))
        {
            StatusMessage = "Klasa o tej nazwie już istnieje!";
            return;
        }

        SchoolClass newClass = new SchoolClass(NewClassName);
        _fileService.SaveClass(newClass);

        ClassNames.Add(NewClassName);
        SelectedClassName = NewClassName;
        NewClassName = string.Empty;

        StatusMessage = $"Utworzono klasę: {newClass.ClassName}";
    }

    private void LoadClass()
    {
        if (string.IsNullOrWhiteSpace(SelectedClassName))
        {
            IsClassSelected = false;
            ResetSlotDigits();
            return;
        }

        SchoolClass? loadedClass = _fileService.LoadClass(SelectedClassName);

        if (loadedClass != null)
        {
            _currentClass = loadedClass;
            _highlightedStudentNumber = null;
            RefreshStudentsList();
            IsClassSelected = true;
            StatusMessage = $"Załadowano klasę: {loadedClass.ClassName}";
            UpdateStudentStatuses();
            ResetSlotDigits();
        }
        else
        {
            StatusMessage = "Nie udało się załadować klasy!";
            IsClassSelected = false;
            ResetSlotDigits();
        }
    }

    private void DeleteClass()
    {
        if (string.IsNullOrWhiteSpace(SelectedClassName))
        {
            StatusMessage = "Wybierz klasę do usunięcia!";
            return;
        }

        _fileService.DeleteClass(SelectedClassName);
        ClassNames.Remove(SelectedClassName);

        _currentClass = null;
        Students.Clear();
        IsClassSelected = false;
        SelectedClassName = string.Empty;
        ResetSlotDigits();

        StatusMessage = "Usunięto klasę";
    }

    private void AddStudent()
    {
        if (_currentClass == null)
        {
            StatusMessage = "Najpierw wybierz lub utwórz klasę!";
            return;
        }

        if (string.IsNullOrWhiteSpace(NewStudentName))
        {
            StatusMessage = "Podaj imię i nazwisko ucznia!";
            return;
        }

        int newNumber = _currentClass.Students.Count + 1;
        Student newStudent = new Student(newNumber, NewStudentName);
        _currentClass.Students.Add(newStudent);

        RefreshStudentsList();
        _fileService.SaveClass(_currentClass);
        NewStudentName = string.Empty;

        StatusMessage = $"Dodano ucznia: {newStudent.Name}";
    }

    private void RemoveStudent(StudentViewModel? studentVm)
    {
        if (_currentClass == null || studentVm == null)
        {
            StatusMessage = "Wybierz ucznia do usunięcia!";
            return;
        }

        Student? studentToRemove = _currentClass.Students.Find(s => s.Number == studentVm.Number);

        if (studentToRemove != null)
        {
            _currentClass.Students.Remove(studentToRemove);
            RenumberStudents();
            RefreshStudentsList();
            _fileService.SaveClass(_currentClass);
            StatusMessage = $"Usunięto ucznia: {studentToRemove.Name}";
        }
    }

    private void RenumberStudents()
    {
        if (_currentClass == null) return;

        for (int i = 0; i < _currentClass.Students.Count; i++)
        {
            _currentClass.Students[i].Number = i + 1;
        }
    }

    private async void DrawStudent()
    {
        if (_isSpinning)
        {
            return;
        }

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
            _highlightedStudentNumber = null;
            UpdateHighlights();
            ResetSlotDigits();
            return;
        }

        int maxNumber = _currentClass.Students.Count > 0 ? _currentClass.Students.Max(s => s.Number) : selected.Number;

        try
        {
            IsSpinning = true;
            StatusMessage = "Losowanie w toku...";

            await SpinDigitsAsync(selected.Number, maxNumber);

            _randomService.MarkStudentAsQuestioned(_currentClass, selected);
            DrawnStudent = selected;
            _highlightedStudentNumber = selected.Number;
            UpdateHighlights();
            UpdateStudentStatuses();
            _fileService.SaveClass(_currentClass);
            StatusMessage = $"Wylosowano: {selected.Name} (nr {selected.Number})";

            SpinCompleted?.Invoke(this, EventArgs.Empty);
            _ = SpeakDrawnNumberAsync(selected.Number);
        }
        finally
        {
            IsSpinning = false;
        }
    }

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
        StatusMessage = $"Szczęśliwy numerek: {LuckyNumber}";
    }

    private void ToggleEditMode()
    {
        IsEditing = !IsEditing;
        StatusMessage = IsEditing ? "Tryb edycji włączony" : "Tryb edycji wyłączony";
    }

    private void ResetQuestioned()
    {
        if (_currentClass == null) return;

        foreach (Student student in _currentClass.Students)
        {
            student.WasQuestioned = false;
            student.QuestionedTurnsAgo = 0;
        }

        UpdateStudentStatuses();
        _fileService.SaveClass(_currentClass);
        StatusMessage = "Zresetowano stan odpytanych uczniów";
    }

    private void RefreshStudentsList()
    {
        Students.Clear();

        if (_currentClass == null) return;

        foreach (Student student in _currentClass.Students)
        {
            StudentViewModel studentVm = new StudentViewModel(student, _luckyNumber);
            studentVm.PresenceChanged += OnStudentPresenceChanged;
            studentVm.NameChanged += OnStudentNameChanged;
            studentVm.IsHighlighted = _highlightedStudentNumber == studentVm.Number;
            Students.Add(studentVm);
        }

        OnPropertyChanged(nameof(DrawnStudentDisplay));
    }

    private void UpdateStudentStatuses()
    {
        foreach (StudentViewModel studentVm in Students)
        {
            studentVm.UpdateLuckyNumber(_luckyNumber);
        }

        OnPropertyChanged(nameof(DrawnStudentDisplay));
    }

    private void OnStudentPresenceChanged(object? sender, EventArgs e)
    {
        if (_currentClass == null)
        {
            return;
        }

        _fileService.SaveClass(_currentClass);
        OnPropertyChanged(nameof(DrawnStudentDisplay));
        StatusMessage = "Zaktualizowano obecność ucznia";
    }

    private void OnStudentNameChanged(object? sender, EventArgs e)
    {
        if (_currentClass == null)
        {
            return;
        }

        _fileService.SaveClass(_currentClass);
        StatusMessage = "Zaktualizowano dane ucznia";
    }

    private void UpdateHighlights()
    {
        foreach (StudentViewModel studentVm in Students)
        {
            studentVm.IsHighlighted = _highlightedStudentNumber == studentVm.Number;
        }
    }

    private async void ImportClass()
    {
        if (_currentClass == null)
        {
            StatusMessage = "Wybierz klasę przed importem";
            return;
        }

        try
        {
            FilePickerFileType textFileType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.WinUI, new[] { ".txt" } }
            });

            FileResult? result = await FilePicker.PickAsync(new PickOptions
            {
                PickerTitle = "Wybierz plik z listą uczniów",
                FileTypes = textFileType
            });

            if (result == null)
            {
                StatusMessage = "Anulowano import";
                return;
            }

            List<Student> imported = _fileService.ImportStudentsFromFile(result.FullPath);
            if (imported.Count == 0)
            {
                StatusMessage = "Plik nie zawiera uczniów";
                return;
            }

            var mauiWindow = Application.Current?.Windows.FirstOrDefault();
            var page = mauiWindow?.Page;
            if (page == null)
            {
                StatusMessage = "Brak aktywnego okna do wyboru opcji";
                return;
            }

            string? choice = await page.DisplayActionSheet(
                "Import listy uczniów",
                "Anuluj",
                null,
                "Dodaj do listy",
                "Zastąp listę");

            if (choice == "Dodaj do listy")
            {
                AppendStudents(imported);
                StatusMessage = "Dodano uczniów z pliku";
            }
            else if (choice == "Zastąp listę")
            {
                ReplaceStudents(imported);
                StatusMessage = "Zastąpiono listę uczniów";
            }
            else
            {
                StatusMessage = "Anulowano import";
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Błąd importu: {ex.Message}";
            Debug.WriteLine(ex);
        }
    }

    private async void ExportClass()
    {
        if (_currentClass == null)
        {
            StatusMessage = "Brak klasy do eksportu";
            return;
        }

        try
        {
            string content = _fileService.GetStudentsExportContent(_currentClass);
            string safeName = string.Join("_", _currentClass.ClassName.Split(Path.GetInvalidFileNameChars()));
            string fileName = $"{safeName}-uczniowie-{DateTime.Now:yyyyMMdd-HHmmss}.txt";

            string? savedPath = await SaveExportWithPickerWindowsAsync(fileName, content);
            if (string.IsNullOrWhiteSpace(savedPath))
            {
                StatusMessage = "Anulowano eksport";
                return;
            }

            StatusMessage = $"Zapisano eksport: {savedPath}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Błąd eksportu: {ex.Message}";
        }
    }

    private void AppendStudents(IEnumerable<Student> imported)
    {
        if (_currentClass == null) return;

        int nextNumber = _currentClass.Students.Count + 1;
        foreach (Student student in imported)
        {
            student.Number = nextNumber++;
            _currentClass.Students.Add(student);
        }

        RefreshStudentsList();
        _fileService.SaveClass(_currentClass);
    }

    private void ReplaceStudents(IEnumerable<Student> imported)
    {
        if (_currentClass == null) return;

        _currentClass.Students.Clear();
        int number = 1;
        foreach (Student student in imported)
        {
            student.Number = number++;
            _currentClass.Students.Add(student);
        }

        RefreshStudentsList();
        _fileService.SaveClass(_currentClass);
    }

    private static async Task<string?> SaveExportWithPickerWindowsAsync(string suggestedFileName, string content)
    {
        FileSavePicker picker = new FileSavePicker
        {
            SuggestedFileName = suggestedFileName
        };
        picker.FileTypeChoices.Add("Plik tekstowy", new List<string> { ".txt" });

        Microsoft.Maui.Controls.Window? mauiWindow = Application.Current?.Windows.FirstOrDefault();
        if (mauiWindow?.Handler?.PlatformView is MauiWinUIWindow nativeWindow)
        {
            InitializeWithWindow.Initialize(picker, nativeWindow.WindowHandle);
        }

        StorageFile file = await picker.PickSaveFileAsync();
        if (file == null)
        {
            return null;
        }

        await FileIO.WriteTextAsync(file, content);
        return file.Path;
    }

    private async Task SpinDigitsAsync(int finalNumber, int maxNumber)
    {
        string finalText = finalNumber.ToString();
        int steps = 18;
        int delay = 55;
        int max = Math.Max(1, maxNumber);

        for (int i = 0; i < steps; i++)
        {
            SlotDigit1 = _slotRandom.Next(1, max + 1).ToString();
            if (i % 2 == 0)
            {
                SlotDigit2 = _slotRandom.Next(1, max + 1).ToString();
            }
            if (i % 3 == 0)
            {
                SlotDigit3 = _slotRandom.Next(1, max + 1).ToString();
            }
            await Task.Delay(delay);
        }

        // sekwencyjne ujawnienie finalnej wartości, po kolei
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

    private void RenameClass()
    {
        if (string.IsNullOrWhiteSpace(SelectedClassName))
        {
            StatusMessage = "Wybierz klasę do zmiany nazwy!";
            return;
        }

        if (string.IsNullOrWhiteSpace(NewSelectedClassName))
        {
            StatusMessage = "Podaj nową nazwę klasy!";
            return;
        }

        if (_fileService.ClassExists(NewSelectedClassName))
        {
            StatusMessage = "Klasa o takiej nazwie już istnieje!";
            return;
        }

        // próbujemy przeładować klasę, przypisać nową nazwę, zapisać pod nową nazwą i usunąć stary plik
        SchoolClass? loaded = _fileService.LoadClass(SelectedClassName);
        if (loaded == null)
        {
            StatusMessage = "Nie udało się załadować klasy do zmiany nazwy";
            return;
        }

        string oldName = loaded.ClassName;
        loaded.ClassName = NewSelectedClassName;
        _fileService.SaveClass(loaded);
        _fileService.DeleteClass(oldName);

        // aktualizujemy listę i wybór
        int idx = ClassNames.IndexOf(SelectedClassName);
        if (idx >= 0)
        {
            ClassNames[idx] = NewSelectedClassName;
        }
        SelectedClassName = NewSelectedClassName;
        NewSelectedClassName = string.Empty;
        StatusMessage = $"Zmieniono nazwę klasy na: {SelectedClassName}";
    }
}
