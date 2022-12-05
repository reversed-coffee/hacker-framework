using System;

using HackerFramework.Streams;

namespace HackerFramework.NativeObjects;

/// <summary>
///     Represents a runtime module in the target process.
/// </summary>
public class RtModule : IEquatable<RtModule> {
    /// <summary>
    ///     The base address of the module.
    /// </summary>
    public readonly uint Base;

    /// <summary>
    ///     The size of the module.
    /// </summary>
    public readonly int Size;

    internal RtModule(RtProc target, uint baseAddress, int size) {
        Target = target;
        Base = baseAddress;
        Size = size;
    }

    /// <summary>
    ///     The process that the module is loaded under.
    /// </summary>
    public RtProc Target { get; }

    /// <summary>
    ///     The portable executable file of the module.
    /// </summary>
    public Pe32 Pe {
        get {
            var openPrior = Target.IsOpen;
            Target.Open();

            using var ts = new TargetStream(Target, Base);
            var pe = Pe32.Open(ts);

            if (openPrior)
                Target.Close();

            return pe;
        }
    }

    /// <summary>
    ///     The path to the module.
    /// </summary>
    public string Path { get; init; }

    /// <summary>
    ///     The module's name.
    /// </summary>
    public string Name => Path != null ? System.IO.Path.GetFileName(Path) : null;

    public bool Equals(RtModule other) {
        return other != null && other.Base == Base && other.Target == Target;
    }
}