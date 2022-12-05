using System;
using System.IO;
using System.Runtime.InteropServices;

namespace HackerFramework.Streams;

/// <summary>
///     Reads binary data from a stream.
/// </summary>
internal class MemoryReader : BinaryReader {
    public MemoryReader(byte[] buffer) : base(new MemoryStream(buffer)) { }

    public MemoryReader(Stream stream) : base(stream) { }

    /// <summary>
    ///     <para>Reads a structure from the stream.</para>
    ///     <para>
    ///         This function is internalized to ensure that some retard
    ///         won't use this without understanding what they're doing.
    ///     </para>
    ///     <strong>NOTE:</strong> USE WITH CAUTION. ENSURE REFERENCE TYPES
    ///     ARE SPECIFICALLY MARKED WITH A <see cref="MarshalAsAttribute" />
    ///     TO ENSURE A CONSTANT SIZE. FAILURE TO DO SO CAN RESULT IN MEMORY
    ///     CORRUPTION.
    /// </summary>
    /// <typeparam name="T">An unmanaged structure.</typeparam>
    // Checking that reference types have a MarshalAsAtrribute uses reflection and makes the code slower, so I won't check it.
    // You can't use an unmanaged type constraint here because the marshaller can marshal reference types like strings.
    internal unsafe T ReadStructFast<T>() where T : struct {
        var size = Marshal.SizeOf<T>();
        var buffer = ReadBytes(size);

        fixed (byte* arrPtr = buffer) {
            return Marshal.PtrToStructure<T>((IntPtr)arrPtr)!;
        }
    }
}