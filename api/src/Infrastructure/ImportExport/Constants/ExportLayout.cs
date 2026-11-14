namespace Infrastructure.ImportExport.Constants;

/// <summary>Sizes used by the XLSX and PDF exporters.</summary>
public static class ExportLayout
{
    public const string CultureName = "uk-UA";

    public const int FirstRow = 1;
    public const int FirstColumn = 1;
    public const int SectionGapRows = 2;
    public const double AisleColumnWidth = 2;
    public const double DeskColumnWidth = 18;
    public const double RowLabelColumnWidth = 8;

    public const float PdfMargin = 20;
    public const float PdfFontSize = 9;
    public const float PdfTitleFontSize = 14;
    public const float PdfRowLabelWidth = 40;
    public const float PdfAisleWidth = 8;
    public const float PdfNumberWidth = 30;
    public const float PdfSeatColumnWidth = 45;
    public const float PdfSignatureWidth = 120;
    public const float PdfBorder = 0.5f;
    public const float PdfCellPadding = 2;
    public const float PdfSpacing = 8;
}