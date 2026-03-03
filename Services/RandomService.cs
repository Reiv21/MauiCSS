using MauiCSS.Models;

namespace MauiCSS.Services;

public class RandomService
{
    private readonly Random _random;

    public RandomService()
    {
        _random = new Random();
    }
    
    public Student? SelectRandomStudent(SchoolClass schoolClass, int globalLuckyNumber = 0)
    {
        List<Student> availableStudents = GetAvailableStudents(schoolClass, globalLuckyNumber);
        
        if (availableStudents.Count == 0)
        {
            return null;
        }

        int index = _random.Next(availableStudents.Count);
        return availableStudents[index];
    }
    
    public List<Student> GetAvailableStudents(SchoolClass schoolClass, int globalLuckyNumber = 0)
    {
        return schoolClass.Students
            .Where(s => s.IsPresent && s.Number != globalLuckyNumber && !s.WasQuestioned)
            .ToList();
    }
    
    public void MarkStudentAsQuestioned(SchoolClass schoolClass, Student selectedStudent)
    { 
        // update turn counters for all questioned students
        foreach (Student student in schoolClass.Students)
        {
            if (student.WasQuestioned)
            {
                student.QuestionedTurnsAgo++;
                
                // after 3 turns student return to pool
                if (student.QuestionedTurnsAgo >= 3)
                {
                    student.WasQuestioned = false;
                    student.QuestionedTurnsAgo = 0;
                }
            }
        }

        //mark selected student as questioned
        Student? studentToMark = schoolClass.Students.Find(s => s.Number == selectedStudent.Number);
        if (studentToMark != null)
        {
            studentToMark.WasQuestioned = true;
            studentToMark.QuestionedTurnsAgo = 0;
        }
    }
    
    public int GenerateLuckyNumber(int maxNumber)
    {
        return _random.Next(1, maxNumber + 1);
    }
}
