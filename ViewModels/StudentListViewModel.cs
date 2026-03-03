using System.Collections.ObjectModel;
using System.Windows.Input;
using MauiCSS.Models;
using MauiCSS.Services;
using WinRT.Interop;
using Windows.Storage;
using Windows.Storage.Pickers;
using Microsoft.Maui.Platform;
using System.IO;
using System.Diagnostics;

namespace MauiCSS.ViewModels;

public class StudentListViewModel : BaseViewModel
{
    private readonly FileService _fileService;

    private SchoolClass? _currentClass;
    private int _luckyNumber;
    private int? _highlightedStudentNumber;
    private string _newStudentName = string.Empty;
    private bool _isEditing;

    public StudentListViewModel(FileService fileService)
    {
        _fileService = fileService;

        AddStudentCommand = new RelayCommand(AddStudent);
        RemoveStudentCommand = new RelayCommand<StudentViewModel>(RemoveStudent);
        ToggleEditModeCommand = new RelayCommand(ToggleEditMode);
        ResetQuestionedCommand = new RelayCommand(ResetQuestioned);
        ImportClassCommand = new RelayCommand(ImportClass);
        ExportClassCommand = new RelayCommand(ExportClass);
    }

    public ObservableCollection<StudentViewModel> Students { get; } = new();

    public string NewStudentName
    {
        get => _newStudentName;
        set => SetProperty(ref _newStudentName, value);
    }

    public bool IsEditing
    {
        get => _isEditing;
        set => SetProperty(ref _isEditing, value);
    }

    public string StatusMessage { get; private set; } = string.Empty;

    public ICommand AddStudentCommand { get; }
    public ICommand RemoveStudentCommand { get; }
    public ICommand ToggleEditModeCommand { get; }
    public ICommand ResetQuestionedCommand { get; }
    public ICommand ImportClassCommand { get; }
    public ICommand ExportClassCommand { get; }

    // called by MainViewModel when the selected class changes
    public void SetClass(SchoolClass? schoolClass)
    {
        _currentClass = schoolClass;
        _highlightedStudentNumber = null;
        RefreshStudentsList();
    }

    // called by MainViewModel when lucky number changes
    public void UpdateLuckyNumber(int luckyNumber)
    {
        _luckyNumber = luckyNumber;
        foreach (StudentViewModel vm in Students)
            vm.UpdateLuckyNumber(luckyNumber);
    }

    // called by DrawViewModel after a student is drawn
    public void HighlightStudent(int? studentNumber)
    {
        _highlightedStudentNumber = studentNumber;
        UpdateHighlights();
    }

    public void RefreshStudentsList()
    {
        Students.Clear();
        if (_currentClass == null) return;

        foreach (Student student in _currentClass.Students)
        {
            StudentViewModel vm = new StudentViewModel(student, _luckyNumber);
            vm.PresenceChanged += OnStudentPresenceChanged;
            vm.NameChanged += OnStudentNameChanged;
            vm.IsHighlighted = _highlightedStudentNumber == vm.Number;
            Students.Add(vm);
        }
    }

    private void AddStudent()
    {
        if (_currentClass == null)
        {
            StatusMessage = "Najpierw wybierz lub utwórz klasę";
            return;
        }

        if (string.IsNullOrWhiteSpace(NewStudentName))
        {
            StatusMessage = "Podaj imie i nazwisko ucznia!";
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
            StatusMessage = "Wybierz ucznia do usuniecia!";
            return;
        }

        Student? toRemove = _currentClass.Students.Find(s => s.Number == studentVm.Number);
        if (toRemove == null) return;

        _currentClass.Students.Remove(toRemove);
        RenumberStudents();
        RefreshStudentsList();
        _fileService.SaveClass(_currentClass);
        StatusMessage = $"Usunięto ucznia: {toRemove.Name}";
    }

    private void RenumberStudents()
    {
        if (_currentClass == null) return;
        for (int i = 0; i < _currentClass.Students.Count; i++)
            _currentClass.Students[i].Number = i + 1;
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

        RefreshStudentsList();
        _fileService.SaveClass(_currentClass);
        StatusMessage = "Zresetowano stan odpytanych uczniów";
    }

    private void OnStudentPresenceChanged(object? sender, EventArgs e)
    {
        if (_currentClass == null) return;
        _fileService.SaveClass(_currentClass);
        StatusMessage = "Zaktualizowano obecność ucznia";
    }

    private void OnStudentNameChanged(object? sender, EventArgs e)
    {
        if (_currentClass == null) return;
        _fileService.SaveClass(_currentClass);
        StatusMessage = "Zaktualizowano dane ucznia";
    }

    private void UpdateHighlights()
    {
        foreach (StudentViewModel vm in Students)
            vm.IsHighlighted = _highlightedStudentNumber == vm.Number;
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
            FilePickerFileType textFileType = new FilePickerFileType(
                new Dictionary<DevicePlatform, IEnumerable<string>>
                {
                    { DevicePlatform.WinUI, new[] { ".txt" } }
                });

            FileResult? result = await FilePicker.PickAsync(new PickOptions
            {
                PickerTitle = "Wybierz plik z listę uczniów",
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

            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page == null) return;

            string? choice = await page.DisplayActionSheet(
                "Import listy uczniów", "Anuluj", null,
                "Dodaj do listy", "Zastąp liste");

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
            StatusMessage = $"Blad importu: {ex.Message}";
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

            string? savedPath = await SaveExportWithPickerAsync(fileName, content);
            StatusMessage = string.IsNullOrWhiteSpace(savedPath)
                ? "Anulowano eksport"
                : $"Zapisano eksport: {savedPath}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Bląd eksportu: {ex.Message}";
        }
    }

    private void AppendStudents(IEnumerable<Student> imported)
    {
        if (_currentClass == null) return;
        int next = _currentClass.Students.Count + 1;
        foreach (Student s in imported)
        {
            s.Number = next++;
            _currentClass.Students.Add(s);
        }
        RefreshStudentsList();
        _fileService.SaveClass(_currentClass);
    }

    private void ReplaceStudents(IEnumerable<Student> imported)
    {
        if (_currentClass == null) return;
        _currentClass.Students.Clear();
        int number = 1;
        foreach (Student s in imported)
        {
            s.Number = number++;
            _currentClass.Students.Add(s);
        }
        RefreshStudentsList();
        _fileService.SaveClass(_currentClass);
    }

    private static async Task<string?> SaveExportWithPickerAsync(string suggestedFileName, string content)
    {
        FileSavePicker picker = new FileSavePicker { SuggestedFileName = suggestedFileName };
        picker.FileTypeChoices.Add("Plik tekstowy", new List<string> { ".txt" });

        var mauiWindow = Application.Current?.Windows.FirstOrDefault();
        if (mauiWindow?.Handler?.PlatformView is MauiWinUIWindow nativeWindow)
            InitializeWithWindow.Initialize(picker, nativeWindow.WindowHandle);

        StorageFile file = await picker.PickSaveFileAsync();
        if (file == null) return null;

        await FileIO.WriteTextAsync(file, content);
        return file.Path;
    }
}

