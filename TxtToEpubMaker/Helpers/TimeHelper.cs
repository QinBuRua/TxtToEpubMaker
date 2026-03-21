namespace TxtToEpubMaker.Helpers;

public static class TimeHelper
{
    public static string GetNowUtcTimeString()
    {
        var nowTime = DateTime.UtcNow;
        return nowTime.ToString("yyyy-MM-ddTHH:mm:ssZ");
    }
}