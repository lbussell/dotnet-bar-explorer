using System.CommandLine;
using System.Text.Json;
using BarExplorer.Cli;
using Microsoft.DotNet.DarcLib;
using Microsoft.DotNet.ProductConstructionService.Client.Models;
using Microsoft.Extensions.Logging;

namespace BarExplorer;

internal class AssetsOptions : IOptions
{
    public required int BuildId { get; init; }

    public static List<Argument> Arguments { get; } =
    [
        new Argument<int>("build-id")
        {
            Arity = ArgumentArity.ExactlyOne,
            Description = "BAR build ID to get the assets of"
        },
    ];

    public static List<Option> Options { get; } = [];
}

internal class AssetsCommand(
    IBasicBarClient barClient,
    ILogger<AssetsCommand> logger)
    : BaseCommand<AssetsOptions>
{
    private readonly IBasicBarClient _barClient = barClient;
    private readonly ILogger<AssetsCommand> _logger = logger;

    public override async Task<int> ExecuteAsync(AssetsOptions options)
    {
        _logger.LogInformation("Getting assets for build {options.BuildId}", options.BuildId);

        var assets = await _barClient.GetAssetsAsync(null, null, buildId: options.BuildId);

        var assetNames = assets
            .Select(asset => $"(Version: {asset.Version}; Id: {asset.Id}) {asset.Name}")
            .ToList();

        var assetsString = string.Join(Environment.NewLine, assetNames);

        _logger.LogInformation("{assetsString}", assetsString);

        return 0;
    }

    private class ShortBuildResult(Build build)
    {
        public int Id { get; } = build.Id;

        // Use short commit hash
        public string Commit { get; } = build.Commit[..7];

        public int? AzureDevOpsBuildId { get; } = build.AzureDevOpsBuildId;

        public string Repo { get; } = build.GitHubRepository ?? build.AzureDevOpsRepository;

        public string Branch { get; } = build.GitHubBranch ?? build.AzureDevOpsBranch;

        public DateTimeOffset DateProduced { get; } = build.DateProduced;

        public List<Channel> Channels { get; } = build.Channels;

        public bool Released { get; } = build.Released;

        public bool Stable { get; } = build.Stable;
    }
}
