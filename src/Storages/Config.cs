namespace YqlossKeyViewerDotNet.Storages;

public class Config
{
    public string RenderEngine { get; set; } = "Direct2D";
    public double Scale { get; set; } = 1;
    public double MaxDuration { get; set; } = 10e9;
    public double FrameRate { get; set; } = 60;
    public double PollRate { get; set; } = 60;
    public bool HookKeyboard { get; set; } = false;
    public bool HookMouse { get; set; } = false;
    public bool PollKeys { get; set; } = true;
    public string CounterPath { get; set; } = "Global";
    public double SaveCounterInterval { get; set; } = 60e9;

    public Dictionary<string, string> CounterPaths { get; set; } = new()
    {
        { "Global", "%APPDATA%/YqlossKeyViewer/KeyCounter" },
        { "Local", "./YqlossKeyViewer/KeyCounter" }
    };

    public static Config ReadOrGenerate(string filePath)
    {
        if (File.Exists(filePath)) return JsonFile.Deserialize<Config>(filePath)!;
        var config = new Config();
        JsonFile.Serialize(filePath, config);
        return config;
    }
}