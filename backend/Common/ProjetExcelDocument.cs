using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using IncidentsDsi.Api.DTOs;

namespace IncidentsDsi.Api.Common;

public static class ProjetExcelDocument
{
    private static readonly XLColor CorisBlue = XLColor.FromHtml("#123F91");
    private static readonly XLColor CorisBlueSoft = XLColor.FromHtml("#EDF3FF");
    private static readonly XLColor Slate700 = XLColor.FromHtml("#334155");
    private static readonly XLColor Slate500 = XLColor.FromHtml("#64748B");
    private static readonly XLColor Slate400 = XLColor.FromHtml("#94A3B8");
    private static readonly XLColor RowAlt = XLColor.FromHtml("#F8FAFC");
    private static readonly XLColor BorderLight = XLColor.FromHtml("#E2E8F0");
    private static readonly XLColor BorderHeader = XLColor.FromHtml("#CBD5E1");
    private static readonly XLColor GroupBg = XLColor.FromHtml("#E8EEF7");

    private static readonly string[] ProjectHeaders =
    [
        "N° projet", "Nom du projet", "Domaine", "Statut", "Avancement",
        "Date début", "Date fin", "Date échéance",
        "Responsables", "Acteurs métiers",
        "Description", "Support", "Contraintes", "Commentaires",
        "Créé le", "Mis à jour le"
    ];

    private static readonly double[] ProjectColumnWidths =
    [
        14, 36, 26, 14, 12, 13, 13, 14, 28, 26, 42, 24, 32, 36, 18, 18
    ];

    private static readonly string[] StepHeaders =
    [
        "N° projet", "Nom du projet", "Domaine", "N° étape", "Nom de l'étape",
        "Statut", "Avancement", "Date début", "Date fin",
        "Support", "Contraintes", "Commentaires"
    ];

    private static readonly double[] StepColumnWidths =
    [
        14, 34, 24, 10, 36, 14, 12, 13, 13, 24, 30, 34
    ];

