namespace HackerFramework.ToolHelpInterface;

/// <summary>
///     Distinguishes between different modules.
/// </summary>
public readonly struct ModuleFilter {
    /// <summary>
    ///     The process' ID that the module is loaded under.
    /// </summary>
    public int ProcId { get; init; }

    /// <summary>
    ///     The module's name (e.g.: "kernel32.dll").
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    ///     The path to the module (e.g.: "C:\Windows\System32\kernel32.dll").
    /// </summary>
    public string Path { get; init; }
}