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
                var argumentException = firstError switch
                {
                    MissingRequiredOptionError missingRequired 
                        => new ArgumentNullException(missingRequired.NameInfo.LongName, missingRequired.ToString()),
                    MissingValueOptionError missingValue
                        => new ArgumentNullException(missingValue.NameInfo.LongName, missingValue.ToString()),
                    _=> new ArgumentException($"{firstError}")
                };
                var statue = new EpubResult(argumentException);
                Console.WriteLine(AppJsonContext.Serialize(statue));
                Environment.Exit(-1);
            });
    }
}