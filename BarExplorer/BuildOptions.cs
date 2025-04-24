// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.CommandLine;
using BarExplorer.Cli;

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
