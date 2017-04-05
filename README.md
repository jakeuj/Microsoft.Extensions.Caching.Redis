# Microsoft.Extensions.Caching.Redis
Microsoft.Extensions.Caching.Redis

# Usage


## 1. appsettings.json

```json
{
  "redis": {
    "Host": "localhost",
    "Password": ""
  },
  "Logging": {
    "IncludeScopes": false,
    "LogLevel": {
      "Default": "Warning"
    }
  }
}
```

## 2. Startup.cs

```csharp
public class Startup
{
    public void ConfigureServices(IServiceCollection services)
    {
        // Add framework services.
        services.AddMvc();

        // Add Redis Cache services
        services.AddDistributedServiceStackRedisCache(options =>
        {
            Configuration.GetSection("redis").Bind(options);
            //Workaround for deadlock when resolving host name
            IPAddress ip;
            if (!IPAddress.TryParse(options.Host, out ip))
            {
                options.Host = Dns.GetHostAddressesAsync(options.Host)
                .Result.FirstOrDefault(a => a.AddressFamily == AddressFamily.InterNetwork).ToString();
            }
        });
        // ...
    }
    // ...
}
```

## 3. Done

```csharp
public class ValuesController : Controller
{
    private readonly IServiceStackRedisCache _cache;
    public ValuesController(IServiceStackRedisCache cache)
    {
        _cache = cache;
    }
	
    [HttpGet]
    public List<User> Get()
    {
        return _cache.GetAll<User>().ToList();
    }
	
    [HttpGet("{id}")]
    public void Get(int id)
    {
        // from db
        // var users = _userRepository.GetAll().ToList();

        // test data
        List<User> users = new List<User>();
        for (int a = 1; a < id; a++)
            users.Add(new User() { Id = a, Name = string.Format("Name{0}", a) });

        _cache.SetAll(users);
    }
}
```