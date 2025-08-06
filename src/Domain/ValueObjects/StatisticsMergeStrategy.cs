namespace GroupProject.Domain.ValueObjects;

/// <summary>
/// Represents the strategies for merging imported statistics with existing data.
/// </summary>
public enum StatisticsMergeStrategy
{
    /// <summary>
    /// Overwrite existing statistics with imported data
    /// </summary>
    Overwrite,
    
    /// <summary>
    /// Skip importing statistics for players that already exist
    /// </summary>
    Skip,
    
    /// <summary>
    /// Merge imported statistics with existing data by adding values
    /// </summary>
    Merge,
    
    /// <summary>
    /// Create new player entries with modified names if conflicts exist
    /// </summary>
    CreateNew
}