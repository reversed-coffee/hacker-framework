using System;
using System.Collections.Generic;
using System.Threading;

using HackerFramework.ToolHelpInterface;

using static HackerFramework.Native;

namespace HackerFramework.NativeObjects;

/// <summary>
///     Represents a Windows process.
/// </summary>
public class RtProc : IDisposable, IEquatable<RtProc> {
    bool _disposed;
    internal int Handle;

    internal RtProc(int procId, bool waitForModule = true) {
        Id = procId;

        if (!waitForModule)
            return;

        // I've had this issue where the process would be initialized, but there would be no main module.
        // This should fix that.
        var moduleFilter = new ModuleFilter { ProcId = Id };
        IList<RtModule> moduleList;

        do {
            moduleList = ToolHelp.GetModules(moduleFilter, 1);
            Thread.Sleep(10);
        } while (moduleList.Count == 0);
    }

    /// <summary>
    ///     The process' ID.
    /// </summary>
    public int Id { get; internal set; }

    /// <summary>
    ///     Determines if the process has an open handle.
    /// </summary>
    public bool IsOpen { get; private set; }

    /// <summary>
    ///     Indicates if a debugger is attached to the process.
    /// </summary>
    public bool IsDebuggerPresent {
        get {
            var flag = false;
            CheckRemoteDebuggerPresent(Handle, ref flag);

            return flag;
        }
    }

    /// <summary>
    ///     The main module of the process.
    /// </summary>
    public RtModule MainModule { get; internal set; }

    /// <summary>
    ///     The name of the process' main module.
    /// </summary>
    public string ExeName => MainModule.Name;

    /// <summary>
    ///     The full path to the process' main module.
    /// </summary>
    public string ExePath => MainModule.Path;

    public void Dispose() {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public bool Equals(RtProc other) {
        return other != null && Id == other.Id;
    }

    /// <summary>
    ///     Creates a new <see cref="RtProc" /> from a process id and opens its handle.
    /// </summary>
    public static RtProc FromId(int procId) {
        var target = new RtProc(procId);
        target.Open();
        return target;
    }

    /// <summary>
    ///     Opens the process, allowing you to utilize it.
    /// </summary>
    public RtProc Open() {
        if (IsOpen)
            return this;

        Handle = OpenProcess(ProcAccessFlags.All, false, Id);
        IsOpen = Handle != 0;

        return IsOpen ? this : null;
    }

    /// <summary>
    ///     Closes the process; deallocates the process' handle.
    /// </summary>
    public bool Close() {
        return !IsOpen || NtClose(Handle) == NtSuccess;
    }

    protected virtual void Dispose(bool disposing) {
        if (_disposed)
            return;

        _disposed = true;

        if (disposing)
            Close();
    }

    // Destructor because people are bozos and don't call Dispose().
    ~RtProc() {
        Dispose(false);
    }
}