using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Grains;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Orleans;

namespace Silo.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HelloController : ControllerBase
    {
        private readonly IGrainFactory _client;
        private readonly IHelloWorld _grain;

        public HelloController(IGrainFactory client)
        {
            _client = client;
            _grain = _client.GetGrain<IHelloWorld>(0);
        }

        [HttpGet]
        public Task<string> SayHello() => this._grain.SayHello();

        [HttpGet]
        [Route("SetUser")]
        public async Task<string> SetUser()
        {
            await _grain.SetUserAsync(new UserState { Name = "cch", Birthday = DateTime.Now });
            return "ok";
        }

        [HttpGet]
        [Route("GetUser")]
        public async Task<UserState> GetUser()
        {
            return await _grain.GetUserAsync();
        }
    }
}
