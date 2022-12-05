using System;
using System.Collections.Generic;
using System.Linq;

using HackerFramework.NativeObjects;
using HackerFramework.Streams;

namespace HackerFramework;

// TODO: REWRITE USING HDE32

/// <summary>
///     Provides methods for disassembly.
/// </summary>
public static class Disasm {
    /// <summary>
    ///     Determines if the given address initializes a stack frame.
    /// </summary>
    public static bool PrologueAt(RtProc target, uint addr) {
        var bytes = Memory.ReadBytes(target, addr, 3);

        if (bytes[2] != 0x8B) // Not a "mov r32, r32" instruction
            return false;

        return
            (bytes[0] == 0x55 && bytes[2] == 0xEC) || // push ebp; mov ebp, esp
            (bytes[0] == 0x53 && bytes[2] == 0xDC) || // push ebx; mov ebx, esp
            (bytes[0] == 0x56 && bytes[2] == 0xF4); // push esi; mov esi, esp
    }

    /// <summary>
    ///     Determines if the given address cleans up a stack frame.
    ///     <para>__cdecl has callee clean up stack ("ret" instruction)</para>
    ///     <para>
    ///         __stdcall has caller clean up the stack ("ret imm16" instruction), but that
    ///         can be mutated, such as popping into junk registers or adding onto esp, which
    ///         <strong>isn't taken into acccount</strong>.
    ///     </para>
    /// </summary>
    public static bool EpilogueAt(RtProc target, uint addr) {
        var bytes = Memory.ReadBytes(target, addr - 1, 3);

        if (bytes[0] != 0x5D && bytes[0] != 0xC9) // Not a "pop ebp" or "leave" instruction
            return false;

        switch (bytes[1]) {
            case 0xC3:
                // ret
                return true;
            case 0xC2: {
                // ret imm16
                var imm = Memory.ReadUShort(target, addr + 1);
                return imm % 4 == 0; // Stack is aligned to 4 bytes, so the imm reg must be a multiple of 4.
            }
            default:
                return false;
        }
    }

    /// <summary>
    ///     Gets the prologue of the function at the given address.
    /// </summary>
    public static uint GetPrologue(RtProc target, uint addr) {
        return PrologueAt(target, addr) ? addr : LastPrologue(target, addr);
    }

    /// <summary>
    ///     Gets the epilogue of the function at the given address.
    /// </summary>
    public static uint GetEpilogue(RtProc target, uint addr) {
        return EpilogueAt(target, addr) ? addr : NextEpilogue(target, addr);
    }

    /// <summary>
    ///     Gets the next prologue.
    /// </summary>
    public static uint NextPrologue(RtProc target, uint addr) {
        if (PrologueAt(target, addr))
            addr += 16;

        if (addr % 16 == 0)
            addr += 16 - addr % 16;

        while (!PrologueAt(target, addr))
            addr += 16;

        return addr;
    }

    /// <summary>
    ///     Gets the last prologue.
    /// </summary>
    public static uint LastPrologue(RtProc target, uint addr) {
        if (PrologueAt(target, addr))
            addr -= 16;

        if (addr % 16 != 0)
            addr -= addr % 16;

        while (!PrologueAt(target, addr))
            addr -= 16;

        return addr;
    }

    /// <summary>
    ///     Gets the next epilogue.
    /// </summary>
    public static uint NextEpilogue(RtProc target, uint addr) {
        if (EpilogueAt(target, addr))
            addr++;

        while (!EpilogueAt(target, addr))
            addr++;

        return addr;
    }

    /// <summary>
    ///     Gets the last epilogue.
    /// </summary>
    public static uint LastEpilogue(RtProc target, uint addr) {
        if (EpilogueAt(target, addr))
            addr--;

        while (!EpilogueAt(target, addr))
            addr--;

        return addr;
    }

    /// <summary>
    ///     Gets the address of a rel32 operand.
    /// </summary>
    public static uint GetRel32(RtProc target, uint addr) {
        return addr + 5 + Memory.ReadPointer(target, addr + 1);
    }

    /// <summary>
    ///     Determines if the address is a call rel32 instruction.
    /// </summary>
    public static bool IsRel32Call(RtProc target, uint addr) {
        // Not a call instruction
        if (Memory.ReadByte(target, addr) != 0xE8)
            return false;

        var dest = GetRel32(target, addr);

        // Ensure the dest is aligned
        if (dest % 16 != 0)
            return false;

        // Check if the call's dest is within the same module
        return Memory.ModuleAt(target, addr) == Memory.ModuleAt(target, dest);
    }

    /// <summary>
    ///     Determines if the address is a jmp rel32 instruction.
    /// </summary>
    public static bool IsRel32Jmp(RtProc target, uint addr) {
        // Not a jmp instruction
        if (Memory.ReadByte(target, addr) != 0xE9)
            return false;

        var dest = GetRel32(target, addr);

        // Ensure the dest is aligned
        if (dest % 16 != 0)
            return false;

        // Check if the jmp's dest is within the same module
        return Memory.ModuleAt(target, addr) == Memory.ModuleAt(target, dest);
    }

    /// <summary>
    ///     Gets the next instruction that matches the predicate.
    /// </summary>
    public static uint NextGeneric(RtProc target, uint addr, Func<RtProc, uint, bool> predicate) {
        while (predicate(target, addr))
            addr++;

        return addr;
    }

    /// <summary>
    ///     Gets the last instruction that matches the predicate.
    /// </summary>
    public static uint LastGeneric(RtProc target, uint addr, Func<RtProc, uint, bool> predicate) {
        while (predicate(target, addr))
            addr--;

        return addr;
    }

    /// <summary>
    ///     Gets the cross references (calls) to the given address.
    /// </summary>
    public static IList<uint> GetXrefs(RtProc target, uint addr) {
        var calls = Scanner.FindPattern(target, new ScanOptions {
            Range = new VirtualRange(Memory.ModuleAt(target, addr)),
            Pattern = AobPattern.FromSimple("\xE8\x00\x00\x00\x00", "x????")
        });

        return calls.Where(call => GetRel32(target, call) == addr).ToList();
    }

    /// <summary>
    ///     Gets the cross references to the given string.
    /// </summary>
    public static IList<uint> GetXrefs(RtProc target, string str, bool isConstant = true, RtModule module = null) {
        module ??= target.MainModule;

        using var stream = new TargetStream(target, module.Base);
        var pe = Pe32.Open(stream);

        var sectName = isConstant ? ".rdata" : ".data";

        if (!pe.Sections.TryGetValue(sectName, out var section))
            throw new Exception($"No section exists called '{sectName}'");

        var rva = Scanner.FindPattern(target, new ScanOptions {
            Range = new VirtualRange(section),
            Pattern = AobPattern.FromString(str),
            Limit = 1,
            Alignment = 4
        }).FirstOrDefault();

        if (rva == 0)
            throw new Exception($"No xrefs to string '{str}' exist in module '{module.Name}'.");

        return Scanner.Find(target, rva, new ScanOptions {
            Range = new VirtualRange(module)
        });
    }
}