using MauiCSS.Services;

namespace MauiCSS.ViewModels;

// MainViewModel coordinates the other ViewModels and handles communication between them
public class MainViewModel : BaseViewModel
{
    private readonly FileService _fileService = new FileService();
    private readonly RandomService _randomService = new RandomService();

    private string _statusMessage = "Wybierz lub utwórz klase";

    // fired when spin finish view reacts with confetti + sound
    public event EventHandler? SpinCompleted;

    public MainViewModel()
    {
        LuckyNumber = new LuckyNumberViewModel(_fileService, _randomService);
        Class = new ClassViewModel(_fileService);
        StudentList = new StudentListViewModel(_fileService);
        Draw = new DrawViewModel(_fileService, _randomService);

        // when class changes push new SchoolClass to StudentList and Draw
        Class.ClassChanged += (_, schoolClass) =>
        {
            StudentList.SetClass(schoolClass);
            Draw.SetClass(schoolClass);
            StatusMessage = Class.StatusMessage;
        };

        // when lucky number changes push new value to StudentList and Draw
        LuckyNumber.LuckyNumberChanged += (_, number) =>
        {
            StudentList.UpdateLuckyNumber(number);
            Draw.SetLuckyNumber(number);
        };

        // when a student is drawn highlight them in the list
        Draw.StudentDrawn += (_, studentNumber) =>
        {
            StudentList.HighlightStudent(studentNumber);
            StudentList.RefreshStudentsList();
            StatusMessage = Draw.StatusMessage;
        };

        // bubble SpinCompleted up to the View
        Draw.SpinCompleted += (s, e) => SpinCompleted?.Invoke(s, e);

        // bubble status messages from sub-viewModels
        StudentList.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(StudentListViewModel.StatusMessage))
                StatusMessage = StudentList.StatusMessage;
        };

        LuckyNumber.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(LuckyNumberViewModel.StatusMessage))
                StatusMessage = LuckyNumber.StatusMessage;
        };
    }

    public ClassViewModel Class { get; }
    public StudentListViewModel StudentList { get; }
    public LuckyNumberViewModel LuckyNumber { get; }
    public DrawViewModel Draw { get; }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }
}
