using Microsoft.Extensions.Caching.Distributed;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Microsoft.Extensions.Caching.Redis
{
    public interface IServiceStackRedisCache : IDistributedCache
    {
        void Delete<T>(T item);
        void DeleteAll<T>(T item);
        T Get<T>(string id);
        IQueryable<T> GetAll<T>();
        IQueryable<T> GetAll<T>(string hash, string value);
        IQueryable<T> GetAll<T>(string hash, string value, Expression<Func<T, bool>> filter);
        long PublishMessage(string channel, object item);
        void Set<T>(T item);
        void Set<T>(T item, List<string> hash, List<string> value, string keyName);
        void Set<T>(T item, string hash, string value, string keyName);
        void SetAll<T>(List<T> listItems);
        void SetAll<T>(List<T> list, List<string> hash, List<string> value, string keyName);
        void SetAll<T>(List<T> list, string hash, string value, string keyName);
    }
}