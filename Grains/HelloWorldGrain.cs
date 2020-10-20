using Microsoft.Extensions.Logging;
using Orleans;
using Orleans.Runtime;
using System;
using System.Threading.Tasks;

namespace Grains
{
    public class HelloWorldGrain : Orleans.Grain, IHelloWorld
    {
        private readonly ILogger<HelloWorldGrain> _logger; 
        
        private readonly IPersistentState<UserState> _user;

        public HelloWorldGrain(ILogger<HelloWorldGrain> logger,
            [PersistentState("user", "demo")] IPersistentState<UserState> user)
        {
            _logger = logger;
            _user = user;
        }

        public async Task<string> SayHello()
        {
            _logger.LogInformation("Silo:{0} is answering.", this.RuntimeIdentity);
            return await Task.FromResult("Hello world!");
        }

        public Task<UserState> GetUserAsync() => Task.FromResult(_user.State);

        public async Task SetUserAsync(UserState user)
        {
            _user.State = user;
            await _user.WriteStateAsync();
        }
    }
}
