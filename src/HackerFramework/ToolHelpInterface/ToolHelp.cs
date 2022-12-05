using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using HackerFramework.NativeObjects;

using static HackerFramework.Native;

namespace HackerFramework.ToolHelpInterface;

/// <summary>
///     A class used to communicate with the toolhelp interface.
/// </summary>
public static class ToolHelp {
    /// <summary>
    ///     Gets a list of modules with a filter.
    /// </summary>
    public static IList<RtModule> GetModules(ModuleFilter filter, int limit = int.MaxValue) {
        if (filter.ProcId == default)
            throw new ArgumentException("Process ID must be specified.", nameof(filter));

        var modules = new List<RtModule>();

        var snap = CreateToolhelp32Snapshot(SnapFlags.Module | SnapFlags.Module32, filter.ProcId);

        if (snap == -1)
            return modules;

        var entry = new ModuleEntry { Size = Marshal.SizeOf<ModuleEntry>() };

        if (Module32First(snap, ref entry))
            do {
                if (filter.Name != null && !string.Equals(entry.ModuleName, filter.Name,
                        StringComparison.InvariantCultureIgnoreCase))
                    continue;

                if (filter.Path != null && entry.ModulePath != filter.Path)
                    continue;

                modules.Add(new RtModule(new RtProc(entry.ProcId, false), entry.ModBaseAddr, entry.ModBaseSize) {
                    Path = entry.ModulePath
                });

                if (modules.Count == limit)
                    break;
            } while (Module32Next(snap, ref entry));

        NtClose(snap);

        return modules;
    }

    /// <summary>
    ///     Gets a list of threads with a filter.
    /// </summary>
    public static IList<RtThread> GetThreads(ThreadFilter? filter = null, int limit = int.MaxValue) {
        var threads = new List<RtThread>();

        var procId = filter?.ProcId ?? throw new InvalidOperationException("Process id cannot be null.");

        var snap = CreateToolhelp32Snapshot(SnapFlags.Thread,
            0); // Process ID scoping doesn't work for TH32CS_SNAPTHREAD

        if (snap == -1)
            return threads;

        var entry = new ThreadEntry { Size = Marshal.SizeOf<ThreadEntry>() };

        if (Thread32First(snap, ref entry))
            do {
                if (entry.ProcId != procId)
                    continue;

                threads.Add(new RtThread(new RtProc(entry.ProcId), entry.ThreadId));

                if (threads.Count == limit)
                    break;
            } while (Thread32Next(snap, ref entry));

        NtClose(snap);

        return threads;
    }
}