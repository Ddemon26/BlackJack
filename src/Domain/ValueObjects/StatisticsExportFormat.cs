namespace GroupProject.Domain.ValueObjects;

/// <summary>
/// Represents the available formats for exporting player statistics.
/// </summary>
public enum StatisticsExportFormat
{
    /// <summary>
    /// JavaScript Object Notation format
    /// </summary>
    Json,
    
    /// <summary>
    /// Comma-Separated Values format
    /// </summary>
    Csv,
    
    /// <summary>
    /// Extensible Markup Language format
    /// </summary>
    Xml,
    
    /// <summary>
    /// Plain text format with formatted output
    /// </summary>
    Text
}