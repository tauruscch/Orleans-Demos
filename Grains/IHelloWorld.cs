using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Grains
{
    public interface IHelloWorld : Orleans.IGrainWithIntegerKey
    {
        Task<string> SayHello();

        Task<UserState> GetUserAsync();

        Task SetUserAsync(UserState user);
    }
}
