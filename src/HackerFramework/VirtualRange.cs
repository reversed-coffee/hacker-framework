using HackerFramework.NativeObjects;

namespace HackerFramework;

/// <summary>
///     Represents a range in virtual memory.
/// </summary>
public readonly struct VirtualRange {
    /// <summary>
    ///     The minimum (starting) value.
    /// </summary>
    public readonly uint Min;

    /// <summary>
    ///     The maximum (ending) value.
    /// </summary>
    public readonly uint Max;

    /// <summary>
    ///     Instantiate a virtual range using a minimum and maximum value.
    /// </summary>
    public VirtualRange(uint min, uint? max = null) {
        Min = min;
        Max = max ?? int.MaxValue; // 0x7FFFFFFF is the end of the virtual address space
    }

    /// <summary>
    ///     Instantiate a virtual range using a <see cref="RtModule" />'s address space.
    /// </summary>
    public VirtualRange(RtModule module) {
        Min = module.Base;
        Max = module.Base + (uint)module.Size;
    }

    /// <summary>
    ///     Instantiate a virtual range using a <see cref="SectionHeader" />'s address space.
    /// </summary>
    public VirtualRange(SectionHeader header) {
        Min = header.VirtualAddress;
        Max = header.VirtualAddress + header.VirtualSize;
    }
}