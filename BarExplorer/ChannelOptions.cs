using System.CommandLine;
using BarExplorer.Cli;

namespace BarExplorer;

internal class ChannelOptions : IOptions
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
