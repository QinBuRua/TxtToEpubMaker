using System.Text.Json;
using TxtToEpubMaker;
using TxtToEpubMaker.Structs;

var taskFile = File.ReadAllText("./test_epub_content/translation_task.json");
var task = JsonSerializer.Deserialize<TranslationTask>(taskFile);

var statue = TxtToEpubMaker.TxtToEpubMaker.MakeEpubFromTask(task);

Console.Write(JsonSerializer.Serialize(statue));