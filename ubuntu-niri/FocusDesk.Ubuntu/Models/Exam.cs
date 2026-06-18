using System;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FocusDesk.Models;

public partial class Exam : ObservableObject
{
    private int _id;
    public int Id { get => _id; set => SetProperty(ref _id, value); }

    private string _name = string.Empty;
    public string Name { get => _name; set => SetProperty(ref _name, value); }

    private DateTime? _writtenDate;
    public DateTime? WrittenDate { get => _writtenDate; set => SetProperty(ref _writtenDate, value); }

    private DateTime? _quizDate;
    public DateTime? QuizDate { get => _quizDate; set => SetProperty(ref _quizDate, value); }

    private DateTime? _oralDate;
    public DateTime? OralDate { get => _oralDate; set => SetProperty(ref _oralDate, value); }

    private DateTime? _practicalDate;
    public DateTime? PracticalDate { get => _practicalDate; set => SetProperty(ref _practicalDate, value); }
    
    private bool _hasWrittenExam;
    public bool HasWrittenExam { get => _hasWrittenExam; set => SetProperty(ref _hasWrittenExam, value); }

    private bool _hasQuiz;
    public bool HasQuiz { get => _hasQuiz; set => SetProperty(ref _hasQuiz, value); }

    private bool _hasOralExam;
    public bool HasOralExam { get => _hasOralExam; set => SetProperty(ref _hasOralExam, value); }

    private bool _hasPracticalExam;
    public bool HasPracticalExam { get => _hasPracticalExam; set => SetProperty(ref _hasPracticalExam, value); }

    private string _description = string.Empty;
    public string Description { get => _description; set => SetProperty(ref _description, value); }
}
