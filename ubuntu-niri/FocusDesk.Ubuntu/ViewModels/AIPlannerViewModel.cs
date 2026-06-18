using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FocusDesk.Models;
using FocusDesk.Data;
using FocusDesk.Services;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using MsBox.Avalonia;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;

namespace FocusDesk.ViewModels;

public partial class AIPlannerViewModel : ObservableObject
{
    private readonly MainViewModel? _mainVm;
    private readonly AIService _aiService;

    public ObservableCollection<Exam> Exams { get; } = new();
    
    // Lista dei testi estratti dai file (gestiti dalla View)
    public List<string> UploadedFilesContent { get; } = new();
    [ObservableProperty] private string _uploadedFilesSummary = "Nessun file selezionato";

    [ObservableProperty] private string _additionalNotes = string.Empty;
    [ObservableProperty] private bool _isGenerating = false;

    public ObservableCollection<StudySession> GeneratedSessions { get; } = new();

    public AIPlannerViewModel() { 
        _aiService = new AIService();
    } // Designer support
    
    public AIPlannerViewModel(MainViewModel mainVm)
    {
        _mainVm = mainVm;
        _aiService = new AIService();
        _ = LoadExamsAsync();
    }

    private async Task LoadExamsAsync()
    {
        try
        {
            using var db = new AppDbContext();
            var dbExams = await db.Exams.ToListAsync();
            Exams.Clear();
            foreach(var e in dbExams) Exams.Add(e);
        }
        catch { }
    }

    [RelayCommand]
    private async Task AddExam()
    {
        var newExam = new Exam { Name = "Nuova Materia" };
        using var db = new AppDbContext();
        db.Exams.Add(newExam);
        await db.SaveChangesAsync();
        Exams.Add(newExam);
    }

    [RelayCommand]
    private async Task SaveExam(Exam exam)
    {
        if (exam == null) return;
        using var db = new AppDbContext();
        db.Exams.Update(exam);
        await db.SaveChangesAsync();
    }

    [RelayCommand]
    private async Task DeleteExam(Exam exam)
    {
        if (exam == null) return;
        using var db = new AppDbContext();
        db.Exams.Remove(exam);
        await db.SaveChangesAsync();
        Exams.Remove(exam);
    }

    [RelayCommand]
    private async Task GeneratePlan()
    {
        if (_mainVm == null) return;

        if (!Exams.Any())
        {
            await MessageBoxManager.GetMessageBoxStandard("FocusDesk", "Aggiungi almeno un esame per il quale preparare il piano di studio.", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Warning).ShowAsync();
            return;
        }

        var apiKey = _mainVm.Settings.GeminiApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            await MessageBoxManager.GetMessageBoxStandard("FocusDesk", "Devi inserire la tua Google Gemini API Key nelle Impostazioni (scheda Servizi AI) prima di procedere.", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Warning).ShowAsync();
            return;
        }

        IsGenerating = true;
        GeneratedSessions.Clear();

        try
        {
            // Salva gli esami per sicurezza prima di procedere
            using (var db = new AppDbContext())
            {
                foreach(var e in Exams) db.Exams.Update(e);
                await db.SaveChangesAsync();
            }

            var sessions = await _aiService.GenerateStudyPlanAsync(apiKey, Exams.ToList(), UploadedFilesContent, AdditionalNotes);
            
            // Ordina per data
            foreach(var s in sessions.OrderBy(x => x.Date).ThenBy(x => x.TimeOfDay))
            {
                GeneratedSessions.Add(s);
            }
        }
        catch (Exception ex)
        {
            await MessageBoxManager.GetMessageBoxStandard("FocusDesk", $"Errore durante la generazione:\n{ex.Message}", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).ShowAsync();
        }
        finally
        {
            IsGenerating = false;
        }
    }

    [RelayCommand]
    private void RemoveGeneratedSession(StudySession session)
    {
        GeneratedSessions.Remove(session);
    }

    [RelayCommand]
    private async Task ImportPlanToAgenda()
    {
        if (!GeneratedSessions.Any()) return;

        try
        {
            using var db = new AppDbContext();
            foreach (var session in GeneratedSessions)
            {
                db.StudySessions.Add(session);
            }
            await db.SaveChangesAsync();
            GeneratedSessions.Clear();

            await MessageBoxManager.GetMessageBoxStandard("FocusDesk", "Piano importato con successo! Ora le sessioni sono visibili nell'Agenda.", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Success).ShowAsync();
        }
        catch (Exception ex)
        {
            await MessageBoxManager.GetMessageBoxStandard("FocusDesk", $"Errore durante l'importazione:\n{ex.Message}", MsBox.Avalonia.Enums.ButtonEnum.Ok, MsBox.Avalonia.Enums.Icon.Error).ShowAsync();
        }
    }
}
