global using System.Configuration;
global using System.Runtime.InteropServices;
global using System.Text;
global using static MIDIMapper.CustomClass;
using MIDIMapper;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices(services =>
    {
        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();
