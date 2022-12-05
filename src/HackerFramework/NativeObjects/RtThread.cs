using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace HackerFramework.NativeObjects;

using static Native;

/// <summary>
///     Represents a native Win32 thread.
/// </summary>
public class RtThread : IDisposable, IEquatable<RtThread> {
    /// <summary>
    ///     The thread's ID.
    /// </summary>
    public readonly int Id;

    /// <summary>
    ///     The thread's owner process.
    /// </summary>
    public readonly RtProc Target;

    bool _disposed;

    int _handle;

    internal RtThread(RtProc target, int threadId) {
        Target = target;
        Id = threadId;
    }

    /// <summary>
    ///     Determines if the thread is open.
    /// </summary>
    public bool IsOpen { get; private set; }

    /// <summary>
    ///     Indicates if the thread has a suspension record.
    /// </summary>
    public bool IsFrozen {
        get {
            if (NtSuspendThread(_handle, out var suspendCount) != NtSuccess ||
                NtResumeThread(_handle, out _) != NtSuccess)
                return false;

            return suspendCount != 0;
        }
    }

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public bool Equals(RtThread other) {
        return other != null && other.Id == Id && other.Target == Target;
    }

    /// <summary>
    ///     Opens the thread, allowing you to perform actions on it.
    /// </summary>
    public RtThread Open() {
        if (IsOpen)
            return this;

        _handle = OpenThread(ThreadAccessFlags.All, false, Id);
        IsOpen = _handle != 0;

        return IsOpen ? this : null;
    }

    /// <summary>
    ///     Closes the thread; deallocates the thread's handle.
    /// </summary>
    public bool Close() {
        return !IsOpen || NtClose(_handle) == NtSuccess;
    }

    /// <summary>
    ///     Suspends the thread.
    /// </summary>
    public bool Freeze() {
        return NtSuspendThread(_handle, out _) == NtSuccess;
    }

    /// <summary>
    ///     Resumes the thread. Unwrapping will unwrap all suspensions on the thread.
    /// </summary>
    public bool Thaw(bool unwrap = false) {
        if (!unwrap)
            return NtResumeThread(_handle, out _) == NtSuccess;

        while (NtResumeThread(_handle, out var suspendCount) == NtSuccess
               && suspendCount > 0) { }

        return true;
    }

    /// <summary>
    ///     <para>Terminates the thread.</para>
    ///     <strong>Note:</strong> Only use this if you understand what you're doing. Abruptly killing a thread without
    ///     notification can cause memory leaks, deadlocks, etc.
    /// </summary>
    public bool Kill(int exitCode) {
        return NtTerminateThread(_handle, exitCode) == NtSuccess;
    }

    /// <summary>
    ///     Gets the thread's execution cache. Contains information such as the thread's current instruction pointer, stack
    ///     pointer, etc.
    /// </summary>
    public ThreadContext GetCtx() {
        var ctx = new ThreadContext { Flags = ThreadCtxFlags.All };

        if (NtGetContextThread(_handle, ref ctx) != NtSuccess)
            throw new Win32Exception("Failed to get thread context.");

        return ctx;
    }

    /// <summary>
    ///     Sets the thread's execution cache.
    /// </summary>
    public bool SetCtx(ThreadContext ctx) {
        return NtSetContextThread(_handle, ref ctx) == NtSuccess;
    }

    /// <summary>
    ///     Sets the thread's instruction pointer to the given address.
    /// </summary>
    public bool SetEip(uint eip) {
        var ctx = GetCtx();
        ctx.Eip = eip;
        return SetCtx(ctx);
    }

    public uint[] DumpStack(RtProc target, out int stackSize) {
        Freeze();

        var basicInfo = new ThreadInfoBasic();

        if (NtQueryInformationThread(_handle, 0, ref basicInfo, Marshal.SizeOf<ThreadInfoBasic>(), out _) !=
            NtSuccess) {
            stackSize = 0;
            return Array.Empty<uint>();
        }

        var tib = Memory.Read<ThreadInfoBlock>(target, basicInfo.TebBaseAddress);

        var ctx = GetCtx();
        var absStackSize = (int)(tib.StackBase - ctx.Esp);
        stackSize = absStackSize / IntPtr.Size;
        var stack = new uint[stackSize];

        NtReadVirtualMemory(target.Handle, ctx.Esp, Unsafe.As<byte[]>(stack), absStackSize, out _);

        Thaw();

        return stack;
    }

    protected virtual void Dispose(bool disposing) {
        if (_disposed)
            return;

        _disposed = true;

        if (disposing && IsOpen)
            Close();
    }

    ~RtThread() {
        Dispose(false);
    }
}