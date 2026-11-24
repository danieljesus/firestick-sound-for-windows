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

var filter = args.FirstOrDefault()?.ToLowerInvariant();
var connections = new ConcurrentDictionary<string, AudioPlaybackConnection>();
var names = new ConcurrentDictionary<string, string>();

void Log(string msg) => Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {msg}");

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
