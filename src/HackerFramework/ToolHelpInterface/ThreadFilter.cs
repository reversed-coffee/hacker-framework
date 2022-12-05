namespace HackerFramework.ToolHelpInterface;

/// <summary>
///     Distinguishes between different threads.
/// </summary>
public readonly struct ThreadFilter {
    /// <summary>
    ///     The process ID of the thread's owner.
    /// </summary>
    public int? ProcId { get; init; }
}