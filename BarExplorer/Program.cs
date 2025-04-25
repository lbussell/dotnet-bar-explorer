using System.CommandLine;
using System.CommandLine.Hosting;
using Microsoft.DotNet.DarcLib;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using BarExplorer;

var rootCommand = new RootCommand()
{
    AssetsCommand.Create(
        name: "assets",
        description: "Get assets for a specific BAR build"),
    BuildCommand.Create(
        name: "build",
        description: "Get information about a specific BAR build"),
    BuildsCommand.Create(
        name: "builds",
        description: "Get multiple BAR builds"),
    ChannelCommand.Create(
        name: "channel",
        description: "Get information about a specific BAR channel"),
};

var config = new CommandLineConfiguration(rootCommand);

config.UseHost(
    _ => Host.CreateDefaultBuilder(),
    host => host.ConfigureServices(services =>
        {
            services.AddHttpClient();
            services.AddSingleton<IBasicBarClient>(_ => new BarApiClient(null, null, disableInteractiveAuth: false));

            AssetsCommand.Register<AssetsCommand>(services);
            BuildCommand.Register<BuildCommand>(services);
            BuildsCommand.Register<BuildsCommand>(services);
            ChannelCommand.Register<ChannelCommand>(services);
        })
    );

return await config.InvokeAsync(args);
