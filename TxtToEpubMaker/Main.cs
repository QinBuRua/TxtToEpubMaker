using CommandLine;
using TxtToEpubMaker;
using TxtToEpubMaker.CommandLineRunners;
using TxtToEpubMaker.Structs;

var commandLineArgs = Environment.GetCommandLineArgs().Skip(1).ToArray();

Parser.Default.ParseArguments<VerbFromOptions, VerbBuildOptions>(commandLineArgs)
    .WithParsed<VerbFromOptions>(VerbFromOptions.Run)
    .WithParsed<VerbBuildOptions>(VerbBuildOptions.Run)
    .WithNotParsed(errors =>
    {
        var firstError = errors.FirstOrDefault()!;
        var statue = new EpubResult
        {
            Success = false,
            ErrorMessage = $"{firstError.GetType().Namespace}.{firstError.GetType().FullName}: {firstError}"
        };
        Console.WriteLine(AppJsonContext.Serialize(statue));
        Environment.Exit(-1);
    });