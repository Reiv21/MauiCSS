namespace MauiCSS.Models;

public class Student
{
    public int Number { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsPresent { get; set; } = true;
    public bool WasQuestioned { get; set; }
    public int QuestionedTurnsAgo { get; set; }

    public Student()
    {
        // used for TryParseStudentLine in FileService.cs
        //i change properties one by one becase of parsing errors
    }

    public Student(int number, string name)
    {
        Number = number;
        Name = name;
        IsPresent = true;
        WasQuestioned = false;
        QuestionedTurnsAgo = 0;
    }
}

