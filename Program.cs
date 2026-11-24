using System.Collections.Concurrent;
using Windows.Devices.Enumeration;
using Windows.Devices.Radios;
using Windows.Media.Audio;

if (args.FirstOrDefault() == "--restart-radio")
{
    await Radio.RequestAccessAsync();
    foreach (var r in (await Radio.GetRadiosAsync()).Where(r => r.Kind == RadioKind.Bluetooth))
    {
        Console.WriteLine($"{r.Name}: {r.State} -> Off: {await r.SetStateAsync(RadioState.Off)}");
        await Task.Delay(3000);
        Console.WriteLine($"{r.Name}: -> On: {await r.SetStateAsync(RadioState.On)}");
    }
    return;
}

var logIndex = Array.IndexOf(args, "--log");
var logFile = logIndex >= 0 && logIndex + 1 < args.Length ? args[logIndex + 1] : null;
var filter = args.Where((a, i) => logIndex < 0 || (i != logIndex && i != logIndex + 1)).FirstOrDefault()?.ToLowerInvariant();
var connections = new ConcurrentDictionary<string, AudioPlaybackConnection>();
var names = new ConcurrentDictionary<string, string>();
var logLock = new object();

void Log(string msg)
{
    var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {msg}";
    Console.WriteLine(line);
    if (logFile is null) return;
    lock (logLock)
    {
        try { File.AppendAllText(logFile, line + Environment.NewLine); } catch (IOException) { }
    }
}

async Task Connect(string id)
{
    var name = names.GetValueOrDefault(id, id);
    if (connections.TryRemove(id, out var old)) old.Dispose();

    var con = AudioPlaybackConnection.TryCreateFromId(id);
    if (con is null) { Log($"Could not create a connection for {name}"); return; }
    connections[id] = con;

    con.StateChanged += (s, _) =>
    {
        Log($"{name}: {s.State}");
        if (s.State == AudioPlaybackConnectionState.Closed)
            _ = Task.Delay(3000).ContinueWith(_ => Connect(id));
    };

    await con.StartAsync();
    Log($"{name}: receiver ready, waiting for audio...");
    var r = await con.OpenAsync();
    Log($"{name}: open -> {r.Status}{(r.ExtendedError is { } e ? $" ({e.Message})" : "")}");
}

var watcher = DeviceInformation.CreateWatcher(AudioPlaybackConnection.GetDeviceSelector());
watcher.Added += (w, info) =>
{
    Log($"Audio source found: {info.Name}");
    if (filter is not null && !info.Name.ToLowerInvariant().Contains(filter)) return;
    names[info.Id] = info.Name;
    _ = Connect(info.Id);
};
watcher.Removed += (w, upd) =>
{
    if (connections.TryRemove(upd.Id, out var c)) c.Dispose();
    Log($"Removed: {names.GetValueOrDefault(upd.Id, upd.Id)}");
};
watcher.EnumerationCompleted += (w, _) =>
    Log(connections.IsEmpty ? "No paired audio sources yet. Pair your Fire TV with this PC." : "Enumeration complete.");

watcher.Start();
Log($"Bluetooth A2DP receiver running{(filter is null ? "" : $" (filter: {filter})")}. Ctrl+C to quit.");
var done = new TaskCompletionSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; done.TrySetResult(); };
await done.Task;
watcher.Stop();
foreach (var c in connections.Values) c.Dispose();
