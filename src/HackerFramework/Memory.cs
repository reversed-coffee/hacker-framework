using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using HackerFramework.NativeObjects;
using HackerFramework.ToolHelpInterface;

using static HackerFramework.Native;

namespace HackerFramework;

/// <summary>
///     Provides methods for reading and writing memory.
/// </summary>
public static class Memory {
    /// <summary>
    ///     Rebases the given RVA to a module.
    /// </summary>
    public static uint Rebase(RtModule module, uint rva, uint? baseRva = null) {
        return module.Base + rva - (baseRva ?? 0);
    }

    /// <summary>
    ///     Debases the given RVA from a module. Vice versa with <see cref="Rebase" />.
    /// </summary>
    public static uint Debase(RtModule module, uint rva, uint? baseRva = null) {
        return rva - module.Base + (baseRva ?? 0);
    }

    /// <summary>
    ///     Get the module at the given address.
    /// </summary>
    public static RtModule ModuleAt(RtProc target, uint addr) {
        var modules = ToolHelp.GetModules(new ModuleFilter { ProcId = target.Id });
        return modules.FirstOrDefault(m => addr >= m.Base && addr < m.Base + m.Size);
    }

    /// <summary>
    ///     Attempts to get a static address if it's relative to a module.
    /// </summary>
    public static string GetStaticAddress(RtProc target, uint addr) {
        var module = ModuleAt(target, addr);
        return module != null ? $"{module.Name}+0x{Debase(module, addr):X}" : null;
    }

    /// <summary>
    ///     Allocates virtual memory in the given process.
    /// </summary>
    public static uint Allocate(RtProc proc, int size, MemSecurityFlags protect = MemSecurityFlags.ExecuteReadWrite) {
        uint baseAddress = 0;

        if (NtAllocateVirtualMemory(proc.Handle, ref baseAddress, 0, size, MemAllocFlags.Commit | MemAllocFlags.Reserve,
                protect) != NtSuccess)
            throw new Win32Exception("Failed to allocate memory.");

        return baseAddress;
    }

    /// <summary>
    ///     Releases virtual memory from the given process.
    /// </summary>
    public static bool Free(RtProc target, uint addr) {
        return NtFreeVirtualMemory(target.Handle, addr, 0, MemAllocFlags.Release) == NtSuccess;
    }

    /// <summary>
    ///     Protects virtual memory at the given address.
    /// </summary>
    public static MemSecurityFlags Protect(RtProc target, uint addr, int size, MemSecurityFlags protect) {
        if (NtProtectVirtualMemory(target.Handle, addr, size, protect, out var old) != NtSuccess)
            throw new Win32Exception("Failed to protect memory.");

        return old;
    }

    /* Generic r/w */

    public static byte[] ReadBytes(RtProc target, uint addr, int size) {
        var buf = new byte[size];

        if (NtReadVirtualMemory(target.Handle, addr, buf, size, out var bytesRead) != NtSuccess || bytesRead != size)
            throw new Win32Exception("Failed to read memory.");

        return buf;
    }

    public static void WriteBytes(RtProc target, uint addr, byte[] value) {
        if (NtWriteVirtualMemory(target.Handle, addr, value, value.Length, out var bytesWritten) != NtSuccess ||
            bytesWritten != value.Length)
            throw new Win32Exception("Failed to write memory.");
    }

    /// <summary>
    ///     Reads an unmanaged value from the given address.
    /// </summary>
    public static unsafe T Read<T>(RtProc target, uint addr) where T : unmanaged {
        var size = Marshal.SizeOf<T>();
        var buf = ReadBytes(target, addr, size);
        
        if (NtReadVirtualMemory(target.Handle, addr, buf, buf.Length, out var bytesRead) != NtSuccess ||
            bytesRead != buf.Length)
            throw new Win32Exception("Failed to read memory.");

        fixed (byte* bufPtr = buf) {
            return Marshal.PtrToStructure<T>((IntPtr)bufPtr)!;
        }
    }

    /// <summary>
    ///     Writes an unmanaged value from the given address.
    /// </summary>
    public static unsafe void Write<T>(RtProc target, uint addr, T value) {
        var buf = new byte[Marshal.SizeOf<T>()];
        
        fixed (byte* bufPtr = buf) {
            Marshal.StructureToPtr(value, (IntPtr)bufPtr, false);
        }

        if (NtWriteVirtualMemory(target.Handle, addr, buf, buf.Length, out var bytesWritten) != NtSuccess ||
            bytesWritten != buf.Length)
            throw new Win32Exception("Failed to write memory.");
    }

    /* int8 */

    public static byte ReadByte(RtProc target, uint addr) {
        return ReadBytes(target, addr, 1)[0];
    }

    public static void WriteByte(RtProc target, uint addr, byte value) {
        WriteBytes(target, addr, new[] { value });
    }

    /* int16 */

    public static ushort ReadUShort(RtProc target, uint addr) {
        return BitConverter.ToUInt16(ReadBytes(target, addr, 2), 0);
    }

    public static short ReadShort(RtProc target, uint addr) {
        return BitConverter.ToInt16(ReadBytes(target, addr, 2), 0);
    }

    public static void WriteUShort(RtProc target, uint addr, ushort value) {
        WriteBytes(target, addr, BitConverter.GetBytes(value));
    }

    public static void WriteShort(RtProc target, uint addr, short value) {
        WriteBytes(target, addr, BitConverter.GetBytes(value));
    }

    /* int32 */

    public static uint ReadPointer(RtProc target, uint addr) {
        // remember: x86 process
        return BitConverter.ToUInt32(ReadBytes(target, addr, 4), 0);
    }

    public static uint ReadUInt(RtProc target, uint addr) {
        return BitConverter.ToUInt32(ReadBytes(target, addr, 4), 0);
    }

    public static int ReadInt(RtProc target, uint addr) {
        return BitConverter.ToInt32(ReadBytes(target, addr, 4), 0);
    }

    public static void WriteUInt(RtProc target, uint addr, uint value) {
        WriteBytes(target, addr, BitConverter.GetBytes(value));
    }

    public static void WriteInt(RtProc target, uint addr, int value) {
        WriteBytes(target, addr, BitConverter.GetBytes(value));
    }

    /* float */

    public static float ReadFloat(RtProc target, uint addr) {
        return BitConverter.ToSingle(ReadBytes(target, addr, 4), 0);
    }

    public static void WriteFloat(RtProc target, uint addr, float value) {
        WriteBytes(target, addr, BitConverter.GetBytes(value));
    }

    /* int64 */

    public static ulong ReadULong(RtProc target, uint addr) {
        return BitConverter.ToUInt64(ReadBytes(target, addr, 8), 0);
    }

    public static long ReadLong(RtProc target, uint addr) {
        return BitConverter.ToInt64(ReadBytes(target, addr, 8), 0);
    }

    public static void WriteULong(RtProc target, uint addr, long value) {
        WriteBytes(target, addr, BitConverter.GetBytes(value));
    }

    public static void WriteLong(RtProc target, uint addr, long value) {
        WriteBytes(target, addr, BitConverter.GetBytes(value));
    }

    /* double */

    public static double ReadDouble(RtProc target, uint addr) {
        return BitConverter.ToDouble(ReadBytes(target, addr, 8), 0);
    }

    public static void WriteDouble(RtProc target, uint addr, double value) {
        WriteBytes(target, addr, BitConverter.GetBytes(value));
    }
}