using System;
using System.Runtime.InteropServices;

namespace HackerFramework;

/// <summary>
///     Provides methods for interacting with the operating system.
/// </summary>
public class Native {
    [Flags]
    public enum MemAllocFlags : uint {
        Commit = 0x1000,
        Reserve = 0x2000,
        Decommit = 0x4000,
        Release = 0x8000,
        Reset = 0x80000,
        Physical = 0x400000,
        TopDown = 0x100000,
        WriteWatch = 0x200000,
        LargePages = 0x20000000
    }

    public enum MemoryInfoClass {
        MemoryBasicInformation,
        MemoryWorkingSetList,
        MemorySectionName,
        MemoryBasicVlmInformation,
        MemoryWorkingSetExList
    }

    [Flags]
    public enum MemSecurityFlags : uint {
        NoAccess = 0x01,
        ReadOnly = 0x02,
        ReadWrite = 0x04,
        WriteCopy = 0x08,
        Execute = 0x10,
        ExecuteRead = 0x20,
        ExecuteReadWrite = 0x40,
        ExecuteWriteCopy = 0x80,
        Guard = 0x100,
        NoCache = 0x200,
        WriteCombine = 0x400
    }

    [Flags]
    public enum ProcAccessFlags : uint {
        Terminate = 0x1,
        CreateThread = 0x02,
        VirtualMemoryOperation = 0x08,
        VirtualMemoryRead = 0x10,
        VirtualMemoryWrite = 0x20,
        DuplicateHandle = 0x40,
        CreateProcess = 0x80,
        SetQuota = 0x100,
        SetInformation = 0x200,
        QueryInformation = 0x400,
        QueryLimitedInformation = 0x1000,
        Synchronize = 0x100000,
        All = 0x001F0FFF
    }

    [Flags]
    public enum SnapFlags : uint {
        HeapList = 0x01,
        Process = 0x02,
        Thread = 0x04,
        Module = 0x08,
        Module32 = 0x10,
        Inherit = 0x80000000,
        SnapAll = HeapList | Process | Thread | Module
    }

    [Flags]
    public enum ThreadAccessFlags : uint {
        Terminate = 0x01,
        SuspendResume = 0x02,
        GetContext = 0x08,
        SetContext = 0x10,
        SetInfo = 0x20,
        QueryInfo = 0x40,
        SetToken = 0x0080,
        Impersonate = 0x0100,
        DirectImpersonate = 0x200,
        All = 0xF0000 | 0x100000 | 0xFFFF
    }

    [Flags]
    public enum ThreadCtxFlags : uint {
        I386 = 0x10000,
        Control = I386 | 0x01,
        Integer = I386 | 0x02,
        Segments = I386 | 0x04,
        FloatingPoint = I386 | 0x08,
        DebugRegisters = I386 | 0x10,
        ExtendedRegisers = I386 | 0x20,
        Full = Control | Integer | Segments,
        All = Control | Integer | Segments | FloatingPoint | DebugRegisters | ExtendedRegisers
    }

    public enum ThreadInfoClass {
        ThreadBasicInformation,
        ThreadTimes,
        ThreadPriority,
        ThreadBasePriority,
        ThreadAffinityMask,
        ThreadImpersonationToken,
        ThreadDescriptorTableEntry,
        ThreadEnableAlignmentFaultFixup,
        ThreadEventPairReusable,
        ThreadQuerySetWin32StartAddress,
        ThreadZeroTlsCell,
        ThreadPerformanceCount,
        ThreadAmILastThread,
        ThreadIdealProcessor,
        ThreadPriorityBoost,
        ThreadSetTlsArrayAddress,
        ThreadIsIoPending,
        ThreadHideFromDebugger,
        ThreadBreakOnTermination,
        ThreadSwitchLegacyState,
        ThreadIsTerminated,
        ThreadLastSystemCall,
        ThreadIoPriority,
        ThreadCycleTime,
        ThreadPagePriority,
        ThreadActualBasePriority,
        ThreadTebInformation,
        ThreadCSwitchMon,
        ThreadCSwitchPmu,
        ThreadWow64Context,
        ThreadGroupInformation,
        ThreadUmsInformation,
        ThreadCounterProfiling,
        ThreadIdealProcessorEx,
        MaxThreadInfoClass
    }

    /* WinNT api functions (because lower level = more control) */

    public const int NtSuccess = 0;
    /* Win32 api functions */

    [DllImport("kernel32.dll")]
    public static extern int OpenProcess(ProcAccessFlags dwDesiredAccessFlags, bool bInheritHandle, int dwProcId);

    [DllImport("kernel32.dll")]
    public static extern int OpenThread(ThreadAccessFlags dwDesiredAccessFlags, bool bInheritHandle, int dwThreadId);

    [DllImport("kernel32.dll")]
    public static extern bool CheckRemoteDebuggerPresent(int hProc, ref bool isDebuggerPresent);

    [DllImport("user32.dll")]
    public static extern uint GetWindowThreadProcessId(int hWnd, out uint lpdwProcessId);

    [DllImport("kernel32.dll")]
    public static extern int CreateToolhelp32Snapshot(SnapFlags snapFlags, int procId);

    [DllImport("kernel32.dll")]
    public static extern bool Module32First(int hSnapshot, ref ModuleEntry lpme);

    [DllImport("kernel32.dll")]
    public static extern bool Module32Next(int hSnapshot, ref ModuleEntry lpme);

    [DllImport("kernel32.dll")]
    public static extern bool Thread32First(int hSnapshot, ref ThreadEntry lpte);

