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
    public DateTime? WrittenDate { get => _writtenDate; set { if (SetProperty(ref _writtenDate, value)) OnPropertyChanged(nameof(CountdownText)); } }

    private DateTime? _quizDate;
    public DateTime? QuizDate { get => _quizDate; set { if (SetProperty(ref _quizDate, value)) OnPropertyChanged(nameof(CountdownText)); } }

    private DateTime? _oralDate;
    public DateTime? OralDate { get => _oralDate; set { if (SetProperty(ref _oralDate, value)) OnPropertyChanged(nameof(CountdownText)); } }

    private DateTime? _practicalDate;
    public DateTime? PracticalDate { get => _practicalDate; set { if (SetProperty(ref _practicalDate, value)) OnPropertyChanged(nameof(CountdownText)); } }
    
    private bool _hasWrittenExam;
    public bool HasWrittenExam { get => _hasWrittenExam; set { if (SetProperty(ref _hasWrittenExam, value)) OnPropertyChanged(nameof(CountdownText)); } }

    private bool _hasQuiz;
    public bool HasQuiz { get => _hasQuiz; set { if (SetProperty(ref _hasQuiz, value)) OnPropertyChanged(nameof(CountdownText)); } }

    private bool _hasOralExam;
    public bool HasOralExam { get => _hasOralExam; set { if (SetProperty(ref _hasOralExam, value)) OnPropertyChanged(nameof(CountdownText)); } }

    private bool _hasPracticalExam;
    public bool HasPracticalExam { get => _hasPracticalExam; set { if (SetProperty(ref _hasPracticalExam, value)) OnPropertyChanged(nameof(CountdownText)); } }

    private string _description = string.Empty;
    public string Description { get => _description; set => SetProperty(ref _description, value); }

    public string CountdownText
    {
        get
        {
            var dates = new System.Collections.Generic.List<DateTime>();
            if (HasWrittenExam && WrittenDate.HasValue) dates.Add(WrittenDate.Value);
            if (HasQuiz && QuizDate.HasValue) dates.Add(QuizDate.Value);
            if (HasOralExam && OralDate.HasValue) dates.Add(OralDate.Value);
            if (HasPracticalExam && PracticalDate.HasValue) dates.Add(PracticalDate.Value);

            var futureDates = System.Linq.Enumerable.ToList(System.Linq.Enumerable.OrderBy(System.Linq.Enumerable.Where(dates, d => d.Date >= DateTime.Now.Date), d => d));
            
            if (futureDates.Count == 0) return "Nessuna data futura";

            var nextDate = futureDates[0];
            var daysLeft = (nextDate.Date - DateTime.Now.Date).Days;
            
            if (daysLeft == 0) return "Oggi!";
            if (daysLeft == 1) return "Domani!";
            return $"-{daysLeft} giorni";
        }
    }
}
