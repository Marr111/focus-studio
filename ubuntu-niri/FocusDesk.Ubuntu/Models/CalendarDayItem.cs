using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FocusDesk.Models;

public partial class CalendarDayItem : ObservableObject
{
    [ObservableProperty] private DateTime _date;
    [ObservableProperty] private bool _isCurrentMonth;
    [ObservableProperty] private bool _isToday;
    
    public ObservableCollection<StudySession> Sessions { get; } = new();

    public CalendarDayItem(DateTime date, bool isCurrentMonth)
    {
        Date = date;
        IsCurrentMonth = isCurrentMonth;
        IsToday = date.Date == DateTime.Today;
    }
}
