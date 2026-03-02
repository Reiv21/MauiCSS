namespace LosowankoPytanko.Models;

public class SchoolClass
{
    public string ClassName { get; set; } = string.Empty;
    public List<Student> Students { get; set; } = new List<Student>();
    public int LuckyNumber { get; set; } = 0;

    public SchoolClass()
    {
    }

    public SchoolClass(string className)
    {
        ClassName = className;
        Students = new List<Student>();
        LuckyNumber = 0;
    }
}

