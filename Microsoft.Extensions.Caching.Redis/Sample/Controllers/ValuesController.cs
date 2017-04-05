using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Sample.Controllers
{
    [Route("api/[controller]")]
    public class ValuesController : Controller
    {
        private readonly IServiceStackRedisCache _cache;
        public ValuesController(IServiceStackRedisCache cache)
        {
            _cache = cache;
        }
        // GET api/values
        [HttpGet]
        public List<User> Get()
        {
            return _cache.GetAll<User>().ToList();
        }

        // GET api/values/5
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

        // POST api/values
        [HttpPost]
        public void Post([FromBody]string value)
        {
        }

        // PUT api/values/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/values/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }

    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }
}
