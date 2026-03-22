using System.Text.Json;
using TxtToEpubMaker.Structs;

var commandLineArgs = Environment.GetCommandLineArgs();
EpubResult statue;

if (commandLineArgs.Length != 2)
{
    statue = new EpubResult
    {
        Success = false,
        ErrorMessage = "Invalid arguments"
    };
}
else if (!File.Exists(commandLineArgs[1]))
{
    statue = new EpubResult
    {
        Success = false,
        ErrorMessage = $"TaskFile \"{commandLineArgs[1]}\" not found"
    };
}
else
{
    try
    {
        var taskFile = File.ReadAllText(Environment.GetCommandLineArgs()[1]);
        var task = JsonSerializer.Deserialize<TranslationTask>(taskFile);
        statue = TxtToEpubMaker.TxtToEpubMaker.MakeEpubFromTask(task);
    }
    catch (Exception exception)
    {
        statue = new EpubResult
        {
            Success = false,
            ErrorMessage = $"{exception.GetType().Name}: {exception}"
        };
    }
}

Console.Write(JsonSerializer.Serialize(statue));