using LosowankoPytanko.Models;

namespace LosowankoPytanko.Services;

public class FileService
{
    private readonly string _dataDirectory;
    private readonly string _globalLuckyNumberFilePath;

    public FileService()
    {
        _dataDirectory = Path.Combine(FileSystem.AppDataDirectory, "Classes");
        _globalLuckyNumberFilePath = Path.Combine(FileSystem.AppDataDirectory, "global-lucky-number.txt");
        if (!Directory.Exists(_dataDirectory))
        {
            Directory.CreateDirectory(_dataDirectory);
        }
    }
    
    public void SaveClass(SchoolClass schoolClass)
    {
        string filePath = GetFilePath(schoolClass.ClassName);
        List<string> lines = CreateClassLines(schoolClass);
        File.WriteAllLines(filePath, lines);
    }
    
    public SchoolClass? LoadClass(string className)
    {
        string filePath = GetFilePath(className);
        
        if (!File.Exists(filePath))
        {
            return null;
        }

        string[] lines = File.ReadAllLines(filePath);
        return ParseClassLines(lines);
    }
    
    public List<string> GetAllClassNames()
    {
        List<string> classNames = new List<string>();
        
        if (Directory.Exists(_dataDirectory))
        {
            string[] files = Directory.GetFiles(_dataDirectory, "*.txt");
            
            foreach (string file in files)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                classNames.Add(fileName);
            }
        }
        
        return classNames;
    }
    
    public void DeleteClass(string className)
    {
        string filePath = GetFilePath(className);
        
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
    }
    
    public bool ClassExists(string className)
    {
        string filePath = GetFilePath(className);
        return File.Exists(filePath);
    }

    public int LoadGlobalLuckyNumber()
    {
        if (!File.Exists(_globalLuckyNumberFilePath))
        {
            return 0;
        }

        string content = File.ReadAllText(_globalLuckyNumberFilePath).Trim();
        return int.TryParse(content, out int value) ? value : 0;
    }

    public void SaveGlobalLuckyNumber(int luckyNumber)
    {
        File.WriteAllText(_globalLuckyNumberFilePath, luckyNumber.ToString());
    }

    public int GetMaxStudentNumber()
    {
        int maxNumber = 0;
        foreach (string className in GetAllClassNames())
        {
            SchoolClass? schoolClass = LoadClass(className);
            if (schoolClass == null)
            {
                continue;
            }

            if (schoolClass.Students.Count > 0)
            {
                int classMax = schoolClass.Students.Max(s => s.Number);
                if (classMax > maxNumber)
                {
                    maxNumber = classMax;
                }
            }
        }

        return maxNumber;
    }

    public List<Student> ImportStudentsFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return new List<Student>();
        }

        string[] lines = File.ReadAllLines(filePath);
        return ParseStudentLines(lines);
    }

    public string GetStudentsExportContent(SchoolClass schoolClass)
    {
        List<string> lines = CreateStudentLines(schoolClass.Students);
        return string.Join(Environment.NewLine, lines);
    }

    private List<string> CreateClassLines(SchoolClass schoolClass)
    {
        List<string> lines = new List<string>
        {
            schoolClass.ClassName,
            "0"
        };

        lines.AddRange(CreateStudentLines(schoolClass.Students));
        return lines;
    }

    private static List<string> CreateStudentLines(IEnumerable<Student> students)
    {
        List<string> lines = new List<string>();
        foreach (Student student in students)
        {
            string line = $"{student.Number}|{student.Name}|{student.IsPresent}|{student.WasQuestioned}|{student.QuestionedTurnsAgo}";
            lines.Add(line);
        }
        return lines;
    }

    private SchoolClass? ParseClassLines(string[] lines)
    {
        if (lines.Length < 2)
        {
            return null;
        }

        SchoolClass schoolClass = new SchoolClass
        {
            ClassName = lines[0]
        };

        if (int.TryParse(lines[1], out int luckyNumber))
        {
            schoolClass.LuckyNumber = luckyNumber;
        }

        List<Student> students = ParseStudentLines(lines.Skip(2).ToArray());
        schoolClass.Students.AddRange(students);

        return schoolClass;
    }

    private static List<Student> ParseStudentLines(string[] lines)
    {
        List<Student> students = new List<Student>();
        foreach (string line in lines)
        {
            if (TryParseStudentLine(line, out Student? student))
            {
                students.Add(student);
            }
        }
        return students;
    }

    private static bool TryParseStudentLine(string line, out Student? student)
    {
        student = null;
        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        string[] parts = line.Split('|');
        if (parts.Length < 5)
        {
            return false;
        }

        Student parsed = new Student();
        if (int.TryParse(parts[0], out int number))
        {
            parsed.Number = number;
        }

        parsed.Name = parts[1];

        if (bool.TryParse(parts[2], out bool isPresent))
        {
            parsed.IsPresent = isPresent;
        }

        if (bool.TryParse(parts[3], out bool wasQuestioned))
        {
            parsed.WasQuestioned = wasQuestioned;
        }

        if (int.TryParse(parts[4], out int turnsAgo))
        {
            parsed.QuestionedTurnsAgo = turnsAgo;
        }

        student = parsed;
        return true;
    }

    private string GetFilePath(string className)
    {
        string sanitizedName = string.Join("_", className.Split(Path.GetInvalidFileNameChars()));
        return Path.Combine(_dataDirectory, $"{sanitizedName}.txt");
    }
}
