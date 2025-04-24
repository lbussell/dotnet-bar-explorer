using System.CommandLine;
using System.CommandLine.Hosting;
using Microsoft.DotNet.DarcLib;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using BarExplorer;

var rootCommand = new RootCommand()
{
    BuildCommand.Create(
        name: "build",
        description: "Get information about a specific BAR build"),
};

var config = new CommandLineConfiguration(rootCommand);

config.UseHost(
    _ => Host.CreateDefaultBuilder(),
    host => host.ConfigureServices(services =>
        {
            services.AddSingleton<IBasicBarClient>(_ => new BarApiClient(null, null, disableInteractiveAuth: false));

            BuildCommand.Register<BuildCommand>(services);
        })
    );

return await config.InvokeAsync(args);
