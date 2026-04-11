using System.Text.Json;
using CommandLine;
using TxtToEpubMaker.Structs;

namespace TxtToEpubMaker.CommandLineRunners;

[Verb("from", HelpText = "Get translation task config from a Json file or a Json string")]
public class VerbFromOptions : BaseOptions
{
    [Option('f', "file", HelpText = "Get config from a Json file. You should input a file path.")]
    public string? FilePath { get; init; }

    [Option('s', "string", HelpText = "Get config from a Json string. You should input a Json string")]
    public string? JsonString { get; init; }

    public static void Run(VerbFromOptions options)
    {
        var fileIsNull = string.IsNullOrEmpty(options.FilePath);
        var stringIsNull = string.IsNullOrEmpty(options.JsonString);

        EpubResult statue;

        if (fileIsNull == stringIsNull)
        {
            var argumentException = fileIsNull
                ? new ArgumentNullException($"file", "Must input ONE argument")
                : new ArgumentException("Must input ONLY ONE argument");
            statue = new EpubResult(argumentException);
        }
        else
        {
            try
            {
                var jsonString = fileIsNull ? options.JsonString! : File.ReadAllText(options.FilePath!);
                var translationTask = AppJsonContext.Deserialize<TranslationTask>(jsonString);

                statue = TxtToEpubMaker.MakeEpubFromTask(translationTask);
            }
            catch (Exception exception)
            {
                statue = new EpubResult(exception);
            }
        }

        options.WriteCommandResultAndExit(statue);
    }
}