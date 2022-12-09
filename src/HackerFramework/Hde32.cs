using System;

namespace HackerFramework;

public struct Hde32Reg {
    byte[] _data = new byte[4];

    public Hde32Reg() { }

    public uint I32 {
        get => BitConverter.ToUInt32(_data, 0);
        set => _data = BitConverter.GetBytes(value);
    }

    public ushort I16 {
        get => BitConverter.ToUInt16(_data, 0);
        set => _data = BitConverter.GetBytes(value);
    }

    public byte I8 {
        get => _data[0];
        set => _data[0] = value;
    }

    public override string ToString() {
        return $"int32 0x{I32:X8}, int16 0x{I16:X4}, int8 0x{I8:X2}";
    }
}

public enum Hde32Seg {
    None,
    Cs = 0x2E,
    Ss = 0x36,
    Ds = 0x3E,
    Es = 0x26,
    Fs = 0x64,
    Gs = 0x65
}

public struct Hde32Inst {
    public HdeFlags Flags = new();
    public byte Size;
    public Hde32Seg SegOverride = new();
    public ushort Opcode;
    public byte Dst;
    public byte ModRmMod;
    public byte ModRmReg;
    public byte Src;
    public byte Sib;
    public byte SibScale;
    public byte SibIndex;
    public byte SibBase;
    public Hde32Reg Imm = new();
    public Hde32Reg Disp = new();

    public Hde32Inst() { }
}

[Flags]
public enum HdeFlags {
    ModRm = 0x02,
    Sib = 0x04,
    Imm8 = 0x08,
    Imm16 = 0x10,
    Imm32 = 0x20,
    Disp8 = 0x40,
    Disp16 = 0x80,
    Disp32 = 0x100,
    Relative = 0x200,
    Imm16X2 = 0x400,
    Error = 0x800,
    ErrorOpcode = 0x1000,
    ErrorLength = 0x2000,
    ErrorLock = 0x4000,
    ErrorOperand = 0x8000,
    PrefixRepne = 0x10000,
    PrefixRep = 0x20000,
    Prefix66 = 0x40000,
    Prefix67 = 0x80000,
    PrefixLock = 0x100000,
    PrefixSeg = 0x200000
}

public class Hde32 {
    const byte CModrm = 0x01;
    const byte CImm8 = 0x02;
    const byte CImm16 = 0x04;
    const byte CImmP66 = 0x10;
    const byte CRel8 = 0x20;
    const byte CRel32 = 0x40;
    const byte CGroup = 0x80;
    const byte CError = 0xFF;

    const uint PreNone = 0x01;
    const uint PreF2 = 0x02;
    const uint PreF3 = 0x04;
    const uint Pre66 = 0x08;
    const uint Pre67 = 0x10;
    const uint PreLock = 0x20;
    const uint PreSeg = 0x40;

    const int DeltaOpcodes = 0x04a;
    const int DeltaFpuReg = 0x0f1;
    const int DeltaFpuModrm = 0x0f8;
    const int DeltaPrefixes = 0x130;
    const int DeltaOpLockOk = 0x1a1;
    const int DeltaOp2LockOk = 0x1b9;
    const int DeltaOpOnlyMem = 0x1cb;
    const int DeltaOp2OnlyMem = 0x1da;
    public static readonly string[] R8Names = { "AL", "CL", "DL", "BL", "AH", "CH", "DH", "BH" };
    public static readonly string[] R16Names = { "AX", "CX", "DX", "BX", "SP", "BP", "SI", "DI" };
    public static readonly string[] R32Names = { "EAX", "ECX", "EDX", "EBX", "ESP", "EBP", "ESI", "EDI" };

