using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

using HackerFramework.NativeObjects;

using static HackerFramework.Native;

namespace HackerFramework;

/// <summary>
///     Represents a pattern that's used to compare against raw data.
///     Used by the <see cref="Scanner" /> class.
/// </summary>
public interface IPattern {
    /// <summary>
    ///     The size of the pattern.
    /// </summary>
    public int Size { get; init; }

    /// <summary>
    ///     Compares the given byte array to the pattern.
    /// </summary>
    public bool Compare(uint addr, ref byte[] data, ref int offset);
}

/// <summary>
///     Represents an AOB pattern.
/// </summary>
public readonly struct AobPattern : IPattern {
    /// <summary>
    ///     The data to compare in the pattern.
    /// </summary>
    public readonly byte[] Data;

    /// <summary>
    ///     The mask data for the pattern.
    /// </summary>
    public readonly bool[] Mask;

    public int Size { get; init; }

    static bool[] GetMaskFromSimple(string simpleStr) {
        var mask = new bool[simpleStr.Length];

        for (var i = 0; i < simpleStr.Length; i++)
            if (simpleStr[i] != '?')
                mask[i] = true;

        return mask;
    }

    /// <summary>
    ///     <para>Gets a pattern from an AOB string.</para>
    ///     Example: <c>FromAobString("01 02 ?? 04")</c><br />
    ///     <strong>Note:</strong> Does not support partial wildcards, e.g., "01 02 ?3 04."
    /// </summary>
    public static AobPattern FromAobString(string pattern) {
        var parts = pattern.Split(' ');
        var bytePattern = new byte[parts.Length];
        var mask = new bool[parts.Length];

        for (var i = 0; i < parts.Length; i++) {
            if (parts[i] == "?" || parts[i] == "??") {
                mask[i] = true;
                continue;
            }

            bytePattern[i] = Convert.ToByte(parts[i], 16);
        }

        return new AobPattern(bytePattern, mask);
    }

    /// <summary>
    ///     <para>Gets a pattern from a string.</para>
    ///     Example: <c>FromString("Hello, world!")</c>
    /// </summary>
    public static AobPattern FromString(string str, bool nullTerminated = true) {
        var pattern = Encoding.ASCII.GetBytes(nullTerminated ? str + "\0" : str);
        return new AobPattern(pattern, new bool[pattern.Length]);
    }

    /// <summary>
    ///     <para>Gets a pattern from a simple pattern.</para>
    ///     Example: <c>FromSimple("\x01\x02\x03\x04")</c>
    /// </summary>
    public static AobPattern FromSimple(string pattern) {
        var bytePattern = Encoding.ASCII.GetBytes(pattern);
        return new AobPattern(bytePattern, new bool[bytePattern.Length]);
    }

    /// <summary>
    ///     <para>Gets a pattern from a simple pattern and a byte array mask.</para>
    ///     Example: <c>FromSimple("\x01\x02\x03\x04", new bool[] { false, false, true, false })</c>
    /// </summary>
    public static AobPattern FromSimple(string pattern, bool[] mask) {
        var bytePattern = Encoding.ASCII.GetBytes(pattern);
        return new AobPattern(bytePattern, mask);
    }

    /// <summary>
    ///     <para>Gets a pattern from a simple pattern and a simple mask.</para>
    ///     Example: <c>FromSimple("\x01\x02\x03\x04", "xx?x")</c>
    /// </summary>
    public static AobPattern FromSimple(string pattern, string mask) {
        var bytePattern = Encoding.ASCII.GetBytes(pattern);
        return new AobPattern(bytePattern, GetMaskFromSimple(mask));
    }

    /// <summary>
    ///     <para>Gets a pattern from a byte array pattern.</para>
    ///     Example: <c>FromRaw(new byte[] { 0x01, 0x02, 0x03, 0x04 })</c>
    /// </summary>
    /// <param name="pattern"></param>
    /// <returns></returns>
    public static AobPattern FromRaw(byte[] pattern) {
        return new AobPattern(pattern, new bool[pattern.Length]);
    }

    /// <summary>
    ///     <para>Gets a patten from a byte array pattern and a byte array mask.</para>
    ///     Example: <code>FromRaw(new byte[] { 0x01, 0x02, 0x03, 0x04 }, new bool[] { false, false, true, false })</code>
    /// </summary>
    public static AobPattern FromRaw(byte[] pattern, bool[] mask) {
        return new AobPattern(pattern, mask);
    }

    /// <summary>
    ///     <para>Gets a pattern from a byte array pattern and a simple mask.</para>
    ///     Example: <c>FromRaw(new byte[] { 0x01, 0x02, 0x03, 0x04 }, "xx?x")</c>
    /// </summary>
    public static AobPattern FromRaw(byte[] pattern, string mask) {
        return new AobPattern(pattern, GetMaskFromSimple(mask));
    }

    internal AobPattern(byte[] pattern, bool[] mask) {
        if (pattern.Length != mask.Length)
            throw new Exception("Pattern and mask must be the same length");

        Data = pattern;
        Mask = mask;
        Size = pattern.Length;
    }

    public bool Compare(uint addr, ref byte[] buffer, ref int offset) {
        for (var i = 0; i < Size; i++) {
            if (Mask[i] || buffer[offset + i] == Data[i])
                continue;

            return false;
        }

        return true;
    }
}

