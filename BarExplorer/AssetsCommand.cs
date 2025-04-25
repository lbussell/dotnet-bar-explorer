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
    public int? AssetId { get; init; } = null;
    public bool Contents { get; init; } = false;

    public static List<Argument> Arguments { get; } =
    [
        new Argument<int>("build-id")
        {
            Arity = ArgumentArity.ExactlyOne,
            Description = "BAR build ID to get the assets of"
        },
    ];

    public static List<Option> Options { get; } =
    [
        new Option<int?>("--asset-id", "-id")
        {
            Required = false,
            Description = "Specific asset ID to get detailed information about"
        },
        new Option<bool?>("--contents")
        {
            Required = false,
            Description = "Get the contents (as text) of the specific asset"
        },
    ];
}

internal class AssetsCommand(
    HttpClient httpClient,
    IBasicBarClient barClient,
    ILogger<AssetsCommand> logger)
    : BaseCommand<AssetsOptions>
{
    private readonly HttpClient _httpClient = httpClient;
    private readonly IBasicBarClient _barClient = barClient;
    private readonly ILogger<AssetsCommand> _logger = logger;

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true,
    };

    public override async Task<int> ExecuteAsync(AssetsOptions options)
    {
        if (options.AssetId is not null)
        {
            return await GetSpecificAssetInfo(options.BuildId, options.AssetId.Value, options.Contents);
        }

        return await GetAssetsSummary(options.BuildId);
    }

    private async Task<int> GetSpecificAssetInfo(int buildId, int assetId, bool getContents = false)
    {
        // Fetch the asset information from BAR
        _logger.LogInformation("Getting asset {assetId} from build {buildId}", assetId, buildId);
        var assets = await _barClient.GetAssetsAsync(null, null, buildId: buildId);

        // Log basic information about the asset
        var asset = assets.Single(a => a.Id == assetId);
        var assetJson = JsonSerializer.Serialize(asset, s_jsonOptions);
        _logger.LogInformation("{assetJson}", assetJson);

        // Get the asset's locations
        var assetUrls = GetAssetUrls(asset);
        var assetUrlsString = string.Join(Environment.NewLine, assetUrls);
        _logger.LogInformation(
            """
            Asset {asset.Name} has {asset.Locations.Count} locations:
            {assetUrls}
            """,
            asset.Name, asset.Locations.Count, assetUrls);

        // If specified, download and log text contents of the asset
        if (getContents)
        {
            var downloadUrl = ResolveDownloadUrl(assetUrls);
            _logger.LogInformation("Getting contents from {downloadUrl}", downloadUrl);

            var contents = await GetAssetTextContentsAsync(downloadUrl);
            _logger.LogInformation("Contents of {asset.Name}:\n{contents}", asset.Name, contents);
        }

        return 0;
    }

    private async Task<string> GetAssetTextContentsAsync(string url)
    {
        try
        {
            var response = await _httpClient.GetAsync(url);
            response.EnsureSuccessStatusCode();
            string content = await response.Content.ReadAsStringAsync();
            return content.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch contents from {url}", url);
            throw;
        }
    }

    private async Task<int> GetAssetsSummary(int buildId)
    {
        _logger.LogInformation("Getting assets for build {options.BuildId}", buildId);
        var assets = await _barClient.GetAssetsAsync(null, null, buildId: buildId);

        var assetNames = assets
            .Select(asset => $"(Version: {asset.Version}; Id: {asset.Id}) {asset.Name}")
            .ToList();
        var assetsString = string.Join(Environment.NewLine, assetNames);
        _logger.LogInformation("{assetsString}", assetsString);

        return 0;
    }

    private static string ResolveDownloadUrl(IEnumerable<string> assetUrls)
    {
        var blobStorageLocations =
            assetUrls.Where(url => url.Contains("blob.core.windows.net"));
        var azdoLocations =
            assetUrls.Where(url => url.Contains("dev.azure.com"));

        // Prefer public blob storage locations over azdo locations
        var bestLocation =
            blobStorageLocations.FirstOrDefault()
            ?? azdoLocations.FirstOrDefault()
            ?? throw new InvalidOperationException("Asset does not have any valid download locations");

        return bestLocation;
    }

    private static IEnumerable<string> GetAssetUrls(Asset asset)
    {
        return asset.Locations.Select(l => $"{l.Location}/{asset.Name}");
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
