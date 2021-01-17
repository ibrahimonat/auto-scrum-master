using System;
using Volo.Abp.DependencyInjection;

namespace AutoScrumMaster
{
    public class HelloWorldService : ITransientDependency
    {
        public void SayHello()
        {
            Console.WriteLine("Hello World!");
        }
    }
}