    static readonly byte[] Hde32Table = {
        0xa3, 0xa8, 0xa3, 0xa8, 0xa3, 0xa8, 0xa3, 0xa8, 0xa3, 0xa8, 0xa3, 0xa8, 0xa3, 0xa8, 0xa3,
        0xa8, 0xaa, 0xaa, 0xaa, 0xaa, 0xaa, 0xaa, 0xaa, 0xaa, 0xac, 0xaa, 0xb2, 0xaa, 0x9f, 0x9f,
        0x9f, 0x9f, 0xb5, 0xa3, 0xa3, 0xa4, 0xaa, 0xaa, 0xba, 0xaa, 0x96, 0xaa, 0xa8, 0xaa, 0xc3,
        0xc3, 0x96, 0x96, 0xb7, 0xae, 0xd6, 0xbd, 0xa3, 0xc5, 0xa3, 0xa3, 0x9f, 0xc3, 0x9c, 0xaa,
        0xaa, 0xac, 0xaa, 0xbf, 0x03, 0x7f, 0x11, 0x7f, 0x01, 0x7f, 0x01, 0x3f, 0x01, 0x01, 0x90,
        0x82, 0x7d, 0x97, 0x59, 0x59, 0x59, 0x59, 0x59, 0x7f, 0x59, 0x59, 0x60, 0x7d, 0x7f, 0x7f,
        0x59, 0x59, 0x59, 0x59, 0x59, 0x59, 0x59, 0x59, 0x59, 0x59, 0x59, 0x59, 0x9a, 0x88, 0x7d,
        0x59, 0x50, 0x50, 0x50, 0x50, 0x59, 0x59, 0x59, 0x59, 0x61, 0x94, 0x61, 0x9e, 0x59, 0x59,
        0x85, 0x59, 0x92, 0xa3, 0x60, 0x60, 0x59, 0x59, 0x59, 0x59, 0x59, 0x59, 0x59, 0x59, 0x59,
        0x59, 0x59, 0x9f, 0x01, 0x03, 0x01, 0x04, 0x03, 0xd5, 0x03, 0xcc, 0x01, 0xbc, 0x03, 0xf0,
        0x10, 0x10, 0x10, 0x10, 0x50, 0x50, 0x50, 0x50, 0x14, 0x20, 0x20, 0x20, 0x20, 0x01, 0x01,
        0x01, 0x01, 0xc4, 0x02, 0x10, 0x00, 0x00, 0x00, 0x00, 0x01, 0x01, 0xc0, 0xc2, 0x10, 0x11,
        0x02, 0x03, 0x11, 0x03, 0x03, 0x04, 0x00, 0x00, 0x14, 0x00, 0x02, 0x00, 0x00, 0xc6, 0xc8,
        0x02, 0x02, 0x02, 0x02, 0x00, 0x00, 0xff, 0xff, 0xff, 0xff, 0x00, 0x00, 0x00, 0xff, 0xca,
        0x01, 0x01, 0x01, 0x00, 0x06, 0x00, 0x04, 0x00, 0xc0, 0xc2, 0x01, 0x01, 0x03, 0x01, 0xff,
        0xff, 0x01, 0x00, 0x03, 0xc4, 0xc4, 0xc6, 0x03, 0x01, 0x01, 0x01, 0xff, 0x03, 0x03, 0x03,
        0xc8, 0x40, 0x00, 0x0a, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x7f, 0x00, 0x33, 0x01, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0xff, 0xbf, 0xff, 0xff, 0x00, 0x00, 0x00, 0x00, 0x07, 0x00,
        0x00, 0xff, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0xff, 0xff, 0x00, 0x00, 0x00, 0xbf, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x7f, 0x00, 0x00, 0xff, 0x4a, 0x4a, 0x4a, 0x4a, 0x4b, 0x52, 0x4a, 0x4a, 0x4a, 0x4a, 0x4f,
        0x4c, 0x4a, 0x4a, 0x4a, 0x4a, 0x4a, 0x4a, 0x4a, 0x4a, 0x55, 0x45, 0x40, 0x4a, 0x4a, 0x4a,
        0x45, 0x59, 0x4d, 0x46, 0x4a, 0x5d, 0x4a, 0x4a, 0x4a, 0x4a, 0x4a, 0x4a, 0x4a, 0x4a, 0x4a,
        0x4a, 0x4a, 0x4a, 0x4a, 0x4a, 0x61, 0x63, 0x67, 0x4e, 0x4a, 0x4a, 0x6b, 0x6d, 0x4a, 0x4a,
        0x45, 0x6d, 0x4a, 0x4a, 0x44, 0x45, 0x4a, 0x4a, 0x00, 0x00, 0x00, 0x02, 0x0d, 0x06, 0x06,
        0x06, 0x06, 0x0e, 0x00, 0x00, 0x00, 0x00, 0x06, 0x06, 0x06, 0x00, 0x06, 0x06, 0x02, 0x06,
        0x00, 0x0a, 0x0a, 0x07, 0x07, 0x06, 0x02, 0x05, 0x05, 0x02, 0x02, 0x00, 0x00, 0x04, 0x04,
        0x04, 0x04, 0x00, 0x00, 0x00, 0x0e, 0x05, 0x06, 0x06, 0x06, 0x01, 0x06, 0x00, 0x00, 0x08,
        0x00, 0x10, 0x00, 0x18, 0x00, 0x20, 0x00, 0x28, 0x00, 0x30, 0x00, 0x80, 0x01, 0x82, 0x01,
        0x86, 0x00, 0xf6, 0xcf, 0xfe, 0x3f, 0xab, 0x00, 0xb0, 0x00, 0xb1, 0x00, 0xb3, 0x00, 0xba,
        0xf8, 0xbb, 0x00, 0xc0, 0x00, 0xc1, 0x00, 0xc7, 0xbf, 0x62, 0xff, 0x00, 0x8d, 0xff, 0x00,
        0xc4, 0xff, 0x00, 0xc5, 0xff, 0x00, 0xff, 0xff, 0xeb, 0x01, 0xff, 0x0e, 0x12, 0x08, 0x00,
        0x13, 0x09, 0x00, 0x16, 0x08, 0x00, 0x17, 0x09, 0x00, 0x2b, 0x09, 0x00, 0xae, 0xff, 0x07,
        0xb2, 0xff, 0x00, 0xb4, 0xff, 0x00, 0xb5, 0xff, 0x00, 0xc3, 0x01, 0x00, 0xc7, 0xff, 0xbf,
        0xe7, 0x08, 0x00, 0xf0, 0x02, 0x00
    };

