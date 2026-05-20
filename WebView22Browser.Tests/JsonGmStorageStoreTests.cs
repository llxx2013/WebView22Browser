using System.Text.Json;
using WebView22Browser.Core.Models;
using WebView22Browser.Core.Stores;

namespace WebView22Browser.Tests;

public class JsonGmStorageStoreTests : IDisposable
{
    private readonly string _root;
    private readonly JsonGmStorageStore _store;

    public JsonGmStorageStoreTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"gm-store-{Guid.NewGuid():N}");
        _store = new JsonGmStorageStore(_root, new GmStorageQuota
        {
            MaxValueBytes = 1024,
            MaxScriptBytes = 4096,
            MaxKeyLength = 64
        });
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_root))
                Directory.Delete(_root, recursive: true);
        }
        catch
        {
            // ignored
        }
    }

    [Fact]
    public async Task LoadAsync_MissingFile_ReturnsEmpty()
    {
        var loaded = await _store.LoadAsync(Guid.NewGuid());

        Assert.Empty(loaded);
    }

    [Fact]
    public async Task SetValueAsync_ThenLoad_ReturnsValue()
    {
        var scriptId = Guid.NewGuid();
        using var doc = JsonDocument.Parse("\"hello\"");
        await _store.SetValueAsync(scriptId, "key1", doc.RootElement);

        var loaded = await _store.LoadAsync(scriptId);

        Assert.True(loaded.TryGetValue("key1", out var value));
        Assert.Equal("hello", value.GetString());
    }

    [Fact]
    public async Task SetValueAsync_OverwritesExistingKey()
    {
        var scriptId = Guid.NewGuid();
        using var first = JsonDocument.Parse("1");
        using var second = JsonDocument.Parse("2");
        await _store.SetValueAsync(scriptId, "k", first.RootElement);
        await _store.SetValueAsync(scriptId, "k", second.RootElement);

        var loaded = await _store.LoadAsync(scriptId);

        Assert.Equal(2, loaded["k"].GetInt32());
    }

    [Fact]
    public async Task DeleteValueAsync_RemovesKey()
    {
        var scriptId = Guid.NewGuid();
        using var doc = JsonDocument.Parse("true");
        await _store.SetValueAsync(scriptId, "flag", doc.RootElement);
        await _store.DeleteValueAsync(scriptId, "flag");

        var loaded = await _store.LoadAsync(scriptId);

        Assert.Empty(loaded);
        Assert.False(File.Exists(Path.Combine(_root, $"{scriptId:D}.json")));
    }

    [Fact]
    public async Task DeleteScriptAsync_RemovesFile()
    {
        var scriptId = Guid.NewGuid();
        using var doc = JsonDocument.Parse("1");
        await _store.SetValueAsync(scriptId, "a", doc.RootElement);

        var path = Path.Combine(_root, $"{scriptId:D}.json");
        Assert.True(File.Exists(path));

        await _store.DeleteScriptAsync(scriptId);

        Assert.False(File.Exists(path));
    }

    [Fact]
    public async Task DeleteScriptAsync_DoesNotDeadlockOnSingleThreadedContext()
    {
        // Simulates the WPF UI-thread sync context where a synchronous Wait() on a
        // SemaphoreSlim would deadlock. The store must use await WaitAsync(...) so the
        // continuation can be marshalled back onto the single-threaded scheduler.
        var scriptId = Guid.NewGuid();
        using var doc = JsonDocument.Parse("1");
        await _store.SetValueAsync(scriptId, "a", doc.RootElement);

        var previousContext = SynchronizationContext.Current;
        using var context = new SingleThreadSynchronizationContext();
        SynchronizationContext.SetSynchronizationContext(context);
        try
        {
            // Intentionally blocking on the captured single-threaded scheduler: the whole
            // point of this test is to prove DeleteScriptAsync does not deadlock when its
            // continuation has to marshal back onto a single-threaded context.
#pragma warning disable xUnit1031
            var run = Task.Factory.StartNew(
                () => _store.DeleteScriptAsync(scriptId).GetAwaiter().GetResult(),
                CancellationToken.None,
                TaskCreationOptions.None,
                TaskScheduler.FromCurrentSynchronizationContext());
#pragma warning restore xUnit1031

            context.PumpUntil(run);
            await run;
        }
        finally
        {
            SynchronizationContext.SetSynchronizationContext(previousContext);
        }
    }

    private sealed class SingleThreadSynchronizationContext : SynchronizationContext, IDisposable
    {
        private readonly System.Collections.Concurrent.BlockingCollection<(SendOrPostCallback Callback, object? State)> _queue =
            new();

        public override void Post(SendOrPostCallback d, object? state) => _queue.Add((d, state));

        public override void Send(SendOrPostCallback d, object? state) =>
            throw new NotSupportedException("Send is not supported on the synthetic single-threaded context.");

        public void PumpUntil(Task task)
        {
            while (!task.IsCompleted)
            {
                if (!_queue.TryTake(out var work, TimeSpan.FromSeconds(5)))
                    throw new TimeoutException("Single-threaded context starved: a continuation never arrived.");

                work.Callback(work.State);
            }
        }

        public void Dispose() => _queue.Dispose();
    }

    [Fact]
    public async Task SetValueAsync_ExceedsMaxValueBytes_Throws()
    {
        var scriptId = Guid.NewGuid();
        var big = new string('x', 2000);
        using var doc = JsonDocument.Parse($"\"{big}\"");

        await Assert.ThrowsAsync<GmStorageQuotaExceededException>(() =>
            _store.SetValueAsync(scriptId, "big", doc.RootElement));
    }

    [Fact]
    public async Task SetValueAsync_ConcurrentWrites_DoNotCorruptFile()
    {
        var scriptId = Guid.NewGuid();
        var tasks = Enumerable.Range(0, 10)
            .Select(i => Task.Run(async () =>
            {
                using var doc = JsonDocument.Parse(i.ToString());
                await _store.SetValueAsync(scriptId, $"key{i}", doc.RootElement);
            }))
            .ToArray();

        await Task.WhenAll(tasks);

        var loaded = await _store.LoadAsync(scriptId);
        Assert.Equal(10, loaded.Count);
    }
}
