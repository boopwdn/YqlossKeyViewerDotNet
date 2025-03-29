using System.Text.Json;
using YqlossKeyViewerDotNet.Utils;

namespace YqlossKeyViewerDotNet.Storages;

public class KeyCounter
{
    private FileStream CounterFile { get; }

    private Dictionary<int, long> GlobalCounterMap { get; }
    private Dictionary<int, long> CounterMap { get; } = [];
    private Dictionary<int, long> CounterMapForRender { get; set; } = [];

    public KeyCounter(string root, double interval)
    {
        root = root.Replace("%APPDATA%", Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData));
        if (!Directory.Exists(root)) Directory.CreateDirectory(root);
        var fileName = $"{root}/{Guid.CreateVersion7()}.ykv.partial.json";
        CounterFile = new FileStream(fileName, FileMode.CreateNew, FileAccess.ReadWrite, FileShare.None);
        GlobalCounterMap = MergeCountersIntoMain(root);
        SaveCounter();
        DaemonTimer.FromNanos(interval, SaveCounter).AntiGc();
    }

    private static Dictionary<int, long> MergeCountersIntoMain(string root)
    {
        var mainFileName = $"{root}/main.json";
        FileStream mainFile;
        for (;;)
            try
            {
                mainFile = new FileStream(mainFileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                break;
            }
            catch (IOException)
            {
                Thread.Sleep(100);
            }

        var mainCounter = mainFile.Length == 0 ? [] : JsonSerializer.Deserialize<Dictionary<int, long>>(mainFile)!;
        foreach (var file in Directory.GetFiles(root))
            try
            {
                if (!file.ToLower().EndsWith(".ykv.partial.json")) continue;
                var partialCounter = JsonFile.Deserialize<Dictionary<int, long>>(file)!;
                File.Delete(file);
                foreach (var (keyCode, counter) in partialCounter)
                    mainCounter[keyCode] = mainCounter.GetValueOrDefault(keyCode, 0) + counter;
            }
            catch
            {
                // ignored
            }

        SaveCounterInto(mainFile, mainCounter);
        mainFile.Close();
        return mainCounter;
    }

    private static void SaveCounterInto(FileStream file, Dictionary<int, long> counterMap)
    {
        file.Seek(0, SeekOrigin.Begin);
        file.SetLength(0);
        file.Write(
            JsonSerializer.SerializeToUtf8Bytes(
                counterMap
                    .Select(it => KeyValuePair.Create(it.Key.ToString(), it.Value))
                    .ToDictionary()
            )
        );
        file.Flush();
    }

    public void SaveCounter()
    {
        SaveCounterInto(CounterFile, CounterMapForRender);
    }

    public long GetCounter(int keyCode)
    {
        if (keyCode == Constants.GlobalKeyCode) return GlobalCounterMap.Values.Concat(CounterMap.Values).Sum();
        return GlobalCounterMap.GetValueOrDefault(keyCode, 0) + CounterMap.GetValueOrDefault(keyCode, 0);
    }

    public void AccumulateCounter(int keyCode, long count = 1)
    {
        if (keyCode == Constants.GlobalKeyCode) return;
        lock (this)
        {
            CounterMap[keyCode] = CounterMap.GetValueOrDefault(keyCode, 0) + count;
            CounterMapForRender = new Dictionary<int, long>(CounterMap);
        }
    }
}