public class ScanOptions {
    /// <summary>
    ///     The alignment to use when scanning.
    ///     Useful for faster scanning, since concepts like prologues and heap allocations are often aligned.
    /// </summary>
    public int Alignment = 1;

    /// <summary>
    ///     The maximum amount of results to find.
    /// </summary>
    public int Limit = int.MaxValue;

    /// <summary>
    ///     The pattern to use.
    /// </summary>
    public IPattern Pattern;

    /// <summary>
    ///     The memory protection flags to require when scanning.
    ///     A true value means the flag must be set, a false value means the flag must not be set.
    /// </summary>
    public Dictionary<MemSecurityFlags, bool> ProtectFlags = null;

    /// <summary>
    ///     The range of virtual memory to scan.
    /// </summary>
    public VirtualRange Range;

    /* I prefer object initialization but I give the freedom to use functions to set as well */

    public ScanOptions WithRange(VirtualRange range) {
        Range = range;
        return this;
    }

    public ScanOptions WithPattern(IPattern pattern) {
        Pattern = pattern;
        return this;
    }

    public ScanOptions WithAlignment(int alignment) {
        Alignment = alignment;
        return this;
    }

    public ScanOptions WithLimit(int limit) {
        Limit = limit;
        return this;
    }

    public ScanOptions WithProtect(MemSecurityFlags flag) {
        ProtectFlags[flag] = true;
        return this;
    }

    public ScanOptions WithoutProtect(MemSecurityFlags flag) {
        ProtectFlags[flag] = false;
        return this;
    }
}

public static class Scanner {
    /// <summary>
    ///     Scan for a pattern in the target's memory.
    /// </summary>
    public static IList<uint> FindPattern(RtProc target, ScanOptions options) {
        var results = Array.Empty<uint>();
        var mbi = new MemoryBasicInformation();
        var buffer = Array.Empty<byte>();
        var mbiSize = Marshal.SizeOf<MemoryBasicInformation>(); // Marshal is slow so memoize that

        for (var at = options.Range.Min; at < options.Range.Max; at += (uint)mbi.RegionSize) {
            if (NtQueryVirtualMemory(target.Handle, at, MemoryInfoClass.MemoryBasicInformation, ref mbi, mbiSize,
                    out _) != NtSuccess)
                break;

            // Skip if the memory isn't readable
            if (!mbi.State.HasFlag(MemAllocFlags.Commit) ||
                mbi.Protect.HasFlag(MemSecurityFlags.NoAccess) ||
                mbi.Protect.HasFlag(MemSecurityFlags.Guard) ||
                mbi.Protect.HasFlag(MemSecurityFlags.NoCache))
                continue;

            // Ensure the protection is correct
            if (options.ProtectFlags != null)
                foreach (var opt in options.ProtectFlags)
                    switch (opt.Value) {
                        case true when (mbi.Protect & opt.Key) == 0:
                        case false when (mbi.Protect & opt.Key) != 0:
                            goto Next;
                    }

            var bufferSize = mbi.RegionSize;

            // Ensure the region size only goes to the end of the range
            if (at + bufferSize > options.Range.Max)
                bufferSize = (int)(options.Range.Max - at);

            // Ensure that the region fits the alignment; truncate if it doesn't
            if (bufferSize % options.Alignment != 0)
                bufferSize -= bufferSize % options.Alignment;

            // Resize the array if it's too small
            if (buffer.Length < bufferSize)
                Array.Resize(ref buffer, bufferSize);

            NtReadVirtualMemory(target.Handle, at, buffer, bufferSize, out var bytesRead);

            for (var i = 0; i < bytesRead - options.Pattern.Size; i += options.Alignment) {
                var addr = at + (uint)i;

                if (!options.Pattern.Compare(addr, ref buffer, ref i))
                    continue;

                Array.Resize(ref results,
                    results.Length + 1); // Manual array resizing to consume least amount of memory possible.
                results[^1] = addr;

                if (results.Length == options.Limit)
                    goto StopScan;
            }

            Next:
            continue;

            StopScan:
            break;
        }

        return results;
    }

    /// <summary>
    ///     Find an unmanaged value in non-executable memory.
    /// </summary>
    // The unmanaged type constraint should keep the code safe enough to not be internalized.
    public static unsafe IList<uint> Find<T>(RtProc target, T value, ScanOptions options) where T : unmanaged {
        var size = Marshal.SizeOf<T>();
        var buffer = new byte[size];

        fixed (byte* bufferPtr = buffer) {
            Marshal.StructureToPtr(value, (IntPtr)bufferPtr, true);
        }

        options.Pattern = AobPattern.FromRaw(buffer);
        return FindPattern(target, options);
    }
}