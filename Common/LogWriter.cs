using System.Text;

namespace VnbisAgent.Common;

public static class LogWriter
{
    private static string GetFileName()
    {
        try
        {
            string folder;
            folder = FileSystem.Current.AppDataDirectory;
            return Path.Combine(folder, "Agent.log");
        }
        catch
        {            
            return "";
        }        
    }

    public static void WriteLine(string text)
    {
        try
        {
            string fileName;
            fileName = GetFileName();
            //string lineStr = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + text;
            string lineStr = DateTime.Now.ToString("HH:mm:ss") + "  " + text;
            File.AppendAllText(fileName, lineStr + Environment.NewLine, Encoding.UTF8);
        }
        catch { }
    }

    public static string ReadAll()
    {
        string fileName;
        fileName = GetFileName();
        if (File.Exists(fileName) == false)
        {
            return "";
        }
        string mText;
        try
        {
            mText= File.ReadAllText(fileName);
        }
        catch
        {
            mText = "";
        }
        return mText;
    }

    public static void Clear()
    {
        string fileName;

        fileName = GetFileName();

        if (File.Exists(fileName))
        {
            File.Delete(fileName);
        }
    }
}