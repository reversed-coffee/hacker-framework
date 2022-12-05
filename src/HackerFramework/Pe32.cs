using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

using HackerFramework.Streams;

namespace HackerFramework;

// The entirety of this work is based on documentation from https://en.wikibooks.org/wiki/X86_Disassembly/Windows_Executable_Files#PE_Files

[StructLayout(LayoutKind.Sequential)]
public struct DosHeader {
    public ushort Signature;
    public ushort LastSize;
    public ushort BlockCount;
    public ushort RelocCount;
    public ushort HeaderSize;
    public ushort MinAlloc;
    public ushort MaxAlloc;
    public uint InitialSS; // void*
    public uint InitialSP; // void*
    public ushort Checksum;
    public uint InitialIP; // void*
    public uint InitialCS; // void*
    public ushort RelocTableOffset;
    public ushort OverlayNumber;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
    public ushort[] Reserved1;

    public ushort OemId;
    public ushort OemInfo;

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
    public ushort[] Reserved2;

    public uint PEOffset;
}

[StructLayout(LayoutKind.Sequential)]
public struct CoffHeader {
    public ushort Machine;
    public ushort NumberOfSections;
    public uint TimeDateStamp;
    public uint PointerToSymbolTable;
    public uint NumberOfSymbols;
    public ushort SizeOfOptionalHeader;
    public ushort Characteristics;
}

[StructLayout(LayoutKind.Sequential)]
public struct PeOptHeader {
    public ushort Magic;
    public byte MajorLinkerVersion;
    public byte MinorLinkerVersion;
    public uint SizeOfCode;
    public uint SizeOfInitializedData;
    public uint SizeOfUninitializedData;
    public uint AddressOfEntryPoint;
    public uint BaseOfCode;
    public uint BaseOfData;
    public uint ImageBase;
    public uint SectionAlignment;
    public uint FileAlignment;
    public ushort MajorOperatingSystemVersion;
    public ushort MinorOperatingSystemVersion;
    public ushort MajorImageVersion;
    public ushort MinorImageVersion;
    public ushort MajorSubsystemVersion;
    public ushort MinorSubsystemVersion;
    public uint Win32VersionValue;
    public uint SizeOfImage;
    public uint SizeOfHeaders;
    public uint CheckSum;
    public ushort Subsystem;
    public ushort DllCharacteristics;
    public uint SizeOfStackReserve;
    public uint SizeOfStackCommit;
    public uint SizeOfHeapReserve;
    public uint SizeOfHeapCommit;
    public uint LoaderFlags;
    public uint NumberOfRvaAndSizes;
}

[StructLayout(LayoutKind.Sequential)]
public struct DataDirectory {
    public uint VirtualAddress;
    public uint Size;
}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
public struct SectionHeader {
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 8)]
    public string Name; // IMAGE_SIZEOF_SHORT_NAME == 8

    public uint VirtualSize;
    public uint VirtualAddress;
    public uint SizeOfRawData;
    public uint PointerToRawData;
    public uint PointerToRelocations;
    public uint PointerToLineNumbers;
    public ushort NumberOfRelocations;
    public ushort NumberOfLineNumbers;
    public uint Characteristics;
}

/// <summary>
///     Represents an x86 portable executable.
/// </summary>
public class Pe32 {
    public CoffHeader CoffHeader;
    public DataDirectory[] DataDirectories;
    public DosHeader DosHeader;
    public PeOptHeader PeOptHeader;
    public SectionHeader[] SectionHeaders;
    public Dictionary<string, SectionHeader> Sections;

    internal Pe32() { }

    public static Pe32 Open(byte[] buffer) {
        using var stream = new MemoryStream(buffer);
        return Open(stream);
    }

    public static Pe32 Open(Stream stream) {
        var reader = new MemoryReader(stream);

        // Read DOS header
        var dosHeader = reader.ReadStructFast<DosHeader>();

        if (dosHeader.Signature != 0x5A4D)
            throw new InvalidDataException("Invalid DOS header signature");

        // Jump to PE header
        stream.Seek(dosHeader.PEOffset - Marshal.SizeOf<DosHeader>(), SeekOrigin.Current);

        // Verify signature
        var peSig = reader.ReadUInt32();

        if (peSig != 0x00004550)
            throw new InvalidDataException("Invalid PE signature");

        // Read COFF header
        var coffHeader = reader.ReadStructFast<CoffHeader>();

        if (coffHeader.Machine != 0x14C) // 0x14C = Intel 386 (i386 or x86)
            throw new InvalidDataException("Invalid COFF header machine");

        // Read optional header
        var optHeader = reader.ReadStructFast<PeOptHeader>();

        if (optHeader.Magic != 0x10B) // 0x10B = PE32
            throw new InvalidDataException("Invalid PE optional header magic");

        // Read data directories
        var dataDirectories = new DataDirectory[optHeader.NumberOfRvaAndSizes];

        for (var i = 0; i < dataDirectories.Length; i++)
            dataDirectories[i] = reader.ReadStructFast<DataDirectory>();

        // Read section headers
        var sectionHeaders = new SectionHeader[coffHeader.NumberOfSections];

        for (var i = 0; i < sectionHeaders.Length; i++)
            sectionHeaders[i] = reader.ReadStructFast<SectionHeader>();

        return new Pe32 {
            DosHeader = dosHeader,
            CoffHeader = coffHeader,
            PeOptHeader = optHeader,
            DataDirectories = dataDirectories,
            SectionHeaders = sectionHeaders,
            Sections = sectionHeaders.ToDictionary(x => x.Name)
        };
    }
}