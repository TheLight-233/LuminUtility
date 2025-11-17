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
        public ulong _fastModMultiplier;
        public int _count;
        public int _version;
        public int _freeList;
        public int _freeCount;
        public IEqualityComparer<TKey> _comparer;
        public Dictionary<TKey, TValue>.KeyCollection _keys;
        public Dictionary<TKey, TValue>.ValueCollection _values;
        
        public struct Entry
        {
            public int HashCode;
            public int Next;
            public TKey Key;
            public TValue Value;
        }
    }
    
    public sealed class HashSetView<T>
    {
        public int[] _buckets;
        public Entry[] _entries;
        private ulong _fastModMultiplier;
        public int _count;
        public int _version;
        public int _freeList;
        public int _freeCount;
        public IEqualityComparer<T> _comparer;
        
        
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
    public static ref TValue GetValueRefOrAddDefault<TKey, TValue, TAlternateKey>(
        this Dictionary<TKey, TValue> dictionary,
        TAlternateKey key,
        out bool exists,
        IAlternateEqualityComparer<TAlternateKey, TKey> alternateComparer)
        where TKey : notnull
    {
        if (dictionary == null)
            throw new ArgumentNullException(nameof(dictionary));
        if (alternateComparer == null)
            throw new ArgumentNullException(nameof(alternateComparer));

        var view = LuminCollectionMarshal.GetDictionaryView(dictionary);
        
        if (view._buckets == null)
        {
            InitializeDictionary(dictionary, 0);
            view = LuminCollectionMarshal.GetDictionaryView(dictionary); // 重新获取视图
        }

        var entries = view._entries;
        uint hashCode = (uint)alternateComparer.GetHashCode(key);
        uint collisionCount = 0;
        
        ref int bucket = ref GetBucketRef(view, hashCode);
        int index = bucket - 1;
        
        while ((uint)index < (uint)entries.Length)
        {
            if (entries[index].HashCode == hashCode && 
                alternateComparer.Equals(key, entries[index].Key))
            {
                exists = true;
                return ref entries[index].Value;
            }
            index = entries[index].Next;
            collisionCount++;
            
            if (collisionCount > (uint)entries.Length)
            {
                throw new InvalidOperationException("Concurrent operations are not supported.");
            }
        }
        
        TKey newKey = alternateComparer.Create(key);
        if (newKey == null)
        {
            throw new ArgumentNullException("key");
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
        
        ref var entry = ref entries[newIndex];
        entry.HashCode = (int)hashCode;
        entry.Next = bucket - 1;
        entry.Key = newKey;
        entry.Value = default!;
        bucket = newIndex + 1;
        view._version++;
        
        if (!typeof(TKey).IsValueType && collisionCount > 100 && 
            alternateComparer is NonRandomizedStringEqualityComparer)
        {
            ResizeDictionary(dictionary, entries.Length, true);
            view = LuminCollectionMarshal.GetDictionaryView(dictionary);
            exists = false;
            return ref FindValue(dictionary, newKey);
        }

        exists = false;
        return ref entry.Value;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ref int GetBucketRef<TKey, TValue>(
        LuminCollectionMarshal.DictionaryView<TKey, TValue> view, 
        uint hashCode)
        where TKey : notnull
    {
        if (view._fastModMultiplier != 0)
        {
            uint bucketCount = (uint)view._buckets.Length;
            uint bucket = (uint)((hashCode * view._fastModMultiplier) >> 32) & (bucketCount - 1);
            return ref view._buckets[bucket];
        }
        else
        {
            uint bucket = hashCode % (uint)view._buckets.Length;
            return ref view._buckets[bucket];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ref TValue FindValue<TKey, TValue>(Dictionary<TKey, TValue> dictionary, TKey key)
        where TKey : notnull
    {
        var view = LuminCollectionMarshal.GetDictionaryView(dictionary);
        var entries = view._entries;
        var comparer = view._comparer;
        uint hashCode = (uint)comparer.GetHashCode(key);
        
        ref int bucket = ref GetBucketRef(view, hashCode);
        int index = bucket - 1;

        while ((uint)index < (uint)entries.Length)
        {
            if (entries[index].HashCode == hashCode && 
                comparer.Equals(entries[index].Key, key))
            {
                return ref entries[index].Value;
            }
            index = entries[index].Next;
        }

        throw new KeyNotFoundException();
    }

    private static void InitializeDictionary<TKey, TValue>(Dictionary<TKey, TValue> dictionary, int capacity)
        where TKey : notnull
    {
        // 这里需要调用字典的初始化方法
        // 由于是内部方法，我们可以通过添加然后立即删除一个虚拟元素来触发初始化
        if (dictionary.Count == 0)
        {
            try
            {
                // 尝试添加一个虚拟键值对来触发初始化
                if (typeof(TKey) == typeof(string))
                {
                    object dummyKey = "__dummy_init_key__";
                    dictionary.Add((TKey)dummyKey, default!);
                    dictionary.Remove((TKey)dummyKey);
                }
                else if (typeof(TKey).IsValueType)
                {
                    TKey dummyKey = default!;
                    dictionary.Add(dummyKey, default!);
                    dictionary.Remove(dummyKey);
                }
            }
            catch
            {
                // 如果虚拟键添加失败，回退到设置容量
                dictionary.EnsureCapacity(Math.Max(capacity, 4));
            }
        }
    }

    private static void ResizeDictionary<TKey, TValue>(Dictionary<TKey, TValue> dictionary, int newSize = 0, bool forceNewHashCodes = false)
        where TKey : notnull
    {
        // 通过公共API触发调整大小
        int newCapacity = newSize == 0 ? dictionary.Count * 2 : newSize;
        dictionary.EnsureCapacity(newCapacity);
        
        // 如果需要强制新哈希码（字符串随机化），我们需要重新计算所有条目的哈希码
        if (forceNewHashCodes)
        {
            var view = LuminCollectionMarshal.GetDictionaryView(dictionary);
            var entries = view._entries;
            var comparer = view._comparer;
            
            for (int i = 0; i < view._count; i++)
            {
                if (entries[i].Next >= -1)
                {
                    entries[i].HashCode = comparer.GetHashCode(entries[i].Key);
                }
            }
        }
    }
}

public interface IAlternateEqualityComparer<TAlternateKey, TKey>
{
    int GetHashCode(TAlternateKey key);
    bool Equals(TAlternateKey alternateKey, TKey dictionaryKey);
    TKey Create(TAlternateKey alternateKey);
}

public class NonRandomizedStringEqualityComparer : IAlternateEqualityComparer<string, string>
{
    public int GetHashCode(string key) => key?.GetHashCode() ?? 0;
    
    public bool Equals(string alternateKey, string dictionaryKey) 
        => string.Equals(alternateKey, dictionaryKey, StringComparison.Ordinal);
    
    public string Create(string alternateKey) => alternateKey;
}