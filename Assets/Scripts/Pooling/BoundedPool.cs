using System;
using UnityEngine;

namespace Survivor.Pooling
{
    public abstract class PoolActor : MonoBehaviour
    {
        public bool IsRented { get; internal set; }
        public abstract void ResetForRent();
        public abstract void ResetForReturn();
    }

    // Fixed allocation at battle setup; no runtime growth or silent eviction of active objects.
    public sealed class BoundedPool<T> where T : PoolActor
    {
        public T[] Items { get; }
        public int ActiveCount { get; private set; }
        public BoundedPool(int capacity, Func<T> factory)
        {
            if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity));
            Items = new T[capacity];
            for (int i = 0; i < capacity; i++)
            { Items[i] = factory(); Items[i].IsRented = false; Items[i].gameObject.SetActive(false); }
        }
        public T Rent()
        {
            foreach (var item in Items)
            {
                if (item.IsRented) continue;
                item.ResetForRent(); item.IsRented = true; ActiveCount++; item.gameObject.SetActive(true); return item;
            }
            return null;
        }
        public void Return(T item)
        {
            if (Array.IndexOf(Items, item) < 0 || !item.IsRented) return;
            item.ResetForReturn(); item.IsRented = false; ActiveCount--; item.gameObject.SetActive(false);
        }
    }
}
