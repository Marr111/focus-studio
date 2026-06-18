using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using FocusDesk.Models;

namespace FocusDesk.Services;

public class AIService
{
    private static readonly HttpClient _httpClient = new HttpClient();

    public async Task<List<StudySession>> GenerateStudyPlanAsync(string apiKey, List<Exam> exams, List<string> materialsContent, string additionalNotes)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new Exception("API Key non configurata nelle impostazioni.");

        string url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-flash-latest:generateContent?key={apiKey}";

        // Costruisci il prompt
        var promptBuilder = new StringBuilder();
        promptBuilder.AppendLine("Sei un tutor universitario esperto in pianificazione dello studio.");
        promptBuilder.AppendLine("Devi generare un piano di studio strutturato basandoti sugli esami, sulle loro date composte (scritto, quiz, orale, pratica) e sui materiali forniti.");
        promptBuilder.AppendLine("\n**Esami:**");
        foreach (var exam in exams)
        {
            promptBuilder.AppendLine($"- {exam.Name}:");
            
            if (exam.HasWrittenExam) promptBuilder.AppendLine($"  - Prova Scritta: {(exam.WrittenDate.HasValue ? exam.WrittenDate.Value.ToString("yyyy-MM-dd") : "Data da definire")}");
            if (exam.HasQuiz) promptBuilder.AppendLine($"  - Quiz: {(exam.QuizDate.HasValue ? exam.QuizDate.Value.ToString("yyyy-MM-dd") : "Data da definire")}");
            if (exam.HasOralExam) promptBuilder.AppendLine($"  - Prova Orale: {(exam.OralDate.HasValue ? exam.OralDate.Value.ToString("yyyy-MM-dd") : "Data da definire")}");
            if (exam.HasPracticalExam) promptBuilder.AppendLine($"  - Prova Pratica: {(exam.PracticalDate.HasValue ? exam.PracticalDate.Value.ToString("yyyy-MM-dd") : "Data da definire")}");
            
            if (!string.IsNullOrWhiteSpace(exam.Description)) promptBuilder.AppendLine($"  - Note/Programma: {exam.Description}");
        }

        if (materialsContent != null && materialsContent.Count > 0)
        {
            promptBuilder.AppendLine("\n**Materiali forniti:**");
            foreach (var mat in materialsContent)
            {
                promptBuilder.AppendLine(mat);
                promptBuilder.AppendLine("---");
            }
        }

        if (!string.IsNullOrWhiteSpace(additionalNotes))
        {
            promptBuilder.AppendLine("\n**Note Aggiuntive dello studente (preferenze di studio):**");
            promptBuilder.AppendLine(additionalNotes);
        }

        promptBuilder.AppendLine($"\nOggi è il {DateTime.Now:yyyy-MM-dd}.");
        promptBuilder.AppendLine("Non pianificare sessioni in giorni passati.");
        promptBuilder.AppendLine("Genera un JSON array di sessioni di studio. Ogni sessione deve rispettare il seguente schema esatto:");
        promptBuilder.AppendLine("[");
        promptBuilder.AppendLine("  {");
        promptBuilder.AppendLine("    \"Date\": \"YYYY-MM-DDT00:00:00\",");
        promptBuilder.AppendLine("    \"Subject\": \"Nome Materia\",");
        promptBuilder.AppendLine("    \"TimeOfDay\": 0, // 0 = Mattina, 1 = Pomeriggio, 2 = Sera");
        promptBuilder.AppendLine("    \"Title\": \"Titolo argomento\",");
        promptBuilder.AppendLine("    \"Description\": \"Note su cosa studiare in dettaglio\",");
        promptBuilder.AppendLine("    \"DurationHours\": 2.0 // Durata in ore");
        promptBuilder.AppendLine("  }");
        promptBuilder.AppendLine("]");

        var requestBody = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = promptBuilder.ToString() } } }
            },
            generationConfig = new
            {
                response_mime_type = "application/json"
            }
        };

        string jsonBody = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(url, content);
        if (!response.IsSuccessStatusCode)
        {
            string error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Errore API Gemini: {response.StatusCode} - {error}");
        }

        string responseContent = await response.Content.ReadAsStringAsync();
        
        using var document = JsonDocument.Parse(responseContent);
        var root = document.RootElement;
        
        // Estrai il testo JSON dalla risposta di Gemini
        var textResponse = root.GetProperty("candidates")[0]
                               .GetProperty("content")
                               .GetProperty("parts")[0]
                               .GetProperty("text").GetString();

        if (string.IsNullOrWhiteSpace(textResponse))
            throw new Exception("Risposta vuota dall'IA.");

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var sessions = JsonSerializer.Deserialize<List<StudySession>>(textResponse, options);

        if (sessions == null)
            throw new Exception("Impossibile interpretare il piano generato.");

        foreach (var session in sessions)
        {
            if (session.Date < DateTime.Now.Date)
                session.Date = DateTime.Now.Date; // Correzione per eventuali errori IA
        }

        return sessions;
    }
}
