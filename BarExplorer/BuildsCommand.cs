using System.CommandLine;
using System.Text.Json;
using BarExplorer.Cli;
using Microsoft.DotNet.DarcLib;
using Microsoft.DotNet.ProductConstructionService.Client.Models;
using Microsoft.Extensions.Logging;

namespace BarExplorer;

internal class BuildsOptions : IOptions
{
    public required string Repo { get; init; }
    public required string? Commit { get; init; } = null;
    public int NumberOfBuilds { get; init; } = 10;

    public static List<Argument> Arguments { get; } =
    [
        new Argument<string>("repo")
        {
            Arity = ArgumentArity.ExactlyOne,
            Description = "Repo URI (e.g. https://github.com/dotnet/dotnet)"
        },
    ];

    public static List<Option> Options { get; } =
    [
        new Option<string?>("--commit") { Required = false },
        new Option<string?>("--number-of-builds", "-n") { Required = false },
    ];
}

internal class BuildsCommand(
    IBasicBarClient barClient,
    ILogger<BuildsCommand> logger)
    : BaseCommand<BuildsOptions>
{
    private readonly IBasicBarClient _barClient = barClient;
    private readonly ILogger<BuildsCommand> _logger = logger;

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true,
    };

    public override async Task<int> ExecuteAsync(BuildsOptions options)
    {
        _logger.LogInformation(
            "Getting builds for {options.Repo} @ {options.Commit}",
            options.Repo, options.Commit ?? "(no commit provided)");

        var builds = await _barClient.GetBuildsAsync(repoUri: options.Repo, commit: options.Commit);
        var buildsResults = builds
            .Take(options.NumberOfBuilds)
            .Select(build => new ShortBuildResult(build)).ToList();
        var buildsJson = JsonSerializer.Serialize(buildsResults, s_jsonOptions);

        _logger.LogInformation("{buildsJson}", buildsJson);

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
