using CommandLine;
using TxtToEpubMaker.Structs;

namespace TxtToEpubMaker.CommandLineRunners;

public class BaseOptions
{
    [Option('v', "verbose", Required = false, Default = false, HelpText = "Get detailed information made by Json string")]
    public bool Verbose { get; init; }

    public void WriteCommandResultAndExit(EpubResult statue)
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

        var errorMessage = statue.ErrorMessage!;
        var exceptionType = ExtractExceptionType(errorMessage) ?? "";
        var messageResult =
            ErrorMessageTranslationTab.GetValueOrDefault(exceptionType, ExtractDefaultErrorMessage)(errorMessage);

        Console.WriteLine(messageResult);
        Environment.Exit(-1);
    }

    private static string? ExtractExceptionType(string message)
    {
        var endIndex = message.IndexOf(": ", StringComparison.Ordinal);
        return endIndex >= 0 ? message[..endIndex] : null;
    }

    private static string ExtractDefaultErrorMessage(string message)
    {
        var index = message.IndexOf(": ", StringComparison.Ordinal) + 2;
        return index >= 0 ? message[index..] : message;
    }

    private static string ExtractJsonErrorMessage(string message)
    {
        var preprocessedMessage = ExtractDefaultErrorMessage(message);
        string result;
        if (preprocessedMessage.Contains("was missing required properties"))
        {
            result = ExtractJsonErrorMessageIfMissingRequired(preprocessedMessage);
        }
        else
        {
            result = preprocessedMessage;
        }

        return result;
    }

    private static string ExtractJsonErrorMessageIfMissingRequired(string preprocessedMessage)
    {
        var setNameFullBegIndex = preprocessedMessage.IndexOf("for type '", StringComparison.Ordinal) + 10;
        var setNameEndIndex = preprocessedMessage.IndexOf('\'', setNameFullBegIndex);
        var setNameBegIndex = preprocessedMessage.LastIndexOfAny(['.', '+'], setNameEndIndex);
        setNameBegIndex = setNameEndIndex >= 0 ? setNameBegIndex + 1 : setNameFullBegIndex;
        var setName = preprocessedMessage[setNameBegIndex..setNameEndIndex];

        var keyBegIndex = preprocessedMessage.IndexOf(": '", StringComparison.Ordinal) + 3;
        var keyEndIndex = preprocessedMessage.IndexOf('\'', keyBegIndex);
        var requiredKey = preprocessedMessage[keyBegIndex..keyEndIndex];

        var lineNumBegIndex = preprocessedMessage.LastIndexOf("| Line: ", StringComparison.Ordinal) + 8;
        var lineNumString = preprocessedMessage[lineNumBegIndex..];

        setName = setName switch
        {
            nameof(TranslationTask) => "GlobalSettings",
            nameof(TranslationTask.BookContentSet) => "BookContent",
            nameof(TranslationTask.BookContentSet.Volume) => "Volume",
            nameof(TranslationTask.BookContentSet.ChapterLinker) => "Chapter",
            _ => setName
        };

        return $"Missing Key \"{requiredKey}\", from \"{setName}\", in line {lineNumString}.";
    }


    private static readonly Dictionary<string, Func<string, string>> ErrorMessageTranslationTab = new()
    {
        ["System.ArgumentNullException"] = message =>
            $"Inner Error. Please Send this bug to developer, thanks. Details: {message}",
        ["System.NullReferenceException"] = message =>
            $"Inner Error. Please Send this bug to developer, thanks.  Details: {message}",
        ["System.IO.FileNotFoundException"] = ExtractDefaultErrorMessage,
        ["System.IO.DirectoryNotFoundException"] = ExtractDefaultErrorMessage,
        ["System.IO.IOException"] = ExtractDefaultErrorMessage,
        ["System.IO.PathTooLongException"] = ExtractDefaultErrorMessage,
        ["System.IO.DriveNotFoundException"] = ExtractDefaultErrorMessage,
        ["System.IO.EndOfStreamException"] = ExtractDefaultErrorMessage,
        ["System.IO.FileLoadException"] = ExtractDefaultErrorMessage,
        ["System.Text.Json.JsonException"] = ExtractJsonErrorMessage,
        [""] = message => message
    };
}