    public static uint Disasm(byte[] code, int offset, out Hde32Inst hs) {
        if (code.Length - offset < 16)
            Array.Resize(ref code, offset + 16);

        var hde32Table = Hde32Table.AsSpan();
        hs = new Hde32Inst();

        byte p = 0, c = 0;
        var pref = 0u;
        var ht = 0;

        for (var i = 0; i < 16; i++) {
            c = code[p++];

            switch (c) {
                case 0xf3:
                    pref |= PreF3;
                    hs.Flags |= HdeFlags.PrefixRep;
                    break;
                case 0xf2:
                    pref |= PreF2;
                    hs.Flags |= HdeFlags.PrefixRepne;
                    break;
                case 0xf0:
                    pref |= PreLock;
                    hs.Flags |= HdeFlags.PrefixLock;
                    break;
                case 0x26:
                case 0x2e:
                case 0x36:
                case 0x3e:
                case 0x64:
                case 0x65:
                    hs.SegOverride = (Hde32Seg)c;
                    pref |= PreSeg;
                    hs.Flags |= HdeFlags.PrefixSeg;
                    break;
                case 0x66:
                    pref |= Pre66;
                    hs.Flags |= HdeFlags.Prefix66;
                    break;
                case 0x67:
                    pref |= Pre67;
                    hs.Flags |= HdeFlags.Prefix67;
                    break;
                default:
                    goto pref_done;
            }
        }

        pref_done:
        hs.Flags = (HdeFlags)(pref << 23);

        if (pref == 0)
            pref |= PreNone;

        byte opcode2 = 0;

        if ((hs.Opcode = c) == 0x0f) {
            opcode2 = c = code[p++];
            ht += DeltaOpcodes;
        }
        else if (c is >= 0xa0 and <= 0xa3) {
            if ((pref & Pre67) != 0)
                pref |= Pre66;
            else
                pref &= ~Pre66;
        }

        var opcode = c;
        var cflags = hde32Table[ht + hde32Table[ht + opcode / 4] + opcode % 4];

        if (cflags == CError) {
            hs.Flags |= HdeFlags.Error | HdeFlags.ErrorOpcode;
            cflags = 0;

            if ((opcode & -3) == 0x24)
                cflags++;
        }

        var x = 0;

        if ((cflags & CGroup) != 0) {
            var ptr = ht + (cflags & 0x7f);
            var t = BitConverter.ToUInt16(hde32Table[ptr..(ptr + 2)]);
            cflags = (byte)t;
            x = (byte)(t >> 8);
        }

        if (opcode2 != 0) {
            ht = DeltaPrefixes;

            if ((hde32Table[ht + hde32Table[ht + opcode / 4] + opcode % 4] & pref) != 0)
                hs.Flags |= HdeFlags.Error | HdeFlags.ErrorOpcode;
        }

        byte dispSize = 0;

        if ((cflags & CModrm) != 0) {
            hs.Flags |= HdeFlags.ModRm;
            hs.Dst = c = code[p++];

            byte mMod;
            hs.ModRmMod = mMod = (byte)(c >> 6);

            byte mRm;
            hs.Src = mRm = (byte)(c & 7);

            byte mReg;
            hs.ModRmReg = mReg = (byte)((c & 0x3F) >> 3);

            if (x != 0 && ((x << mReg) & 0x80) != 0)
                hs.Flags |= HdeFlags.Error | HdeFlags.ErrorOpcode;

            if (opcode2 == 0 && opcode is >= 0xd9 and <= 0xdf) {
                var t = (byte)(opcode - 0xd9);

                if (mMod == 3) {
                    ht = DeltaFpuModrm + t * 8;
                    t = (byte)(hde32Table[ht + mReg] << mRm);
                }
                else {
                    ht = DeltaFpuReg;
                    t = (byte)(hde32Table[ht + t] << mReg);
                }

                if ((t & 0x80) != 0)
                    hs.Flags |= HdeFlags.Error | HdeFlags.ErrorOpcode;
            }

            if ((pref & PreLock) != 0) {
                if (mMod == 3) {
                    hs.Flags |= HdeFlags.Error | HdeFlags.ErrorLock;
                }
                else {
                    int end, op = opcode;

                    if (opcode2 != 0) {
                        ht = DeltaOp2LockOk;
                        end = ht + DeltaOpOnlyMem - DeltaOp2LockOk;
                    }
                    else {
                        ht = DeltaOpLockOk;
                        end = DeltaOp2LockOk - DeltaOpLockOk;
                        op &= -2;
                    }

                    for (; ht != end; ht++)
                        if (hde32Table[ht++] == op) {
                            if (((hde32Table[ht] << mReg) & 0x80) == 0)
                                goto no_lock_error;

                            break;
                        }

                    hs.Flags |= HdeFlags.Error | HdeFlags.ErrorLock;
                    no_lock_error: ;
                }
            }

            if (opcode2 != 0)
                switch (opcode) {
                    case 0x20:
                    case 0x22:
                        mMod = 3;

                        if (mReg is > 4 or 1)
                            goto error_operand;

                        goto no_error_operand;
                    case 0x21:
                    case 0x23:
                        mMod = 3;

                        if (mReg is 4 or 5)
                            goto error_operand;

                        goto no_error_operand;
                }
            else
                switch (opcode) {
                    case 0x8c:
                        if (mReg > 5)
                            goto error_operand;

                        goto no_error_operand;
                    case 0x8e:
                        if (mReg is 1 or > 5)
                            goto error_operand;

                        goto no_error_operand;
                }

            if (mMod == 3) {
                int end;

                if (opcode2 != 0) {
                    ht = DeltaOp2OnlyMem;
                    end = ht + hde32Table.Length - DeltaOp2OnlyMem;
                }
                else {
                    ht = DeltaOpOnlyMem;
                    end = ht + DeltaOp2OnlyMem - DeltaOpOnlyMem;
                }

                for (; ht != end; ht += 2)
                    if (hde32Table[ht++] == opcode) {
                        if ((hde32Table[ht] & pref) != 0 && ((hde32Table[ht] << mReg) & 0x80) == 0)
                            goto error_operand;

                        break;
                    }

                goto no_error_operand;
            }

            if (opcode2 != 0)
                switch (opcode) {
                    case 0x50:
                    case 0xd7:
                    case 0xf7:
                        if ((pref & (PreNone | Pre66)) != 0)
                            goto error_operand;

                        break;
                    case 0xd6:
                        if ((pref & (PreF2 | PreF3)) != 0)
                            goto error_operand;

                        break;
                    case 0xc5:
                        goto error_operand;
                }

            goto no_error_operand;

            error_operand:
            hs.Flags |= HdeFlags.Error | HdeFlags.ErrorOperand;

            no_error_operand:
            c = code[p++];

            if (mReg <= 1)
                switch (opcode) {
                    case 0xf6:
                        cflags |= CImm8;
                        break;
                    case 0xf7:
                        cflags |= CImmP66;
                        break;
                }

            switch (mMod) {
                case 0:
                    if ((pref & Pre67) != 0) {
                        if (mRm == 6)
                            dispSize = 2;
                    }
                    else if (mRm == 5) {
                        dispSize = 4;
                    }

                    break;
                case 1:
                    dispSize = 1;
                    break;
                case 2:
                    dispSize = 2;

                    if ((pref & Pre67) == 0)
                        dispSize <<= 1;
                    break;
            }

            if (mMod != 3 && mRm == 4 && (pref & Pre67) == 0) {
                hs.Flags |= HdeFlags.Sib;
                p++;
                hs.Sib = c;
                hs.SibScale = (byte)(c >> 6);
                hs.SibIndex = (byte)((c & 0x3f) >> 3);

                if ((hs.SibBase = (byte)(c & 7)) == 5 && (mMod & 1) == 0)
                    dispSize = 4;
            }

            p--;

            switch (dispSize) {
                case 1:
                    hs.Flags |= HdeFlags.Disp8;
                    hs.Disp.I8 = hde32Table[p];
                    break;
                case 2:
                    hs.Flags |= HdeFlags.Disp16;
                    hs.Disp.I16 = BitConverter.ToUInt16(code.AsSpan()[p..(p + 2)]);
                    break;
                case 4:
                    hs.Flags |= HdeFlags.Disp32;
                    hs.Disp.I32 = BitConverter.ToUInt32(code.AsSpan()[p..(p + 4)]);
                    break;
            }

            p += dispSize;
        }
        else if ((pref & PreLock) != 0) {
            hs.Flags |= HdeFlags.Error | HdeFlags.ErrorLock;
        }

        var rel32Bypass = false;

        if ((cflags & CImmP66) != 0) {
            if ((cflags & CRel32) != 0) {
                if ((pref & Pre66) != 0) {
                    hs.Flags |= HdeFlags.Imm16 | HdeFlags.Relative;
                    hs.Imm.I16 = BitConverter.ToUInt16(code.AsSpan()[p..(p + 2)]);
                    p += 2;
                    goto disasm_done;
                }

                rel32Bypass = true;
                goto rel32_ok;
            }

            if ((pref & Pre66) != 0) {
                hs.Flags |= HdeFlags.Imm16;
                hs.Imm.I16 = BitConverter.ToUInt16(code.AsSpan()[p..(p + 2)]);
                p += 2;
            }
            else {
                hs.Flags |= HdeFlags.Imm32;
                hs.Imm.I32 = BitConverter.ToUInt32(code.AsSpan()[p..(p + 4)]);
                p += 4;
            }
        }

        if ((cflags & CImm16) != 0) {
            if ((hs.Flags & HdeFlags.Imm32) != 0) {
                hs.Flags |= HdeFlags.Imm16;
                hs.Disp.I16 = BitConverter.ToUInt16(code.AsSpan()[p..(p + 2)]);
            }
            else if ((hs.Flags & HdeFlags.Imm16) != 0) {
                hs.Flags |= HdeFlags.Imm16X2;
                hs.Disp.I16 = BitConverter.ToUInt16(code.AsSpan()[p..(p + 2)]);
            }
            else {
                hs.Flags |= HdeFlags.Imm16;
                hs.Imm.I16 = BitConverter.ToUInt16(code.AsSpan()[p..(p + 2)]);
            }
        }

        if ((cflags & CImm8) != 0) {
            hs.Flags |= HdeFlags.Imm8;
            hs.Imm.I8 = code[p++];
        }

        rel32_ok:

        if ((cflags & CRel32) != 0 || rel32Bypass) {
            hs.Flags |= HdeFlags.Imm32 | HdeFlags.Relative;
            hs.Imm.I32 = BitConverter.ToUInt32(code.AsSpan()[p..(p + 4)]);
            p += 4;
        }
        else if ((cflags & CRel8) != 0) {
            hs.Flags |= HdeFlags.Imm8 | HdeFlags.Relative;
            hs.Imm.I8 = code[p++];
        }

        disasm_done:

        hs.Opcode = (ushort)((opcode2 << 8) | opcode);

        if ((hs.Size = p) <= 15)
            return hs.Size;

        hs.Flags |= HdeFlags.Error | HdeFlags.ErrorLength;
        hs.Size = 15;

        return hs.Size;
    }
}

