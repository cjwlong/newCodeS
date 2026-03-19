using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServiceManager
{
    public sealed class GlobalCollectionService<T>
    {
        private static readonly Lazy<GlobalCollectionService<T>> _instance =
            new Lazy<GlobalCollectionService<T>>(() => new GlobalCollectionService<T>());

        private readonly ConcurrentDictionary<int, ConcurrentBag<T>> _collections =
            new ConcurrentDictionary<int, ConcurrentBag<T>>();

        public static GlobalCollectionService<T> Instance => _instance.Value;

        private GlobalCollectionService() { }

        public int CreateNewCollection()
        {
            var id = 0;
            _collections.TryAdd(id, new ConcurrentBag<T>());
            return id;
        }

        public bool Insert(int collectionId, T item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));

            var bag = _collections.GetOrAdd(collectionId, _ => new ConcurrentBag<T>());

            lock (bag)  // 确保同一集合的添加操作有序
            {
                bag.Add(item);
                return true;
            }
        }

        public bool Contains(int collectionId, T item)
        {
            return _collections.TryGetValue(collectionId, out var bag) &&
                   System.Linq.Enumerable.Contains(bag, item);
        }

        public bool Remove(int collectionId, T item)
        {
         
            if (!_collections.TryGetValue(collectionId, out var dict))
                return false;

            return _collections.TryRemove(collectionId, out _);
        }

     
        public IEnumerable<T> GetAllItems(int collectionId)
        {
            return _collections.TryGetValue(collectionId, out var bag) ?
                bag : Enumerable.Empty<T>();
        }

        public int GetCollectionCount()
        {
            return _collections.Values.Sum(bag => bag.Count);
        }
    }

}
