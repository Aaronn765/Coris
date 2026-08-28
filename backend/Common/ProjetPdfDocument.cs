using System.Globalization;
using System.Text;
using IncidentsDsi.Api.DTOs;

namespace IncidentsDsi.Api.Common;

/// <summary>
/// Builds a small, dependency-free PDF for a project detail sheet.
/// Text is encoded with WinAnsi so French characters remain readable in standard PDF viewers.
/// </summary>
public static class ProjetPdfDocument
{
    private const double PageWidth = 595;
    private const double PageHeight = 842;
    private const double LeftMargin = 48;
    private const double BottomMargin = 48;
    private const double ContentWidth = PageWidth - (LeftMargin * 2);

    public static byte[] Build(ProjetDto project)
    {
        var pages = new List<PdfPage>();
        var current = new PdfPage();

        void EnsureSpace(double height = 16)
        {
            if (current.Y - height < BottomMargin)
            {
                pages.Add(current);
                current = new PdfPage();
            }
        }

        static IReadOnlyList<string> Lines(string? value, double size, double width)
        {
            var maxCharacters = Math.Max(1, (int)(width / (size * 0.52)));
            return Display(value).Replace("\r", string.Empty)
                .Split('\n')
                .SelectMany(paragraph => Wrap(paragraph, maxCharacters))
                .ToList();
        }

        static double TextHeight(string? value, double size, double width, double spacing = 4)
            => Lines(value, size, width).Count * (size + spacing);

        void AddTextAt(string value, double x, double y, double width, double size = 10, bool bold = false, string color = "0.18 0.23 0.31")
        {
            foreach (var line in Lines(value, size, width))
            {
                current.Lines.Add(new PdfLine(line, x, y, size, bold, color));
                y -= size + 4;
            }
        }

        void AddSection(string title)
        {
            EnsureSpace(32);
            var top = current.Y;
            current.Rectangles.Add(new PdfRect(LeftMargin, top - 24, 5, 24, "0.07 0.25 0.57"));
            AddTextAt(title, LeftMargin + 14, top - 17, ContentWidth - 14, 13, true, "0.07 0.25 0.57");
            current.Y -= 34;
        }

        void AddField(string label, string? value, double width = ContentWidth)
        {
            var height = 12 + TextHeight(value, 10, width) + 8;
            EnsureSpace(height);
            var top = current.Y;
            AddTextAt(label.ToUpperInvariant(), LeftMargin, top, width, 8, true, "0.45 0.49 0.56");
            AddTextAt(Display(value), LeftMargin, top - 12, width, 10, false, "0.18 0.23 0.31");
            current.Y -= height;
        }

        void AddTwoFields(string labelOne, string? valueOne, string labelTwo, string? valueTwo)
        {
            const double gap = 20;
            var width = (ContentWidth - gap) / 2;
            var height = 12 + Math.Max(TextHeight(valueOne, 10, width), TextHeight(valueTwo, 10, width)) + 8;
            EnsureSpace(height);
            var top = current.Y;
            var secondX = LeftMargin + width + gap;
            AddTextAt(labelOne.ToUpperInvariant(), LeftMargin, top, width, 8, true, "0.45 0.49 0.56");
            AddTextAt(Display(valueOne), LeftMargin, top - 12, width, 10);
            AddTextAt(labelTwo.ToUpperInvariant(), secondX, top, width, 8, true, "0.45 0.49 0.56");
            AddTextAt(Display(valueTwo), secondX, top - 12, width, 10);
            current.Y -= height;
        }

        void AddStep(EtapeProjetDto step, int index)
        {
            var innerWidth = ContentWidth - 28;
            var titleHeight = TextHeight($"{index + 1}. {step.Nom}", 11, innerWidth, 4);
            var metaHeight = TextHeight($"Statut : {Display(step.StatutEtape?.Nom)}    |    Avancement : {Math.Round(step.TauxAvancement * 100)} %", 9, innerWidth, 3);
            var datesHeight = TextHeight($"Du {FormatDate(step.DateDebut)}    au    {FormatDate(step.DateFin)}", 9, innerWidth, 3);
            var fieldsHeight = new[] { step.Support, step.Contraintes, step.Commentaires }
                .Sum(value => 11 + TextHeight(value, 9.5, innerWidth) + 6);
            var cardHeight = 16 + titleHeight + 4 + metaHeight + datesHeight + 6 + fieldsHeight + 8;
            EnsureSpace(cardHeight + 8);

            var top = current.Y;
            current.Rectangles.Add(new PdfRect(LeftMargin, top - cardHeight, ContentWidth, cardHeight, "0.96 0.98 1"));
            var y = top - 12;
            AddTextAt($"{index + 1}. {step.Nom}", LeftMargin + 14, y, innerWidth, 11, true, "0.07 0.25 0.57");
            y -= titleHeight + 4;
            AddTextAt($"Statut : {Display(step.StatutEtape?.Nom)}    |    Avancement : {Math.Round(step.TauxAvancement * 100)} %", LeftMargin + 14, y, innerWidth, 9, true, "0.25 0.31 0.40");
            y -= metaHeight;
            AddTextAt($"Du {FormatDate(step.DateDebut)}    au    {FormatDate(step.DateFin)}", LeftMargin + 14, y, innerWidth, 9, false, "0.45 0.49 0.56");
            y -= datesHeight + 5;

            foreach (var (label, value) in new[]
            {
                ("Support", step.Support),
                ("Contraintes", step.Contraintes),
                ("Commentaires", step.Commentaires)
            })
            {
                AddTextAt(label.ToUpperInvariant(), LeftMargin + 14, y, innerWidth, 7.5, true, "0.45 0.49 0.56");
                y -= 10;
                AddTextAt(Display(value), LeftMargin + 14, y, innerWidth, 9.5);
                y -= TextHeight(value, 9.5, innerWidth) + 6;
            }

            current.Y = top - cardHeight - 8;
        }

        const double headerHeight = 116;
        var headerTop = PageHeight - 40;
        current.Rectangles.Add(new PdfRect(LeftMargin, headerTop - headerHeight, ContentWidth, headerHeight, "0.07 0.25 0.57"));
        AddTextAt("CORIS BANK", LeftMargin + 18, headerTop - 25, ContentWidth - 36, 10, true, "1 1 1");
        AddTextAt("FICHE PROJET", LeftMargin + 18, headerTop - 51, ContentWidth - 36, 12, true, "0.89 0.11 0.17");
        AddTextAt(project.Nom, LeftMargin + 18, headerTop - 79, ContentWidth - 36, 20, true, "1 1 1");
        AddTextAt($"{project.Numero}   |   {Display(project.DomaineProjet?.Nom)}   |   {Display(project.StatutProjet?.Nom)}", LeftMargin + 18, headerTop - 101, ContentWidth - 36, 9, false, "0.86 0.91 1");
        current.Y = headerTop - headerHeight - 26;

        AddSection("Synthese");
        AddTwoFields("Avancement global", $"{Math.Round(project.TauxAvancement * 100)} %", "Echeance", FormatDate(project.DateEcheance));
        AddTwoFields("Date de debut", FormatDate(project.DateDebut), "Date de fin", FormatDate(project.DateFin));
        AddField("Responsables", project.Responsables.Count == 0 ? null : string.Join(" - ", project.Responsables.Select(x => x.Nom)));

        AddSection("Informations detaillees");
        AddField("Description", project.Description);
        AddTwoFields("Support", project.Support, "Acteurs metiers", project.ActeursMetiers);
        AddTwoFields("Contraintes", project.Contraintes, "Commentaires", project.Commentaires);
        AddTwoFields("Cree le", FormatDateTime(project.CreatedAt), "Mis a jour le", FormatDateTime(project.UpdatedAt));

        AddSection("Etapes du projet");
        if (project.Etapes.Count == 0)
        {
            AddField("Etat", "Aucune etape enregistree.");
        }
        else
        {
            foreach (var (step, index) in project.Etapes.OrderBy(x => x.Ordre).Select((item, itemIndex) => (item, itemIndex)))
                AddStep(step, index);
        }

        pages.Add(current);
        return WritePdf(pages);
    }