/*
    Hacker Disassembler Engine C#
    Copyright (c) 2022 Nicholas "RealNickk" H.
    All rights reserved.

    Redistribution and use in source and binary forms, with or without
    modification, are permitted provided that the following conditions
    are met:

    1. Redistributions of source code must retain the above copyright
       notice, this list of conditions and the following disclaimer.

    2. Redistributions in binary form must reproduce the above copyright
       notice, this list of conditions and the following disclaimer in
       the documentation and/or other materials provided with the
       distribution.

    3. Neither the name of the copyright holder nor the names of its
       contributors may be used to endorse or promote products derived
       from this software without specific prior written permission.

    THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
    "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
    LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
    A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT 
    HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
    SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT
    LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
    DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
    THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT 
    (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE
    OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
    
    ===========================================================================
    Portions of this software use code written by Vyacheslav Patkov. Vyacheslav 
    Patkov uses the BSD 2-Clause license for his original HDE32 library:
    ===========================================================================

    Hacker Disassembler Engine 64 C
    Copyright (c) 2008-2009, Vyacheslav Patkov.
    All rights reserved.

    Redistribution and use in source and binary forms, with or without
    modification, are permitted provided that the following conditions
    are met:
    
    1. Redistributions of source code must retain the above copyright
    notice, this list of conditions and the following disclaimer.
    2. Redistributions in binary form must reproduce the above copyright
    notice, this list of conditions and the following disclaimer in the
    documentation and/or other materials provided with the distribution.
   
    THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
    "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED
    TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A
    PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE REGENTS OR
    CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
    EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
    PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
    PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
    LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
    NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
    SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */
