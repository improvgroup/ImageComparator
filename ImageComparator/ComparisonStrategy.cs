namespace ImageComparator;

/// <summary>Supported image comparison strategies.</summary>
public enum ComparisonStrategy
{
    /// <summary>Legacy dominant-channel algorithm.</summary>
    LegacyDominantChannel,

    /// <summary>Mean absolute per-channel pixel difference on normalized images.</summary>
    MeanAbsoluteDifference,

    /// <summary>Difference hash (dHash) based perceptual comparison.</summary>
    DifferenceHash,

    /// <summary>Automatically choose a strategy based on image characteristics.</summary>
    Auto,
}

