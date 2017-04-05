using Microsoft.Extensions.Caching.Redis;
using Microsoft.Extensions.Configuration;
using System;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class RedisCacheServiceCollectionExtensions
    {
        /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
        public static IServiceCollection AddDistributedServiceStackRedisCache(this IServiceCollection services, Action<ServiceStackRedisCacheOptions> setupAction)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (setupAction == null)
            {
                throw new ArgumentNullException(nameof(setupAction));
            }

            services.AddOptions();
            services.Configure(setupAction);
            //services.Add(ServiceDescriptor.Singleton<IDistributedCache, ServiceStackRedisCache>());
            services.Add(ServiceDescriptor.Singleton<IServiceStackRedisCache, ServiceStackRedisCache>());
            
            return services;
        }

        public static IServiceCollection AddDistributedServiceStackRedisCache(this IServiceCollection services, IConfigurationSection section)
        {
            if (services == null)
            {
                throw new ArgumentNullException(nameof(services));
            }

            if (section == null)
            {
                throw new ArgumentNullException(nameof(section));
            }

            services.Configure<ServiceStackRedisCacheOptions>(section);

            services.Add(ServiceDescriptor.Transient<IServiceStackRedisCache, ServiceStackRedisCache>());

            return services;
        }
    }
}
