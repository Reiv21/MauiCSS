namespace MauiCSS.Models;

public class SchoolClass
{
    public string ClassName { get; set; } = string.Empty;
    public List<Student> Students { get; set; } = new List<Student>();

    public SchoolClass()
    {
        // required by ParseClassLines() in FileService — fields are set manually after parsing
    }

    public SchoolClass(string className)
    {
        ClassName = className;
    }
}

