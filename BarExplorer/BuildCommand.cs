using System.CommandLine;
using System.Text.Json;
using BarExplorer.Cli;
using Microsoft.DotNet.DarcLib;
using Microsoft.DotNet.ProductConstructionService.Client.Models;
using Microsoft.Extensions.Logging;

namespace BarExplorer;

internal class BuildOptions : IOptions
{
    public required int Id { get; init; }

    public static List<Argument> Arguments { get; } =
    [
        new Argument<int>("id")
        {
            Arity = ArgumentArity.ExactlyOne,
            Description = "BAR build ID to inspect (see https://aka.ms/bar)"
        },
    ];

    public static List<Option> Options { get; } =
    [
    ];
}

internal class BuildCommand(
    IBasicBarClient barClient,
    ILogger<BuildCommand> logger)
    : BaseCommand<BuildOptions>
{
    private readonly IBasicBarClient _barClient = barClient;
    private readonly ILogger<BuildCommand> _logger = logger;

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true,
    };

    public override async Task<int> ExecuteAsync(BuildOptions options)
    {
        _logger.LogInformation("Getting build {options.Id}", options.Id);

        var build = await _barClient.GetBuildAsync(options.Id);
        var buildResult = new BuildResult(build);
        var buildJson = JsonSerializer.Serialize(buildResult, s_jsonOptions);

        _logger.LogInformation(
            """
            Build {options.Id}:
            {buildJson}
            """,
            options.Id, buildJson);

        return 0;
    }

    // Custom wrapper to exclude Assets because the array can be really large
    private class BuildResult(Build build)
    {
        public int Id { get; } = build.Id;
        public string Commit { get; } = build.Commit;
        public int? AzureDevOpsBuildId { get; } = build.AzureDevOpsBuildId;
        public int? AzureDevOpsBuildDefinitionId { get; } = build.AzureDevOpsBuildDefinitionId;
        public string AzureDevOpsAccount { get; } = build.AzureDevOpsAccount;
        public string AzureDevOpsProject { get; } = build.AzureDevOpsProject;
        public string AzureDevOpsBuildNumber { get; } = build.AzureDevOpsBuildNumber;
        public string AzureDevOpsRepository { get; } = build.AzureDevOpsRepository;
        public string AzureDevOpsBranch { get; } = build.AzureDevOpsBranch;
        public string GitHubRepository { get; } = build.GitHubRepository;
        public string GitHubBranch { get; } = build.GitHubBranch;
        public DateTimeOffset DateProduced { get; } = build.DateProduced;
        public List<Channel> Channels { get; } = build.Channels;
        public List<BuildRef> Dependencies { get; } = build.Dependencies;
        public List<BuildIncoherence> Incoherencies { get; } = build.Incoherencies;
        public int Staleness { get; } = build.Staleness;
        public bool Released { get; } = build.Released;
        public bool Stable { get; } = build.Stable;
    }
}
