using BarExplorer.Cli;
using Microsoft.DotNet.DarcLib;
using Microsoft.Extensions.Logging;

namespace BarExplorer;

internal partial class BuildCommand(
    IBasicBarClient barClient,
    ILogger<BuildCommand> logger)
    : BaseCommand<BuildOptions>
{
    private readonly IBasicBarClient _barClient = barClient;
    private readonly ILogger<BuildCommand> _logger = logger;

    public override async Task<int> ExecuteAsync(BuildOptions options)
    {
        _logger.LogInformation("Getting build {options.Id}", options.Id);

        var build = await _barClient.GetBuildAsync(options.Id);

        _logger.LogInformation(
            """
            Build {options.Id}:
            {build}
            """,
            options.Id, build);

        return 0;
    }
}
