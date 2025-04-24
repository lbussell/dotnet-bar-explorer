using System.Text.Json;
using BarExplorer.Cli;
using Microsoft.DotNet.DarcLib;
using Microsoft.Extensions.Logging;

namespace BarExplorer;

internal class ChannelCommand(
    IBasicBarClient barClient,
    ILogger<ChannelCommand> logger)
    : BaseCommand<ChannelOptions>
{
    private readonly IBasicBarClient _barClient = barClient;
    private readonly ILogger<ChannelCommand> _logger = logger;

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        WriteIndented = true,
    };

    public override async Task<int> ExecuteAsync(ChannelOptions options)
    {
        _logger.LogInformation("Getting channel {options.Id}", options.Id);

        var channel = await _barClient.GetChannelAsync(options.Id);
        var channelJson = JsonSerializer.Serialize(channel, s_jsonOptions);

        _logger.LogInformation(
            """
            Channel {options.Id}:
            {channelJson}
            """,
            options.Id, channelJson);

        return 0;
    }
}