    [DllImport("kernel32.dll")]
    public static extern bool Thread32Next(int hSnapshot, ref ThreadEntry lpte);

    [DllImport("kernel32.dll")]
    public static extern bool Process32First(int hSnapshot, ref ProcessEntry lppe);

    [DllImport("kernel32.dll")]
    public static extern bool Process32Next(int hSnapshot, ref ProcessEntry lppe);

    [DllImport("kernel32.dll", CharSet = CharSet.Ansi)]
    public static extern uint LoadLibraryA(string lpFileName);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Ansi)]
    public static extern uint GetProcAddress(uint hModule, string lpProcName);

    [DllImport("kernel32.dll")]
    public static extern bool FreeLibrary(uint hModule);

    [DllImport("ntdll.dll")]
    public static extern int NtClose(int handle);

    [DllImport("ntdll.dll")]
    public static extern int NtSuspendThread(int hThread, out int suspendCount);

    [DllImport("ntdll.dll")]
    public static extern int NtResumeThread(int hThread, out int suspendCount);

    [DllImport("ntdll.dll")]
    public static extern int NtQueryInformationThread(int hThread, ThreadInfoClass threadInfoClass,
        ref ThreadInfoBasic threadInfo, int threadInfoLength, out int returnLength);

    [DllImport("ntdll.dll")]
    public static extern int NtGetContextThread(int hThread, ref ThreadContext ctx);

    [DllImport("ntdll.dll")]
    public static extern int NtSetContextThread(int hThread, ref ThreadContext ctx);

    [DllImport("ntdll.dll")]
    public static extern uint NtTerminateThread(int hThread, int statusCode);

    [DllImport("ntdll.dll")]
    public static extern int NtReadVirtualMemory(int hProc, uint baseAddress, byte[] buffer, int bufferSize,
        out int bytesRead);

    [DllImport("ntdll.dll")]
    public static extern int NtWriteVirtualMemory(int hProc, uint baseAddress, byte[] buffer, int numberOfBytesToWrite,
        out int numberOfBytesWritten);

    [DllImport("ntdll.dll")]
    public static extern int NtAllocateVirtualMemory(int hProc, ref uint baseAddress, uint zeroBits,
        int regionSize, MemAllocFlags allocType, MemSecurityFlags protect);

    [DllImport("ntdll.dll")]
    public static extern int NtProtectVirtualMemory(int hProc, uint baseAddress, int size,
        MemSecurityFlags newAccessProtection, out MemSecurityFlags oldAccessProtection);

    [DllImport("ntdll.dll")]
    public static extern int NtFreeVirtualMemory(int hProc, uint baseAddress, int regionSize, MemAllocFlags freeType);

    [DllImport("ntdll.dll")]
    public static extern int NtQueryVirtualMemory(int hProc, uint baseAddress, MemoryInfoClass memoryInfoClass,
        ref MemoryBasicInformation memoryInfo, int memoryInfoLength, out int returnLength);

    [StructLayout(LayoutKind.Sequential)]
    public struct ThreadInfoBlock {
        public uint ExceptionList; // PEXCEPTION_REGISTRATION_RECORD
        public uint StackBase;
        public uint StackLimit;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ThreadClientId {
        public int UniqueProcess; // HANDLE
        public int UniqueThread; // HANDLE
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ThreadInfoBasic {
        public uint ExitStatus; // NTSTATUS
        public uint TebBaseAddress; // ThreadInfoBlock*
        public ThreadClientId ClientId;
        public uint AffinityMask; // KAFFINITY
        public uint Priority; // KPRIORITY
        public uint BasePriority; // W32LONG
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct MemoryBasicInformation {
        public uint BaseAddress;
        public int AllocationBase;
        public uint AllocationProtect;
        public int RegionSize;
        public MemAllocFlags State;
        public MemSecurityFlags Protect;
        public uint Type;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct XmmSave {
        public int ControlWord;
        public int StatusWord;
        public int TagWord;
        public int ErrorOffset;
        public int ErrorSelector;
        public int DataOffset;
        public int DataSelector;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 80)]
        public byte[] RegisterArea;

        public int Cr0NpxState;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ThreadContext {
        public ThreadCtxFlags Flags;
        public uint Dr0;
        public uint Dr1;
        public uint Dr2;
        public uint Dr3;
        public uint Dr6;
        public uint Dr7;
        public XmmSave Xmm;
        public uint SegGs;
        public uint SegFs;
        public uint SegEs;
        public uint SegDs;
        public uint Edi;
        public uint Esi;
        public uint Ebx;
        public uint Edx;
        public uint Ecx;
        public uint Eax;
        public uint Ebp;
        public uint Eip;
        public uint SegCs;
        public uint EFlags;
        public uint Esp;
        public uint SegSs;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 512)]
        public byte[] ExtendedRegisters;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ThreadEntry {
        public int Size;
        public uint UsageCount;
        public int ThreadId;
        public int ProcId;
        public int KeBasePriority;
        public int DeltaPriority;
        public uint Flags;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct ModuleEntry {
        internal int Size;
        internal uint ModuleId;
        internal int ProcId;
        internal uint GlblcntUsage;
        internal uint ProccntUsage;
        internal uint ModBaseAddr;
        internal int ModBaseSize;
        internal int ModuleHandle;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        internal string ModuleName;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        internal string ModulePath;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
    public struct ProcessEntry {
        public int Size;
        public uint CntUsage;
        public int ProcId;
        public int DefaultHeapId;
        public int ModuleId;
        public int Threads;
        public int ParentProcId;
        public int PriorityClassBase;
        public uint Flags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
        public string ExeFile;
    }
}