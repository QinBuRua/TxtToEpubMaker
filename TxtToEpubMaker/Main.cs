using System.Diagnostics.CodeAnalysis;
using CommandLine;
using TxtToEpubMaker.CommandLineRunners;
using TxtToEpubMaker.Structs;

namespace TxtToEpubMaker;

public static class Program
{
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(VerbFromOptions))]
    [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(VerbBuildOptions))]
    public static void Main(string[] args)
    {
        Parser.Default.ParseArguments<VerbFromOptions, VerbBuildOptions>(args)
            .WithParsed<VerbFromOptions>(VerbFromOptions.Run)
            .WithParsed<VerbBuildOptions>(VerbBuildOptions.Run)
            .WithNotParsed(errors =>
            {
                var firstError = errors.FirstOrDefault()!;
                var statue = new EpubResult
                {
                    Success = false,
                    ErrorMessage = $"{firstError.GetType().FullName}: {firstError}"
                };
                Console.WriteLine(AppJsonContext.Serialize(statue));
                Environment.Exit(-1);
            });
    }
}