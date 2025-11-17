using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LuminUtility;

using EncodedPtr = nuint;

public static unsafe class LuminBitOperations
{
    static LuminBitOperations()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            MemCopy = &MemCopyWindows;
        }
        else
        {
            try
            {
                byte a = 0, b = 0;
                MemCopyGlibc(&a, &b, 1);
                MemCopy = &MemCopyGlibc;
            }
            catch (DllNotFoundException)
            {
                try
                {
                    byte a = 0, b = 0;
                    MemCopyMusl(&a, &b, 1);
                    MemCopy = &MemCopyMusl;
                }
                catch (DllNotFoundException)
                {
                    // Fallback to Unsafe.CopyBlock
                    delegate*<void*, void*, uint, void> temp = &Unsafe.CopyBlock;
                    MemCopy = (delegate*<void*, void*, nuint, void>)temp;
                }
            }
        }
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static char ToUpper(char c) 
    {
        if (c is >= 'a' and <= 'z') return (char)(c - 'a' + 'A');
        else return c;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int StringCompareNoCase(char* s, char* t, nint n)
    {
        if (n == 0) return 0;
    
        char* sPtr = s;
        char* tPtr = t;
    
        for (; *sPtr != '\0' && *tPtr != '\0' && n > 0; n--)
        {
            if (ToUpper(*sPtr) != ToUpper(*tPtr)) break;
        
            sPtr = (char*)Unsafe.Add<char>(sPtr, 1);
            tPtr = (char*)Unsafe.Add<char>(tPtr, 1);
        }
    
        return (n == 0 ? 0 : *sPtr - *tPtr);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void StringCopy(char* dest, char* src, nint destSize) {
        if (dest == null || src == null || destSize == 0) return;
        while (*src != 0 && destSize > 1) {
            *dest++ = *src++;
            destSize--;
        }
        *dest = '\0';
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void StringConcatenate(char* dest, char* src, nint destSize) {
        if (dest == null || src == null || destSize == 0) return;
        while (*dest != 0 && destSize > 1) {
            dest++;
            destSize--;
        }
        StringCopy(dest, src, destSize);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static nint StringLength(char* s) {
        if (s == null) return 0;
        nint len = 0;
        while (s[len] != 0) { len++; }
        return len;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static nint StringLengthSafe(char* s, nint maxLen) {
        if (s == null) return 0;
        nint len = 0;
        while (s[len] != 0 && len < maxLen) { len++; }
        return len;
    }
    
    public static bool GetEnvironmentVariable(char* name, char* result, nint resultSize) {
        if (name == null || result == null || resultSize < 64) return false;
        return PrimitiveGetEnvironmentVariable(name, result, resultSize);
    }

    private static void OutputChar(char c, char** output, char* end) {
        char* p = *output;
        if (p >= end) return;
        *p = c;
        *output = p + 1;
    }

    private static void OutputString(char* s, char** output, char* end) {
        if (s == null) return;
        char* p = *output;
        while (*s != 0 && p < end) {
            *p++ = *s++;
        }
        *output = p;
    }

    private static void OutputFill(char fill, nint len, char** output, char* end) {
        char* p = *output;
        for (nint i = 0; i < len && p < end; i++) {
            *p++ = fill;
        }
        *output = p;
    }

    private static void OutputAlignRight(char fill, char* start, nint len, nint extra, char* end) {
        if (len == 0 || extra == 0) return;
        if (start + len + extra >= end) return;
        for (nint i = 1; i <= len; i++) {
            start[len + extra - i] = start[len - i];
        }
        for (nint i = 0; i < extra; i++) {
            start[i] = fill;
        }
    }

    private static void OutputNumber(UIntPtr x, nint baseVal, char prefix, char** output, char* end) {
        if (x == UIntPtr.Zero || baseVal == 0 || baseVal > 16) {
            if (prefix != 0) { OutputChar(prefix, output, end); }
            OutputChar('0', output, end);
        }
        else {
            char* start = *output;
            UIntPtr num = x;
            while (num != UIntPtr.Zero) {
                char digit = (char)(num.ToUInt64() % (ulong)baseVal);
                OutputChar((digit <= 9 ? (char)('0' + digit) : (char)('A' + digit - 10)), output, end);
                num = new UIntPtr(num.ToUInt64() / (ulong)baseVal);
            }
            if (prefix != 0) {
                OutputChar(prefix, output, end);
            }
            nint len = (nint)(*output - start);
            for (nint i = 0; i < (len / 2); i++) {
                (start[len - i - 1], start[i]) = (start[i], start[len - i - 1]);
            }
        }
    }

    public static int FormatString(char* buf, nint bufSize, char* fmt, IntPtr args) {
        if (buf == null || bufSize == 0 || fmt == null) return 0;
        buf[bufSize - 1] = '\0';
        char* end = buf + (bufSize - 1);
        char* input = fmt;
        char* output = buf;
        
        while (true) {
            if (output >= end) break;
            char c;
            
            c = *input; if (c == 0) break; input++;

            if (c != '%') {
                if ((c >= ' ' && c <= '~') || c == '\n' || c == '\r' || c == '\t') {
                    OutputChar(c, &output, end);
                }
            }
            else {
                c = *input; if (c == 0) break; input++;

                char fill = ' ';
                nint width = 0;
                char numType = 'd';
                char numPlus = '\0';
                bool alignRight = true;
                
                if (c == '+' || c == ' ') { numPlus = c; c = *input; if (c == 0) break; input++; }
                if (c == '-') { alignRight = false; c = *input; if (c == 0) break; input++; }
                if (c == '0') { fill = '0'; c = *input; if (c == 0) break; input++; }
                
                if (c >= '1' && c <= '9') {
                    width = (c - '0'); c = *input; if (c == 0) break; input++;
                    while (c >= '0' && c <= '9') {
                        width = (10 * width) + (c - '0'); c = *input; if (c == 0) break; input++;
                    }
                    if (c == 0) break;
                }
                
                if (c is 'z' or 't' or 'L') { numType = c; c = *input; if (c == 0) break; input++; }
                else if (c == 'l') {
                    numType = c; c = *input; if (c == 0) break; input++;
                    if (c == 'l') { numType = 'L'; c = *input; if (c == 0) break; input++; }
                }

                char* start = output;
                if (c == 's') {
                    char* s = (char*)Marshal.ReadIntPtr(args);
                    args += IntPtr.Size;
                    OutputString(s, &output, end);
                }
                else if (c == 'p' || c == 'x' || c == 'u') {
                    UIntPtr x = UIntPtr.Zero;
                    if (c == 'x' || c == 'u') {
                        if (numType == 'z') x = new UIntPtr(Marshal.ReadIntPtr(args).ToPointer());
                        else if (numType == 't') x = new UIntPtr(Marshal.ReadIntPtr(args).ToPointer());
                        else if (numType == 'L') x = (UIntPtr)Marshal.ReadInt64(args);
                        else if (numType == 'l') x = new UIntPtr(Marshal.ReadIntPtr(args).ToPointer());
                        else x = (UIntPtr)Marshal.ReadInt32(args);
                        args += IntPtr.Size;
                    }
                    else if (c == 'p') {
                        x = new UIntPtr(Marshal.ReadIntPtr(args).ToPointer());
                        args += IntPtr.Size;
                        var span = "0X".AsSpan();
                        OutputString((char*)Unsafe.AsPointer(ref MemoryMarshal.GetReference(span)), &output, end);
                        start = output;
                        width = (width >= 2 ? width - 2 : 0);
                    }
                    
                    if (width == 0 && (c == 'x' || c == 'p')) {
                        if (c == 'p') { width = 2 * (x.ToUInt64() <= uint.MaxValue ? 4 : ((x.ToUInt64() >> 16) <= uint.MaxValue ? 6 : sizeof(IntPtr))); }
                        if (width == 0) { width = 2; }
                        fill = '0';
                    }
                    OutputNumber(x, (c is 'x' or 'p' ? 16 : 10), numPlus, &output, end);
                }
                else if (c is 'i' or 'd') {
                    IntPtr x = IntPtr.Zero;
                    if (numType == 'z') x = (IntPtr)Marshal.ReadIntPtr(args);
                    else if (numType == 't') x = (IntPtr)Marshal.ReadIntPtr(args);
                    else if (numType == 'L') x = (IntPtr)Marshal.ReadInt64(args);
                    else if (numType == 'l') x = (IntPtr)Marshal.ReadIntPtr(args);
                    else x = (IntPtr)Marshal.ReadInt32(args);
                    args += IntPtr.Size;
                    
                    char pre = '\0';
                    if (x.ToInt64() < 0) {
                        pre = '-';
                        if (x.ToInt64() > long.MinValue) { x = (IntPtr)(-x.ToInt64()); }
                    }
                    else if (numPlus != '\0') {
                        pre = numPlus;
                    }
                    OutputNumber((UIntPtr)(ulong)Math.Abs(x.ToInt64()), 10, pre, &output, end);
                }
                else if (c is >= ' ' and <= '~') {
                    OutputChar('%', &output, end);
                    OutputChar(c, &output, end);
                }

                nint len = (nint)(output - start);
                if (len < width) {
                    OutputFill(fill, width - len, &output, end);
                    if (alignRight && output <= end) {
                        OutputAlignRight(fill, start, len, width - len, end);
                    }
                }
            }
        }
        
        *output = '\0';
        return (int)(output - buf);
    }

    public static int FormatStringWithArgs(char* buf, nint bufLen, char* fmt, __arglist) {
        IntPtr args = default;
        try {
            args = Marshal.GetFunctionPointerForDelegate((Action<IntPtr>)((argPtr) => {
                // va_list handling would go here
            }));
            return FormatString(buf, bufLen, fmt, args);
        }
        finally {
            // Cleanup if needed
        }
    }

    // Bit counting implementations
    private const uint MaskEvenBits32 = 0x55555555;
    private const uint MaskEvenPairs32 = 0x33333333;
    private const uint MaskEvenNibbles32 = 0x0F0F0F0F;

    private static nint ByteSum32(uint x) {
        x += (x << 8);
        x += (x << 16);
        return (nint)(x >> 24);
    }

    private static nint PopCountGeneric32(uint x) {
        x = x - ((x >> 1) & MaskEvenBits32);
        x = (x & MaskEvenPairs32) + ((x >> 2) & MaskEvenPairs32);
        x = (x + (x >> 4)) & MaskEvenNibbles32;
        return ByteSum32(x);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static nint PopCount32(nint x) {
        return PopCountGeneric32((uint)x);
    }
    
    private const ulong MaskEvenBits64 = 0x5555555555555555;
    private const ulong MaskEvenPairs64 = 0x3333333333333333;
    private const ulong MaskEvenNibbles64 = 0x0F0F0F0F0F0F0F0F;

    private static nint ByteSum64(ulong x) {
        x += (x << 8);
        x += (x << 16);
        x += (x << 32);
        return (nint)(x >> 56);
    }

    private static nint PopCountGeneric64(ulong x) {
        x = x - ((x >> 1) & MaskEvenBits64);
        x = (x & MaskEvenPairs64) + ((x >> 2) & MaskEvenPairs64);
        x = (x + (x >> 4)) & MaskEvenNibbles64;
        return ByteSum64(x);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static nint PopCount64(nint x) {
        return PopCountGeneric64((ulong)x);
    }

    private static bool PrimitiveGetEnvironmentVariable(char* name, char* result, nint resultSize) {
        // Platform-specific implementation would go here
        return false;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPowerOfTwo(nuint x) {
        return (x & (x - 1)) == 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAligned(void* p, nuint alignment) {
        Debug.Assert(alignment != 0);
        return (((nuint)p % alignment) == 0);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static nuint AlignUp(nuint size, nuint alignment) {
        Debug.Assert(alignment != 0);
        nuint mask = alignment - 1;
        if ((alignment & mask) == 0) {
            return ((size + mask) & ~mask);
        }
        else {
            return (((size + mask)/alignment)*alignment);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static nuint AlignDown(nuint size, nuint alignment) {
        Debug.Assert(alignment != 0);
        nuint mask = alignment - 1;
        if ((alignment & mask) == 0) {
            return (size & ~mask);
        }
        else {
            return ((size / alignment) * alignment);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void* AlignUpPointer(void* p, nuint alignment) {
        return (void*)AlignUp((nuint)p, alignment);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void* AlignDownPointer(void* p, nuint alignment) {
        return (void*)AlignDown((nuint)p, alignment);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static nuint DivideUp(nuint size, nuint divider) {
        Debug.Assert(divider != 0);
        return (divider == 0 ? size : ((size + divider - 1) / divider));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static nint Clamp(nint value, nint min, nint max) {
        if (value < min) return min;
        else if (value > max) return max;
        else return value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsMemoryZero(in void* p, nint size) {
        for (nint i = 0; i < size; i++) {
            if (((byte*)p)[i] != 0) return false;
        }
        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static nuint WordSizeFromByteSize(nuint size) {
        Debug.Assert(size <= ulong.MaxValue - (uint)sizeof(nuint));
        return (size + (uint)sizeof(nuint) - 1) / (uint)sizeof(nuint);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void* AllocateAlignedMemory(nuint size, nuint alignment)
    {
        Debug.Assert(IsPowerOfTwo(alignment));
        Debug.Assert(size != 0);
#if NET6_0_OR_GREATER
        return NativeMemory.AlignedAlloc(size, alignment);
#else
        nint align = Unsafe.As<nuint, nint>(ref alignment);
        nint byteCount = Unsafe.As<nuint, nint>(ref size);
        nint extra = align - 1 + sizeof(IntPtr);
        IntPtr rawPtr = Marshal.AllocHGlobal(byteCount + extra);
        
        nint aligned = (rawPtr + extra) & ~(align - 1);
        
        *(IntPtr*)(aligned - sizeof(IntPtr)) = rawPtr;

        return (void*)aligned;
#endif
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void FreeAlignedMemory(void* ptr)
    {
        Debug.Assert(ptr != null);
#if NET6_0_OR_GREATER
        NativeMemory.AlignedFree(ptr);
#else
        IntPtr origin = *(IntPtr*)((byte*)ptr - sizeof(IntPtr));
        Marshal.FreeHGlobal(origin);
#endif
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static EncodedPtr EncodePointer(EncodedPtr key1, EncodedPtr key2, void* ptr)
    {
        if (ptr == null) 
            return UIntPtr.Zero;

        ulong address = (ulong)ptr;
        
        ulong encoded = address ^ key1;
        encoded ^= key2;
        
        return (EncodedPtr)encoded;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void* DecodePointer(EncodedPtr key1, EncodedPtr key2, void* encoded)
    {
        ulong value = (ulong)encoded;
        
        ulong decoded = value ^ key1;
        decoded ^= key2;
        
        return (void*)decoded;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static nuint AlignmentOf<T>() where T : unmanaged => 
        (nuint)sizeof(AlignmentHelper<T>) - (nuint)sizeof(T);
    
    [DllImport("msvcrt.dll", EntryPoint = "memcpy", CallingConvention = CallingConvention.Cdecl)]
    private static extern unsafe void MemCopyWindows(void* dest, void* src, nuint count);
    
    [DllImport("libc.so.6", EntryPoint = "memcpy", CallingConvention = CallingConvention.Cdecl)]
    private static extern unsafe void MemCopyGlibc(void* dest, void* src, nuint count);
    
    [DllImport("libc.so", EntryPoint = "memcpy", CallingConvention = CallingConvention.Cdecl)]
    private static extern unsafe void MemCopyMusl(void* dest, void* src, nuint count);

    public static delegate* <void*, void*, nuint, void> MemCopy;
    
    [StructLayout(LayoutKind.Sequential)]
    private struct AlignmentHelper<T> where T : unmanaged
    {
        private byte _dummy;
        private T _data;
    }
}