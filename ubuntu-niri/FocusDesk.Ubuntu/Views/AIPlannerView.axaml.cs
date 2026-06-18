using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using FocusDesk.ViewModels;
using System;
using System.IO;

namespace FocusDesk.Views;

public partial class AIPlannerView : UserControl
{
    public AIPlannerView()
    {
        InitializeComponent();
    }

    private async void UploadFiles_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not AIPlannerViewModel vm) return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Seleziona i materiali di studio",
            AllowMultiple = true,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("File di testo e PDF") { Patterns = new[] { "*.txt", "*.md", "*.csv", "*.json", "*.pdf" } },
                FilePickerFileTypes.All
            }
        });

        if (files.Count > 0)
        {
            var names = new System.Collections.Generic.List<string>();

            foreach (var file in files)
            {
                names.Add(file.Name);
                
                try
                {
                    await using var stream = await file.OpenReadAsync();
                    
                    if (file.Name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                    {
                        using var ms = new MemoryStream();
                        await stream.CopyToAsync(ms);
                        ms.Position = 0;
                        
                        using var pdfDocument = UglyToad.PdfPig.PdfDocument.Open(ms);
                        var textBuilder = new System.Text.StringBuilder();
                        foreach (var page in pdfDocument.GetPages())
                        {
                            textBuilder.AppendLine(page.Text);
                        }
                        vm.UploadedFilesContent.Add($"[File PDF: {file.Name}]\n{textBuilder.ToString()}");
                    }
                    else
                    {
                        using var reader = new StreamReader(stream);
                        var text = await reader.ReadToEndAsync();
                        vm.UploadedFilesContent.Add($"[File: {file.Name}]\n{text}");
                    }
                }
                catch (Exception ex)
                {
                    vm.UploadedFilesContent.Add($"[File: {file.Name}]\nErrore lettura: {ex.Message}");
                }
            }

            vm.UploadedFilesSummary = $"Caricati {vm.UploadedFilesContent.Count} file totali.";
        }
    }
    private async void UploadFolder_Click(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not AIPlannerViewModel vm) return;

        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel == null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Seleziona la cartella dei materiali",
            AllowMultiple = true
        });

        if (folders.Count > 0)
        {
            foreach (var folder in folders)
            {
                await ProcessFolderAsync(folder, vm);
            }
            vm.UploadedFilesSummary = $"Caricati {vm.UploadedFilesContent.Count} file totali.";
        }
    }

    private async System.Threading.Tasks.Task ProcessFolderAsync(Avalonia.Platform.Storage.IStorageFolder folder, AIPlannerViewModel vm)
    {
        try
        {
            await foreach (var item in folder.GetItemsAsync())
            {
                if (item is Avalonia.Platform.Storage.IStorageFile file)
                {
                    string ext = Path.GetExtension(file.Name).ToLowerInvariant();
                    if (ext == ".txt" || ext == ".md" || ext == ".csv" || ext == ".json" || ext == ".pdf")
                    {
                        await ProcessSingleFileAsync(file, vm);
                    }
                }
                else if (item is Avalonia.Platform.Storage.IStorageFolder subfolder)
                {
                    await ProcessFolderAsync(subfolder, vm);
                }
            }
        }
        catch (Exception ex)
        {
            vm.UploadedFilesContent.Add($"[Cartella: {folder.Name}]\nErrore lettura: {ex.Message}");
        }
    }

    private async System.Threading.Tasks.Task ProcessSingleFileAsync(Avalonia.Platform.Storage.IStorageFile file, AIPlannerViewModel vm)
    {
        try
        {
            await using var stream = await file.OpenReadAsync();
            
            if (file.Name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                using var ms = new MemoryStream();
                await stream.CopyToAsync(ms);
                ms.Position = 0;
                
                using var pdfDocument = UglyToad.PdfPig.PdfDocument.Open(ms);
                var textBuilder = new System.Text.StringBuilder();
                foreach (var page in pdfDocument.GetPages())
                {
                    textBuilder.AppendLine(page.Text);
                }
                vm.UploadedFilesContent.Add($"[File PDF: {file.Name}]\n{textBuilder.ToString()}");
            }
            else
            {
                using var reader = new StreamReader(stream);
                var text = await reader.ReadToEndAsync();
                vm.UploadedFilesContent.Add($"[File: {file.Name}]\n{text}");
            }
        }
        catch (Exception ex)
        {
            vm.UploadedFilesContent.Add($"[File: {file.Name}]\nErrore lettura: {ex.Message}");
        }
    }
}
