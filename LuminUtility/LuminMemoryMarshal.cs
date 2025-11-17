using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LuminUtility;

public static class LuminMemoryMarshal
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref byte GetArrayDataReference<T>(Array? array)
    {
        if (array is null)
            return ref Unsafe.NullRef<byte>();
        
#if NET5_0_OR_GREATER
        return ref MemoryMarshal.GetArrayDataReference(array);
#else
        ref var src = ref Unsafe.As<T, byte>(ref Unsafe.As<byte, T>(ref Unsafe.As<LuminRawArrayData>(array).Data));
        
        return ref Unsafe.Add(ref src, Unsafe.SizeOf<UIntPtr>());
#endif
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref byte GetArrayDataReference<T>(T[]? array)
    {
        return ref GetArrayDataReference<T>((Array) array!);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T GetArrayReference<T>(Array? array)
    {
        if (array is null)
            return ref Unsafe.NullRef<T>();
        
#if NET5_0_OR_GREATER
        return ref Unsafe.As<byte, T>(ref MemoryMarshal.GetArrayDataReference(array));
#else
        ref var src = ref Unsafe.As<T, byte>(ref Unsafe.As<byte, T>(ref Unsafe.As<LuminRawArrayData>(array).Data));
        
        return ref Unsafe.As<byte, T>(ref Unsafe.Add(ref src, Unsafe.SizeOf<UIntPtr>()));
#endif
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T GetArrayReference<T>(T[] array)
    {
        return ref GetArrayReference<T>((Array) array);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Memory<T> AsMemory<T>(ReadOnlyMemory<T> memory)
    {
        return Unsafe.As<ReadOnlyMemory<T>, Memory<T>>(ref memory);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<T> AsSpan<T>(ReadOnlySpan<T> span)
    {
        return MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(span), span.Length);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T GetNonNullPinnableReference<T>(Span<T> span)
    {
        return ref span.Length == 0 
            ? ref Unsafe.NullRef<T>() 
            : ref Unsafe.AsRef(in span.GetPinnableReference());
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T GetNonNullPinnableReference<T>(ReadOnlySpan<T> span)
    {
        return ref span.Length == 0 
            ? ref Unsafe.NullRef<T>() 
            : ref Unsafe.AsRef(in span.GetPinnableReference());
    }
    
    private sealed class LuminRawArrayData
    {
        public uint Length;
        public uint Rank;
        public byte Data;
    }
}