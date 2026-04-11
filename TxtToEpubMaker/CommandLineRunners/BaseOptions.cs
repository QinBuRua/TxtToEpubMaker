using System.Text.Json;
using CommandLine;
using TxtToEpubMaker.Structs;

namespace TxtToEpubMaker.CommandLineRunners;

public class BaseOptions
{
    [Option('v', "verbose", Required = false, Default = false,
        HelpText = "Get detailed information made by Json string")]
    public bool Verbose { get; init; }

    protected void WriteCommandResultAndExit(EpubResult statue)
    {
        if (Verbose)
        {
            Console.WriteLine(AppJsonContext.Serialize(statue));
            Environment.Exit(statue.Success ? 0 : -1);
        }

        if (statue.Success)
        {
            Environment.Exit(0);
        }

        var messageResult =
            ProcessExceptionMessage(
                statue.InnerException ?? new NullReferenceException("statue.InnerException is null"));

        Console.WriteLine(messageResult);
        Environment.Exit(-1);
    }

    private static string ProcessDefaultErrorMessage(Exception exception)
    {
        return $"{exception.Message}";
    }

    private static string ProcessNullReferenceErrorMessage(NullReferenceException nullReferenceException)
    {
        return $"Inner Error. Please send this message to Developer. | Details: {nullReferenceException}";
    }

    private static string ProcessJsonErrorMessage(JsonException jsonException)
    {
        return jsonException.Message.Contains("missing required properties")
            ? ProcessJsonErrorMessageIfMissingRequired(jsonException)
            : $"{jsonException.GetType().FullName}: {jsonException.Message}";
    }

    private static string ProcessJsonErrorMessageIfMissingRequired(JsonException jsonException)
    {
        var errorMessage = jsonException.Message;

        var typeFullNameBegIndex = errorMessage.IndexOf("type '", StringComparison.Ordinal) + 6;
        var typeNameEndIndex = errorMessage.IndexOf('\'', typeFullNameBegIndex);
        var typeNameBegIndex = errorMessage.LastIndexOfAny(['.', '+'], typeNameEndIndex) + 1;
        typeNameBegIndex = typeNameBegIndex >= 0 ? typeNameBegIndex : typeFullNameBegIndex;
        var typeNameString = errorMessage[typeNameBegIndex..typeNameEndIndex];

        var keyNameBegIndex = errorMessage.IndexOf('\'', typeNameEndIndex + 1) + 1;
        var keyNameEndIndex = errorMessage.IndexOf('\'', keyNameBegIndex + 1);
        var keyNameString = errorMessage[keyNameBegIndex..keyNameEndIndex];

        typeNameString = typeNameString switch
        {
            nameof(TranslationTask) => "GlobalSettings",
            nameof(TranslationTask.BookContentSet) => "BookContent",
            nameof(TranslationTask.BookContentSet.Volume) => "Volumes",
            nameof(TranslationTask.BookContentSet.ChapterLinker) => "Chapters",
            _ => typeNameString
        };

        return
            $"Missing required key \"{keyNameString}\", from \"{typeNameString}\", in line {jsonException.LineNumber}";
    }


    private static string ProcessExceptionMessage(Exception exception)
    {
        return exception switch
        {
            JsonException jsonException => ProcessJsonErrorMessage(jsonException),
            NullReferenceException nullReferenceException => ProcessNullReferenceErrorMessage(nullReferenceException),
            _ => ProcessDefaultErrorMessage(exception)
        };
    }
}