    private static byte[] WritePdf(IReadOnlyList<PdfPage> pages)
    {
        var objects = new List<byte[]?> { null };
        var pagesReference = ReserveObject(objects);
        var regularFont = AddObject(objects, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>");
        var boldFont = AddObject(objects, "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>");
        var pageReferences = new List<int>();

        foreach (var page in pages)
        {
            var content = BuildContent(page, regularFont, boldFont, pageReferences.Count + 1, pages.Count);
            var contentBytes = Encoding.ASCII.GetBytes(content);
            var contentReference = AddObject(objects, $"<< /Length {contentBytes.Length} >>\nstream\n{content}\nendstream");
            var pageReference = AddObject(objects,
                $"<< /Type /Page /Parent {pagesReference} 0 R /MediaBox [0 0 {PageWidth:0} {PageHeight:0}] " +
                $"/Resources << /Font << /F1 {regularFont} 0 R /F2 {boldFont} 0 R >> >> /Contents {contentReference} 0 R >>");
            pageReferences.Add(pageReference);
        }

        objects[pagesReference] = Encoding.ASCII.GetBytes(
            $"<< /Type /Pages /Kids [{string.Join(" ", pageReferences.Select(x => $"{x} 0 R"))}] /Count {pageReferences.Count} >>");
        var catalogReference = AddObject(objects, $"<< /Type /Catalog /Pages {pagesReference} 0 R >>");

        using var output = new MemoryStream();
        WriteAscii(output, "%PDF-1.4\n");
        var offsets = new int[objects.Count];
        for (var index = 1; index < objects.Count; index++)
        {
            offsets[index] = checked((int)output.Position);
            WriteAscii(output, $"{index} 0 obj\n");
            output.Write(objects[index]!);
            WriteAscii(output, "\nendobj\n");
        }

        var xrefPosition = output.Position;
        WriteAscii(output, $"xref\n0 {objects.Count}\n0000000000 65535 f \n");
        for (var index = 1; index < objects.Count; index++)
            WriteAscii(output, $"{offsets[index]:D10} 00000 n \n");
        WriteAscii(output, $"trailer\n<< /Size {objects.Count} /Root {catalogReference} 0 R >>\nstartxref\n{xrefPosition}\n%%EOF\n");
        return output.ToArray();
    }

    private static string BuildContent(PdfPage page, int regularFont, int boldFont, int pageNumber, int totalPages)
    {
        var content = new StringBuilder("q\n");
        foreach (var rectangle in page.Rectangles)
        {
            content.Append(rectangle.Color).Append(" rg\n")
                .Append(rectangle.X.ToString("0.##", CultureInfo.InvariantCulture)).Append(' ')
                .Append(rectangle.Y.ToString("0.##", CultureInfo.InvariantCulture)).Append(' ')
                .Append(rectangle.Width.ToString("0.##", CultureInfo.InvariantCulture)).Append(' ')
                .Append(rectangle.Height.ToString("0.##", CultureInfo.InvariantCulture)).Append(" re f\n");
        }
        foreach (var line in page.Lines)
        {
            var font = line.Bold ? "F2" : "F1";
            content.Append(line.Color).Append(" rg\n");
            content.Append("BT /").Append(font).Append(' ').Append(line.Size.ToString("0.##", CultureInfo.InvariantCulture))
                .Append(" Tf ").Append(line.X.ToString("0.##", CultureInfo.InvariantCulture)).Append(' ')
                .Append(line.Y.ToString("0.##", CultureInfo.InvariantCulture)).Append(" Td <")
                .Append(Convert.ToHexString(ToWinAnsi(line.Text))).Append("> Tj ET\n");
        }
        content.Append("0.82 0.85 0.90 RG 0.5 w 48 34 499 0 m 547 34 l S\n");
        content.Append("0.45 0.49 0.56 rg\nBT /F1 8 Tf 48 22 Td <")
            .Append(Convert.ToHexString(ToWinAnsi("Fiche projet"))).Append("> Tj ET\n");
        content.Append("BT /F1 8 Tf 475 22 Td <")
            .Append(Convert.ToHexString(ToWinAnsi($"Page {pageNumber} / {totalPages}"))).Append("> Tj ET\n");
        return content.Append("Q").ToString();
    }

    private static int AddObject(List<byte[]?> objects, string content)
    {
        objects.Add(Encoding.ASCII.GetBytes(content));
        return objects.Count - 1;
    }

    private static int ReserveObject(List<byte[]?> objects)
    {
        objects.Add(null);
        return objects.Count - 1;
    }

    private static IEnumerable<string> Wrap(string value, int maxCharacters)
    {
        var text = string.IsNullOrWhiteSpace(value) ? "Non renseigne" : value.Trim();
        while (text.Length > maxCharacters)
        {
            var breakAt = text.LastIndexOf(' ', Math.Min(maxCharacters, text.Length - 1));
            if (breakAt <= 0) breakAt = maxCharacters;
            yield return text[..breakAt].TrimEnd();
            text = text[breakAt..].TrimStart();
        }
        yield return text;
    }

    private static byte[] ToWinAnsi(string value)
    {
        var bytes = new List<byte>(value.Length);
        foreach (var character in value.Replace("’", "'").Replace("–", "-").Replace("—", "-").Replace("…", "..."))
        {
            var mapped = character switch
            {
                '€' => 0x80, 'Œ' => 0x8C, 'œ' => 0x9C,
                'À' => 0xC0, 'Â' => 0xC2, 'Ç' => 0xC7, 'È' => 0xC8, 'É' => 0xC9,
                'Ê' => 0xCA, 'Ë' => 0xCB, 'Î' => 0xCE, 'Ï' => 0xCF, 'Ô' => 0xD4,
                'Ù' => 0xD9, 'Û' => 0xDB, 'à' => 0xE0, 'â' => 0xE2, 'ç' => 0xE7,
                'è' => 0xE8, 'é' => 0xE9, 'ê' => 0xEA, 'ë' => 0xEB, 'î' => 0xEE,
                'ï' => 0xEF, 'ô' => 0xF4, 'ù' => 0xF9, 'û' => 0xFB,
                >= '\x20' and <= '\x7E' => character,
                _ => '?'
            };
            bytes.Add((byte)mapped);
        }
        return bytes.ToArray();
    }

    private static string Display(string? value) => string.IsNullOrWhiteSpace(value) ? "Non renseigne" : value.Trim();

    private static string FormatDate(DateTime? value) => value.HasValue ? value.Value.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture) : "Non renseignee";

    private static string FormatDateTime(DateTime value) => value.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);

    private static void WriteAscii(Stream stream, string value) => stream.Write(Encoding.ASCII.GetBytes(value));

    private sealed class PdfPage
    {
        public double Y { get; set; } = PageHeight - 48;
        public List<PdfRect> Rectangles { get; } = [];
        public List<PdfLine> Lines { get; } = [];
    }

    private sealed record PdfRect(double X, double Y, double Width, double Height, string Color);
    private sealed record PdfLine(string Text, double X, double Y, double Size, bool Bold, string Color);
}
