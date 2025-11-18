using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace LuminUtility;

public sealed class LuminCollectionMarshal
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<T?> GetListSpan<T>(List<T?>? list)
    {
        if (list is null)
        {
            return Span<T?>.Empty;
        }

#if NET8_0_OR_GREATER
        CollectionsMarshal.SetCount(list, list.Count);
        return CollectionsMarshal.AsSpan(list);
#else
        SetListSize(list, list.Count);
        ref ListView<T?> local = ref Unsafe.As<List<T?>, ListView<T?>>(ref list);
        return local._items.AsSpan(0, list.Count);
#endif
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<T?> GetListSpan<T>(List<T?>? list, int length)
    {
        if (list is null)
        {
            return Span<T?>.Empty;
        }

        if (list.Capacity < length)
        {
            length = list.Capacity;
        }
        
        
#if NET8_0_OR_GREATER
        CollectionsMarshal.SetCount(list, length);
        return CollectionsMarshal.AsSpan(list).Slice(0, length);
#else
        SetListSize(list, length);
        ref ListView<T?> local = ref Unsafe.As<List<T?>, ListView<T?>>(ref list);
        return local._items.AsSpan(0, length);
#endif
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetListSize<T>(List<T?>? list, int size)
    {
        if (list is null) return;
        
        if (list.Capacity < size)
            list.Capacity = size;
        
#if NET8_0_OR_GREATER
        CollectionsMarshal.SetCount(list, size);
        return;
#else
        ref ListView<T?> local = ref Unsafe.As<List<T?>, ListView<T?>>(ref list);
        local._size = size;
#endif
    }
    
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<T?> GetStackSpan<T>(Stack<T?>? stack)
    {
        if (stack is null)
        {
            return Span<T?>.Empty;
        }
        
        SetStackSize(stack, stack.Count);
        ref var view = ref Unsafe.As<Stack<T?>, StackView<T?>>(ref stack);
        return view._items.AsSpan(0, stack.Count);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<T?> GetStackSpan<T>(Stack<T?>? stack, int length)
    {
        if (stack is null)
        {
            return Span<T?>.Empty;
        }
        
        SetStackSize(stack, length);
        ref var view = ref Unsafe.As<Stack<T?>, StackView<T?>>(ref stack);
        return view._items.AsSpan(0, length);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetStackSize<T>(Stack<T?>? stack, int size)
    {
        if (stack is null) return;
        
        ref var view = ref Unsafe.As<Stack<T?>, StackView<T?>>(ref stack);
        view._size = size;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<T?> GetQueueSpan<T>(Queue<T?>? queue)
    {
        if (queue is null)
        {
            return Span<T?>.Empty;
        }
        
        ref QueueView<T?> local = ref Unsafe.As<Queue<T?>, QueueView<T?>>(ref queue);
        return local._items.AsSpan();
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Span<T?> GetQueueSpan<T>(Queue<T?>? queue, int size)
    {
        if (queue is null)
        {
            return Span<T?>.Empty;
        }
        
        ref QueueView<T?> local = ref Unsafe.As<Queue<T?>, QueueView<T?>>(ref queue);
        return local._items.AsSpan(0, size);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void GetQueueSize<T>(Queue<T?>? queue, out int tail, out int head, out int size)
    {
        if (queue is null)
        {
            tail = 0;
            head = 0;
            size = 0;
            
            return;
        }
        
        ref QueueView<T?> local = ref Unsafe.As<Queue<T?>, QueueView<T?>>(ref queue);

        head = local._head;
        tail = local._tail;
        size = local._size;
        
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void SetQueueSize<T>(Queue<T?>? queue, int tail, int head, int size)
    {
        if (queue is null) return;
        
        ref QueueView<T?> local = ref Unsafe.As<Queue<T?>, QueueView<T?>>(ref queue);

        if (local._items.Length < size)
        {
            local._items = new T[size];
        }
        

        local._head = head;
        local._tail = tail;
        local._size = size;
        
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static DictionaryView<TKey, TValue?> GetDictionaryView<TKey, TValue>(Dictionary<TKey, TValue?>? dict) where TKey : notnull
    {
        return Unsafe.As<Dictionary<TKey, TValue?>, DictionaryView<TKey, TValue?>>(ref dict!);
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static HashSetView<T?> GetHashSetView<T>(HashSet<T?>? set)
    {
        return Unsafe.As<HashSet<T?>, HashSetView<T?>>(ref set!);
    }
    
    internal sealed class ListView<T>
    {
        public T[]? _items;
        public int _size;
        public int _version;
    }
    
    internal sealed class StackView<T>
    {
        public T[]? _items;
#if NETSTANDARD2_1
        private IntPtr _ptr;
#endif
        public int _size;
        public int _version;
    }
    
    internal sealed class QueueView<T>
    {
        public T[]? _items;
#if NETSTANDARD2_1
        private IntPtr _ptr;
#endif
        public int _head;
        public int _tail;
        public int _size;
        public int _version;
        
    }
    
    public sealed class DictionaryView<TKey, TValue>
    {
        public int[] _buckets;
        public Entry[] _entries;
        public int _count;
        public int _version;
        public int _freeList;
        public int _freeCount;
        public IEqualityComparer<TKey> _comparer;
        public Dictionary<TKey, TValue>.KeyCollection _keys;
        public Dictionary<TKey, TValue>.ValueCollection _values;
        public object _syncRoot;
        
        public struct Entry
        {
            public uint HashCode;
            public int Next;
            public TKey Key;
            public TValue Value;
        }
    }
    
    public sealed class HashSetView<T>
    {
        public int[] _buckets;
        public Entry[] _entries;
        public int _count;
        public int _version;
        public int _freeList;
        public int _freeCount;
        public IEqualityComparer<T> _comparer;
        public object _syncRoot;
        
        public struct Entry
        {
            public int HashCode;
            public int Next;
            public T Value;
        }
    }
}

public static class DictionaryExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref TValue GetValueRefOrAddDefault<TKey, TValue>(
        this Dictionary<TKey, TValue> dictionary,
        TKey key,
        out bool exists)
        where TKey : notnull
    {
        if (dictionary == null)
            throw new ArgumentNullException(nameof(dictionary));
        
        return ref GetValueRefOrAddDefaultFallback(dictionary, key, out exists);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ref TValue GetValueRefOrAddDefaultFallback<TKey, TValue>(
        Dictionary<TKey, TValue> dictionary,
        TKey key,
        out bool exists)
        where TKey : notnull
    {
        var view = LuminCollectionMarshal.GetDictionaryView(dictionary);
        
        if (view._buckets == null)
        {
            InitializeDictionary(dictionary, 0);
            view = LuminCollectionMarshal.GetDictionaryView(dictionary);
        }
        
        var comparer = view._comparer;
        var entries = view._entries;
        
        uint hashCode = !typeof(TKey).IsValueType || comparer != null 
            ? (uint)comparer.GetHashCode(key) 
            : (uint)key.GetHashCode();
        
        uint collisionCount = 0;
        
        ref int bucket = ref GetBucketRef(view, hashCode);
        int index = bucket - 1;
        
        if (typeof(TKey).IsValueType && comparer == null)
        {
            while ((uint)index < (uint)entries.Length)
            {
                ref var entry = ref entries[index];
                if (entry.HashCode == hashCode && 
                    EqualityComparer<TKey>.Default.Equals(entry.Key, key))
                {
                    exists = true;
                    return ref entry.Value;
                }
                index = entry.Next;
                collisionCount++;
                
                if (collisionCount > (uint)entries.Length)
                {
                    throw new InvalidOperationException("Concurrent operations are not supported.");
                }
            }
        }
        else
        {
            while ((uint)index < (uint)entries.Length)
            {
                ref var entry = ref entries[index];
                if (entry.HashCode == hashCode && 
                    comparer.Equals(entry.Key, key))
                {
                    exists = true;
                    return ref entry.Value;
                }
                index = entry.Next;
                collisionCount++;
                
                if (collisionCount > (uint)entries.Length)
                {
                    throw new InvalidOperationException("Concurrent operations are not supported.");
                }
            }
        }
        
        int newIndex;
        if (view._freeCount > 0)
        {
            newIndex = view._freeList;
            view._freeList = -3 - entries[view._freeList].Next;
            view._freeCount--;
        }
        else
        {
            int count = view._count;
            if (count == entries.Length)
            {
                ResizeDictionary(dictionary);
                view = LuminCollectionMarshal.GetDictionaryView(dictionary);
                entries = view._entries;
                bucket = ref GetBucketRef(view, hashCode);
            }
            newIndex = count;
            view._count = count + 1;
        }
        
        ref var newEntry = ref entries[newIndex];
        newEntry.HashCode = hashCode;
        newEntry.Next = bucket - 1;
        newEntry.Key = key;
        newEntry.Value = default!;
        bucket = newIndex + 1;
        view._version++;
        
        if (!typeof(TKey).IsValueType && collisionCount > 100)
        {
            ResizeDictionary(dictionary, entries.Length, true);
            view = LuminCollectionMarshal.GetDictionaryView(dictionary);
            exists = false;
            return ref FindValue(dictionary, key);
        }

        exists = false;
        return ref newEntry.Value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ref int GetBucketRef<TKey, TValue>(
        LuminCollectionMarshal.DictionaryView<TKey, TValue> view, 
        uint hashCode)
        where TKey : notnull
    {
        uint bucket = hashCode % (uint)view._buckets.Length;
        return ref view._buckets[bucket];
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ref TValue FindValue<TKey, TValue>(Dictionary<TKey, TValue> dictionary, TKey key)
        where TKey : notnull
    {
        var view = LuminCollectionMarshal.GetDictionaryView(dictionary);
        var entries = view._entries;
        var comparer = view._comparer;
        uint hashCode = !typeof(TKey).IsValueType || comparer != null 
            ? (uint)comparer.GetHashCode(key) 
            : (uint)key.GetHashCode();
        
        ref int bucket = ref GetBucketRef(view, hashCode);
        int index = bucket - 1;

        while ((uint)index < (uint)entries.Length)
        {
            ref var entry = ref entries[index];
            if (entry.HashCode == hashCode && 
                comparer.Equals(entry.Key, key))
            {
                return ref entry.Value;
            }
            index = entry.Next;
        }

        throw new KeyNotFoundException();
    }

    private static void InitializeDictionary<TKey, TValue>(Dictionary<TKey, TValue> dictionary, int capacity)
        where TKey : notnull
    {
        if (dictionary.Count == 0)
        {
            dictionary.EnsureCapacity(Math.Max(capacity, 4));
        }
    }

    private static void ResizeDictionary<TKey, TValue>(Dictionary<TKey, TValue> dictionary, int newSize = 0, bool forceNewHashCodes = false)
        where TKey : notnull
    {
        int newCapacity = newSize == 0 ? HashHelpers.ExpandPrime(dictionary.Count) : newSize;
        dictionary.EnsureCapacity(newCapacity);
        
        if (forceNewHashCodes)
        {
            var view = LuminCollectionMarshal.GetDictionaryView(dictionary);
            var entries = view._entries;
            var comparer = view._comparer;
            
            for (int i = 0; i < view._count; i++)
            {
                if (entries[i].Next >= -1)
                {
                    entries[i].HashCode = (uint)comparer.GetHashCode(entries[i].Key);
                }
            }
            
            Array.Clear(view._buckets, 0,  view._buckets.Length);
            for (int i = 0; i < view._count; i++)
            {
                if (entries[i].Next >= -1)
                {
                    ref int bucket = ref GetBucketRef(view, entries[i].HashCode);
                    entries[i].Next = bucket - 1;
                    bucket = i + 1;
                }
            }
        }
    }
}

internal static class HashHelpers
{
    public static int ExpandPrime(int oldSize)
    {
        int newSize = 2 * oldSize;
        if ((uint)newSize > 0x7FEFFFFD)
        {
            return 0x7FEFFFFD;
        }
        return GetPrime(newSize);
    }
    
    static int[] primes = {
        3, 7, 11, 17, 23, 29, 37, 47, 59, 71, 89, 107, 131, 163, 197, 239, 293, 353, 431, 521, 631, 761, 919,
        1103, 1327, 1597, 1931, 2333, 2801, 3371, 4049, 4861, 5839, 7013, 8419, 10103, 12143, 14591,
        17519, 21023, 25229, 30293, 36353, 43627, 52361, 62851, 75431, 90523, 108631, 130363, 156437,
        187751, 225307, 270371, 324449, 389357, 467237, 560689, 672827, 807403, 968897, 1162687, 1395263,
        1674319, 2009191, 2411033, 2893249, 3471899, 4166287, 4999559, 5999471, 7199369
    };
    
    public static int GetPrime(int min)
    {
        foreach (ref int prime in primes.AsSpan())
        {
            if (prime >= min)
                return prime;
        }
        
        return min;
    }
}