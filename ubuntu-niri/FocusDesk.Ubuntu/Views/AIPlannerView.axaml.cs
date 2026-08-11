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
            foreach (var file in files)
            {
                var fileItem = new UploadedMaterialItem { Name = file.Name, Path = file.Path?.ToString() ?? file.Name };
                await ProcessSingleFileAsync(file, fileItem);
                if (fileItem.Contents.Count > 0)
                {
                    vm.UploadedMaterials.Add(fileItem);
                }
            }

            vm.UpdateSummary();
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
                var folderItem = new UploadedMaterialItem { Name = "Cartella: " + folder.Name, Path = folder.Path?.ToString() ?? folder.Name };
                await ProcessFolderAsync(folder, folderItem);
                if (folderItem.Contents.Count > 0)
                {
                    vm.UploadedMaterials.Add(folderItem);
                }
            }
            vm.UpdateSummary();
        }
    }

    private async System.Threading.Tasks.Task ProcessFolderAsync(Avalonia.Platform.Storage.IStorageFolder folder, UploadedMaterialItem item)
    {
        try
        {
            await foreach (var child in folder.GetItemsAsync())
            {
                if (child is Avalonia.Platform.Storage.IStorageFile file)
                {
                    string ext = Path.GetExtension(file.Name).ToLowerInvariant();
                    if (ext == ".txt" || ext == ".md" || ext == ".csv" || ext == ".json" || ext == ".pdf")
                    {
                        await ProcessSingleFileAsync(file, item);
                    }
                }
                else if (child is Avalonia.Platform.Storage.IStorageFolder subfolder)
                {
                    await ProcessFolderAsync(subfolder, item);
                }
            }
        }
        catch (Exception ex)
        {
            item.Contents.Add($"[Cartella: {folder.Name}]\nErrore lettura: {ex.Message}");
        }
    }

    private async System.Threading.Tasks.Task ProcessSingleFileAsync(Avalonia.Platform.Storage.IStorageFile file, UploadedMaterialItem item)
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
                item.Contents.Add($"[File PDF: {file.Name}]\n{textBuilder.ToString()}");
            }
            else
            {
                using var reader = new StreamReader(stream);
                var text = await reader.ReadToEndAsync();
                item.Contents.Add($"[File: {file.Name}]\n{text}");
            }
        }
        catch (Exception ex)
        {
            item.Contents.Add($"[File: {file.Name}]\nErrore lettura: {ex.Message}");
        }
    }
}
