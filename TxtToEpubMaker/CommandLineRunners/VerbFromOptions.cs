using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using CommandLine;
using TxtToEpubMaker.Structs;

namespace TxtToEpubMaker.CommandLineRunners;


[Verb("from", HelpText = "Get translation task config from a Json file or a Json string")]
public class VerbFromOptions
{
    [Option('f', "file", HelpText = "Get config from a Json file. You should input a file path.")]
    public string? FilePath { get; set; }

    [Option('s', "string", HelpText = "Get config from a Json string. You should input a Json string")]
    public string? JsonString { get; set; }

    public VerbFromOptions()
    {
    }

    public VerbFromOptions(string? filePath, string? jsonString)
    {
        FilePath = filePath;
        JsonString = jsonString;
    }

    public static void Run(VerbFromOptions options)
    {
        var fileIsNull = options.FilePath == null;
        var stringIsNull = options.JsonString == null;

        EpubResult statue;

        if (fileIsNull == stringIsNull)
        {
            statue = new EpubResult
            {
                Success = false,
                ErrorMessage = $"Must input {(fileIsNull ? "" : "ONLY ")}ONE argument"
            };
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
                statue = new EpubResult
                {
                    Success = false,
                    ErrorMessage =
                        $"{exception.GetType().Namespace}.{exception.GetType().FullName}: {exception.Message}"
                };
            }
        }

        Console.WriteLine(AppJsonContext.Serialize(statue));
        Environment.Exit(statue.Success ? 0 : -1);
    }
}