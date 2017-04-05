using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using ServiceStack.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Extensions.Caching.Redis
{
    public class ServiceStackRedisCache : IServiceStackRedisCache
    {
        private readonly IRedisClientsManager _redisManager;
        private readonly ServiceStackRedisCacheOptions _options;

        public ServiceStackRedisCache(IOptions<ServiceStackRedisCacheOptions> optionsAccessor)
        {
            if (optionsAccessor == null)
            {
                throw new ArgumentNullException(nameof(optionsAccessor));
            }

            _options = optionsAccessor.Value;

            var host = $"{_options.Password}@{_options.Host}:{_options.Port}";
            RedisConfig.VerifyMasterConnections = false;
            _redisManager = new RedisManagerPool(host);
        }
        #region Base

        public byte[] Get(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            using (var client = _redisManager.GetClient() as IRedisNativeClient)
            {
                if (client.Exists(key) == 1)
                {
                    return client.Get(key);
                }
            }
            return null;
        }

        public async Task<byte[]> GetAsync(string key)
        {
            return Get(key);
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            if (options == null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            using (var client = _redisManager.GetClient() as IRedisNativeClient)
            {
                var expireInSeconds = GetExpireInSeconds(options);
                if (expireInSeconds > 0)
                {
                    client.SetEx(key, expireInSeconds, value);
                    client.SetEx(GetExpirationKey(key), expireInSeconds, Encoding.UTF8.GetBytes(expireInSeconds.ToString()));
                }
                else
                {
                    client.Set(key, value);
                }
            }
        }

        public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options)
        {
            Set(key, value, options);
        }

        public void Refresh(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            using (var client = _redisManager.GetClient() as IRedisNativeClient)
            {
                if (client.Exists(key) == 1)
                {
                    var value = client.Get(key);
                    if (value != null)
                    {
                        var expirationValue = client.Get(GetExpirationKey(key));
                        if (expirationValue != null)
                        {
                            client.Expire(key, int.Parse(Encoding.UTF8.GetString(expirationValue)));
                        }
                    }
                }
            }
        }

        public async Task RefreshAsync(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            Refresh(key);
        }

        public void Remove(string key)
        {
            if (key == null)
            {
                throw new ArgumentNullException(nameof(key));
            }

            using (var client = _redisManager.GetClient() as IRedisNativeClient)
            {
                client.Del(key);
            }
        }

        public async Task RemoveAsync(string key)
        {
            Remove(key);
        }

        private int GetExpireInSeconds(DistributedCacheEntryOptions options)
        {
            if (options.SlidingExpiration.HasValue)
            {
                return (int)options.SlidingExpiration.Value.TotalSeconds;
            }
            else if (options.AbsoluteExpiration.HasValue)
            {
                return (int)options.AbsoluteExpirationRelativeToNow.Value.TotalSeconds;
            }
            else
            {
                return 0;
            }
        }

        private string GetExpirationKey(string key)
        {
            return key + $"-{nameof(DistributedCacheEntryOptions)}";
        }
        #endregion
        #region data
        public T Get<T>(string id)
        {
            using (var redisClient = _redisManager.GetClient())
            {
                var redis = redisClient.As<T>();
                return redis.GetById(id.ToLower());
            }
        }

        public IQueryable<T> GetAll<T>()
        {
            using (var redisClient = _redisManager.GetClient())
            {
                var redis = redisClient.As<T>();
                return redis.GetAll().AsQueryable();
            }
        }

        public IQueryable<T> GetAll<T>(string hash, string value, Expression<Func<T, bool>> filter)
        {
            var filtered = _redisManager.GetClient().GetAllEntriesFromHash(hash).Where(c => c.Value.Equals(value, StringComparison.CurrentCultureIgnoreCase));
            var ids = filtered.Select(c => c.Key);

            var ret = _redisManager.GetClient().As<T>().GetByIds(ids).AsQueryable()
                                .Where(filter);

            return ret;
        }

        public IQueryable<T> GetAll<T>(string hash, string value)
        {
            var filtered = _redisManager.GetClient().GetAllEntriesFromHash(hash).Where(c => c.Value.Equals(value, StringComparison.CurrentCultureIgnoreCase));
            var ids = filtered.Select(c => c.Key);

            var ret = _redisManager.GetClient().As<T>().GetByIds(ids).AsQueryable();
            return ret;
        }

        public void Set<T>(T item)
        {
            using (var redisClient = _redisManager.GetClient())
            {
                var redis = redisClient.As<T>();
                redis.Store(item);
            }
        }

        public void Set<T>(T item, string hash, string value, string keyName)
        {
            Type t = item.GetType();
            PropertyInfo prop = t.GetProperty(keyName);

            _redisManager.GetClient().SetEntryInHash(hash, prop.GetValue(item).ToString(), value.ToLower());

            _redisManager.GetClient().As<T>().Store(item);
        }

        public void Set<T>(T item, List<string> hash, List<string> value, string keyName)
        {
            Type t = item.GetType();
            PropertyInfo prop = t.GetProperty(keyName);

            for (int i = 0; i < hash.Count; i++)
            {
                _redisManager.GetClient().SetEntryInHash(hash[i], prop.GetValue(item).ToString(), value[i].ToLower());
            }

            _redisManager.GetClient().As<T>().Store(item);
        }

        public void SetAll<T>(List<T> listItems)
        {
            using (var redisClient = _redisManager.GetClient())
            {
                var redis = redisClient.As<T>();
                redis.StoreAll(listItems);
            }
        }

        public void SetAll<T>(List<T> list, string hash, string value, string keyName)
        {
            foreach (var item in list)
            {
                Type t = item.GetType();
                PropertyInfo prop = t.GetProperty(keyName);

                _redisManager.GetClient().SetEntryInHash(hash, prop.GetValue(item).ToString(), value.ToLower());

                _redisManager.GetClient().As<T>().StoreAll(list);
            }
        }

        public void SetAll<T>(List<T> list, List<string> hash, List<string> value, string keyName)
        {
            foreach (var item in list)
            {
                Type t = item.GetType();
                PropertyInfo prop = t.GetProperty(keyName);

                for (int i = 0; i < hash.Count; i++)
                {
                    _redisManager.GetClient().SetEntryInHash(hash[i], prop.GetValue(item).ToString(), value[i].ToLower());
                }

                _redisManager.GetClient().As<T>().StoreAll(list);
            }
        }

        public void Delete<T>(T item)
        {
            using (var redisClient = _redisManager.GetClient())
            {
                var redis = redisClient.As<T>();
                redis.Delete(item);
            }
        }

        public void DeleteAll<T>(T item)
        {
            using (var redisClient = _redisManager.GetClient())
            {
                var redis = redisClient.As<T>();
                redis.DeleteAll();
            }
        }

        public long PublishMessage(string channel, object item)
        {
            var ret = _redisManager.GetClient().PublishMessage(channel, JsonConvert.SerializeObject(item));
            return ret;
        }

        #endregion
    }
}