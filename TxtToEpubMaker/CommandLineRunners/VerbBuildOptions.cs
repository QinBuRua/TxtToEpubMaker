using CommandLine;

namespace TxtToEpubMaker.CommandLineRunners;

[Verb("build", HelpText = "Build translation task config from command line arguments.")]
public class VerbBuildOptions
{
    public static void Run(VerbBuildOptions options)
    {
        Console.WriteLine("Still Develop!!!");
        Environment.Exit(-1);
    }
}