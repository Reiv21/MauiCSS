using System.Collections.ObjectModel;
using System.Windows.Input;
using MauiCSS.Models;
using MauiCSS.Services;

namespace MauiCSS.ViewModels;

public class ClassViewModel : BaseViewModel
{
    private readonly FileService _fileService;

    private string _newClassName = string.Empty;
    private string _selectedClassName = string.Empty;
    private string _newSelectedClassName = string.Empty;
    private bool _isClassSelected;

    // fired when the selected class changes so other ViewModels can react
    public event EventHandler<SchoolClass?>? ClassChanged;
    public event Action? ClassListChanged;

    public ClassViewModel(FileService fileService)
    {
        _fileService = fileService;

        CreateClassCommand = new RelayCommand(CreateClass);
        DeleteClassCommand = new RelayCommand(DeleteClass);
        RenameClassCommand = new RelayCommand(RenameClass);

        LoadClassNames();
    }

    public ObservableCollection<string> ClassNames { get; } = new();

    public string NewClassName
    {
        get => _newClassName;
        set => SetProperty(ref _newClassName, value);
    }

    public string SelectedClassName
    {
        get => _selectedClassName;
        set
        {
            if (SetProperty(ref _selectedClassName, value))
                LoadClass();
        }
    }

    public string NewSelectedClassName
    {
        get => _newSelectedClassName;
        set => SetProperty(ref _newSelectedClassName, value);
    }

    public bool IsClassSelected
    {
        get => _isClassSelected;
        set => SetProperty(ref _isClassSelected, value);
    }

    public string StatusMessage { get; private set; } = string.Empty;

    public ICommand CreateClassCommand { get; }
    public ICommand DeleteClassCommand { get; }
    public ICommand RenameClassCommand { get; }

    public void LoadClassNames()
    {
        ClassNames.Clear();
        foreach (string name in _fileService.GetAllClassNames())
            ClassNames.Add(name);
    }

    private void LoadClass()
    {
        if (string.IsNullOrWhiteSpace(SelectedClassName))
        {
            IsClassSelected = false;
            ClassChanged?.Invoke(this, null);
            return;
        }

        SchoolClass? loaded = _fileService.LoadClass(SelectedClassName);
        IsClassSelected = loaded != null;
        StatusMessage = loaded != null
            ? $"Załadowano klasę: {loaded.ClassName}"
            : "Nie udało się załadować klasy!";

        ClassChanged?.Invoke(this, loaded);
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
        ClassListChanged?.Invoke();
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
        SelectedClassName = string.Empty;
        IsClassSelected = false;
        StatusMessage = "Usunięto klasę";
        ClassChanged?.Invoke(this, null);
        ClassListChanged?.Invoke();
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

        int idx = ClassNames.IndexOf(SelectedClassName);
        if (idx >= 0)
            ClassNames[idx] = NewSelectedClassName;

        SelectedClassName = NewSelectedClassName;
        NewSelectedClassName = string.Empty;
        StatusMessage = $"Zmieniono nazwę klasy na: {SelectedClassName}";
        ClassListChanged?.Invoke();
    }
}