    public static byte[] Build(IReadOnlyList<ProjetDto> projects)
    {
        var orderedProjects = projects
            .OrderBy(project => project.DomaineProjet.Nom, StringComparer.OrdinalIgnoreCase)
            .ThenBy(project => StatusOrder(project.StatutProjet.Nom))
            .ThenBy(project => project.DateEcheance ?? DateTime.MaxValue)
            .ThenBy(project => project.Nom, StringComparer.OrdinalIgnoreCase)
            .ToList();

        using var workbook = new XLWorkbook();
        BuildProjectsSheet(workbook, orderedProjects);
        BuildStepsSheet(workbook, orderedProjects);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void BuildProjectsSheet(XLWorkbook workbook, IReadOnlyList<ProjetDto> projects)
    {
        const int headerRow = 4;
        const int firstDataRow = 5;

        var worksheet = workbook.Worksheets.Add("Projets");
        ApplyBaseStyle(worksheet);
        WriteTitleSection(worksheet, ProjectHeaders.Length, "Suivi des projets DSI", $"{projects.Count} projet{(projects.Count > 1 ? "s" : "")}");
        WriteGroupHeaders(worksheet, headerRow - 1, new (int Start, int End, string Label)[]
        {
            (1, 4, "Identification"),
            (5, 5, "Avancement"),
            (6, 8, "Planning"),
            (9, 10, "Équipe"),
            (11, 14, "Détails"),
            (15, 16, "Suivi")
        });
        WriteHeaders(worksheet, headerRow, ProjectHeaders);

        for (var index = 0; index < projects.Count; index++)
            WriteProjectRow(worksheet, projects[index], firstDataRow + index, index % 2 == 1);

        ApplySheetLayout(worksheet, headerRow, firstDataRow, projects.Count, ProjectColumnWidths);
    }

    private static void BuildStepsSheet(XLWorkbook workbook, IReadOnlyList<ProjetDto> projects)
    {
        const int headerRow = 4;
        const int firstDataRow = 5;

        var steps = projects
            .SelectMany(project => project.Etapes
                .OrderBy(step => step.Ordre)
                .ThenBy(step => step.Id)
                .Select(step => (Project: project, Step: step)))
            .ToList();

        var worksheet = workbook.Worksheets.Add("Étapes");
        ApplyBaseStyle(worksheet);
        WriteTitleSection(worksheet, StepHeaders.Length, "Détail des étapes", $"{steps.Count} étape{(steps.Count > 1 ? "s" : "")}");
        WriteGroupHeaders(worksheet, headerRow - 1, new (int Start, int End, string Label)[]
        {
            (1, 3, "Projet"),
            (4, 7, "Étape"),
            (8, 9, "Planning"),
            (10, 12, "Détails")
        });
        WriteHeaders(worksheet, headerRow, StepHeaders);

        for (var index = 0; index < steps.Count; index++)
            WriteStepRow(worksheet, steps[index].Project, steps[index].Step, index + 1, firstDataRow + index, index % 2 == 1);

        ApplySheetLayout(worksheet, headerRow, firstDataRow, steps.Count, StepColumnWidths);
    }

    private static void ApplyBaseStyle(IXLWorksheet worksheet)
    {
        worksheet.Style.Font.FontName = "Calibri";
        worksheet.Style.Font.FontSize = 10;
        worksheet.Style.Font.FontColor = Slate700;
    }

    private static void WriteTitleSection(IXLWorksheet worksheet, int columnCount, string title, string countLabel)
    {
        var titleRange = worksheet.Range(1, 1, 1, columnCount).Merge();
        titleRange.Value = title;
        titleRange.Style.Font.Bold = true;
        titleRange.Style.Font.FontSize = 16;
        titleRange.Style.Font.FontColor = CorisBlue;
        titleRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        worksheet.Row(1).Height = 28;

        var subtitle = worksheet.Range(2, 1, 2, columnCount).Merge();
        subtitle.Value = $"Exporté le {DateTime.Now:dd/MM/yyyy à HH:mm}  ·  {countLabel}";
        subtitle.Style.Font.FontSize = 10;
        subtitle.Style.Font.FontColor = Slate500;
        subtitle.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        worksheet.Row(2).Height = 20;
        worksheet.Row(3).Height = 6;
    }

    private static void WriteGroupHeaders(IXLWorksheet worksheet, int row, IReadOnlyList<(int Start, int End, string Label)> groups)
    {
        foreach (var group in groups)
        {
            var range = worksheet.Range(row, group.Start, row, group.End).Merge();
            range.Value = group.Label;
            range.Style.Font.Bold = true;
            range.Style.Font.FontSize = 9;
            range.Style.Font.FontColor = CorisBlue;
            range.Style.Fill.BackgroundColor = GroupBg;
            range.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            range.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            range.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            range.Style.Border.OutsideBorderColor = BorderLight;
        }

        worksheet.Row(row).Height = 22;
    }

    private static void WriteHeaders(IXLWorksheet worksheet, int row, IReadOnlyList<string> headers)
    {
        for (var column = 0; column < headers.Count; column++)
        {
            var cell = worksheet.Cell(row, column + 1);
            cell.Value = headers[column];
            cell.Style.Font.Bold = true;
            cell.Style.Font.FontColor = XLColor.White;
            cell.Style.Fill.BackgroundColor = CorisBlue;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            cell.Style.Alignment.WrapText = true;
            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            cell.Style.Border.OutsideBorderColor = CorisBlue;
        }

        worksheet.Row(row).Height = 30;
    }

    private static void WriteProjectRow(IXLWorksheet worksheet, ProjetDto project, int row, bool alternate)
    {
        worksheet.Cell(row, 1).Value = project.Numero;
        worksheet.Cell(row, 2).Value = project.Nom;
        worksheet.Cell(row, 3).Value = project.DomaineProjet.Nom;
        worksheet.Cell(row, 4).Value = project.StatutProjet.Nom;
        worksheet.Cell(row, 9).Value = string.Join(Environment.NewLine, project.Responsables.Select(x => x.Nom));
        worksheet.Cell(row, 10).Value = project.ActeursMetiers ?? string.Empty;
        worksheet.Cell(row, 11).Value = project.Description ?? string.Empty;
        worksheet.Cell(row, 12).Value = project.Support ?? string.Empty;
        worksheet.Cell(row, 13).Value = project.Contraintes ?? string.Empty;
        worksheet.Cell(row, 14).Value = project.Commentaires ?? string.Empty;

        var progressCell = worksheet.Cell(row, 5);
        progressCell.Value = project.TauxAvancement;
        progressCell.Style.NumberFormat.Format = "0%";
        ApplyProgressStyle(progressCell, project.TauxAvancement);

        SetDateCell(worksheet.Cell(row, 6), project.DateDebut);
        SetDateCell(worksheet.Cell(row, 7), project.DateFin);
        SetDateCell(worksheet.Cell(row, 8), project.DateEcheance);
        SetDateTimeCell(worksheet.Cell(row, 15), project.CreatedAt);
        SetDateTimeCell(worksheet.Cell(row, 16), project.UpdatedAt);

        worksheet.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        worksheet.Cell(row, 2).Style.Font.Bold = true;
        worksheet.Cell(row, 3).Style.Font.FontColor = Slate500;
        ApplyStatusStyle(worksheet.Cell(row, 4), project.StatutProjet.Nom);

        StyleDataRow(worksheet, row, ProjectHeaders.Length, alternate, wrapColumns: [9, 10, 11, 12, 13, 14], centerColumns: [1, 5, 6, 7, 8, 15, 16]);
        worksheet.Row(row).Height = Math.Min(90, Math.Max(28, EstimateTextHeight(
            project.Description, project.Support, project.ActeursMetiers, project.Contraintes, project.Commentaires,
            string.Join(" ", project.Responsables.Select(x => x.Nom)))));
    }

    private static void WriteStepRow(
        IXLWorksheet worksheet,
        ProjetDto project,
        EtapeProjetDto step,
        int stepNumber,
        int row,
        bool alternate)
    {
        worksheet.Cell(row, 1).Value = project.Numero;
        worksheet.Cell(row, 2).Value = project.Nom;
        worksheet.Cell(row, 3).Value = project.DomaineProjet.Nom;
        worksheet.Cell(row, 4).Value = stepNumber;
        worksheet.Cell(row, 5).Value = step.Nom;
        worksheet.Cell(row, 6).Value = step.StatutEtape.Nom;
        worksheet.Cell(row, 10).Value = step.Support ?? string.Empty;
        worksheet.Cell(row, 11).Value = step.Contraintes ?? string.Empty;
        worksheet.Cell(row, 12).Value = step.Commentaires ?? string.Empty;

        var progressCell = worksheet.Cell(row, 7);
        progressCell.Value = step.TauxAvancement;
        progressCell.Style.NumberFormat.Format = "0%";
        ApplyProgressStyle(progressCell, step.TauxAvancement);

        SetDateCell(worksheet.Cell(row, 8), step.DateDebut);
        SetDateCell(worksheet.Cell(row, 9), step.DateFin);

        worksheet.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        worksheet.Cell(row, 2).Style.Font.Bold = true;
        worksheet.Cell(row, 3).Style.Font.FontColor = Slate500;
        worksheet.Cell(row, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        ApplyStatusStyle(worksheet.Cell(row, 6), step.StatutEtape.Nom);

        StyleDataRow(worksheet, row, StepHeaders.Length, alternate, wrapColumns: [5, 10, 11, 12], centerColumns: [1, 4, 7, 8, 9]);
        worksheet.Row(row).Height = Math.Min(72, Math.Max(24, EstimateTextHeight(step.Nom, step.Support, step.Contraintes, step.Commentaires)));
    }

    private static void StyleDataRow(
        IXLWorksheet worksheet,
        int row,
        int columnCount,
        bool alternate,
        IEnumerable<int> wrapColumns,
        IEnumerable<int> centerColumns)
    {
        var dataRange = worksheet.Range(row, 1, row, columnCount);
        dataRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
        dataRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
        dataRange.Style.Border.BottomBorderColor = BorderLight;

        if (alternate)
            dataRange.Style.Fill.BackgroundColor = RowAlt;

        foreach (var column in wrapColumns)
        {
            worksheet.Cell(row, column).Style.Alignment.WrapText = true;
            worksheet.Cell(row, column).Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
        }

        foreach (var column in centerColumns)
            worksheet.Cell(row, column).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
    }

    private static void ApplySheetLayout(
        IXLWorksheet worksheet,
        int headerRow,
        int firstDataRow,
        int rowCount,
        IReadOnlyList<double> columnWidths)
    {
        var lastRow = rowCount == 0 ? headerRow : firstDataRow + rowCount - 1;
        var tableRange = worksheet.Range(headerRow, 1, lastRow, columnWidths.Count);

        tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        tableRange.Style.Border.InsideBorderColor = BorderLight;
        tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
        tableRange.Style.Border.OutsideBorderColor = BorderHeader;

        worksheet.SheetView.FreezeRows(headerRow);
        worksheet.Range(headerRow, 1, lastRow, columnWidths.Count).SetAutoFilter();

        for (var column = 0; column < columnWidths.Count; column++)
            worksheet.Column(column + 1).Width = columnWidths[column];

        worksheet.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        worksheet.PageSetup.FitToPages(1, 0);
        worksheet.PageSetup.Margins.Top = 0.5;
        worksheet.PageSetup.Margins.Bottom = 0.5;
        worksheet.PageSetup.Margins.Left = 0.4;
        worksheet.PageSetup.Margins.Right = 0.4;
        worksheet.PageSetup.PrintAreas.Add(1, 1, lastRow, columnWidths.Count);
        worksheet.SheetView.ZoomScale = 90;
    }

    private static int StatusOrder(string statusName)
    {
        var normalized = RemoveDiacritics(statusName).ToLowerInvariant();
        if (normalized.Contains("en cours")) return 0;
        if (normalized.Contains("planif")) return 1;
        if (normalized.Contains("suspend")) return 2;
        if (normalized.Contains("clotur")) return 3;
        return 4;
    }

    private static double EstimateTextHeight(params string?[] values)
    {
        var lineCount = values.Max(value => string.IsNullOrWhiteSpace(value) ? 1 : value.Split('\n').Length);
        return 18 + (lineCount * 14);
    }

    private static void SetDateCell(IXLCell cell, DateTime? value)
    {
        if (!value.HasValue)
            return;

        cell.Value = value.Value;
        cell.Style.NumberFormat.Format = "dd/mm/yyyy";
    }

    private static void SetDateTimeCell(IXLCell cell, DateTime value)
    {
        cell.Value = value;
        cell.Style.NumberFormat.Format = "dd/mm/yyyy hh:mm";
    }

    private static void ApplyStatusStyle(IXLCell cell, string statusName)
    {
        var normalized = RemoveDiacritics(statusName).ToLowerInvariant();
        cell.Style.Font.Bold = true;
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        cell.Style.Alignment.WrapText = true;

        if (normalized.Contains("clotur"))
        {
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#EAFAF3");
            cell.Style.Font.FontColor = XLColor.FromHtml("#16815A");
            return;
        }

        if (normalized.Contains("suspend"))
        {
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF5F5");
            cell.Style.Font.FontColor = XLColor.FromHtml("#B81828");
            return;
        }

        if (normalized.Contains("en cours"))
        {
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#FFF8DF");
            cell.Style.Font.FontColor = XLColor.FromHtml("#A86D05");
            return;
        }

        cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#EDF5FF");
        cell.Style.Font.FontColor = XLColor.FromHtml("#2766A9");
    }

    private static void ApplyProgressStyle(IXLCell cell, decimal rate)
    {
        cell.Style.Font.Bold = true;
        cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

        if (rate >= 1m)
        {
            cell.Style.Font.FontColor = XLColor.FromHtml("#16815A");
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#EAFAF3");
            return;
        }

        if (rate > 0m)
        {
            cell.Style.Font.FontColor = CorisBlue;
            cell.Style.Fill.BackgroundColor = CorisBlueSoft;
            return;
        }

        cell.Style.Font.FontColor = Slate400;
    }

    private static string RemoveDiacritics(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        foreach (var character in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                builder.Append(character);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
