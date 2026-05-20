using System.Diagnostics;
using System.Runtime.InteropServices;
using WebView22Browser.Core;
using WebView22Browser.Core.Services;

namespace WebView22Browser.App.Services;

public sealed class SystemPressureMonitor : ISystemPressureMonitor
{
    private readonly BrowserOptions _options;
    private readonly object _lock = new();
    private CancellationTokenSource? _cts;
    private SystemPressureLevel _current = SystemPressureLevel.Normal;
    private bool _hasInitialEma;
    private double _emaMemoryPercent;
    private double _emaCpuPercent;
    private int _consecutiveElevated;
    private int _consecutiveHigh;
    private DateTime? _lastCpuSampleUtc;
    private TimeSpan _lastCpuTotal;

    public SystemPressureMonitor(BrowserOptions options)
    {
        _options = options;
    }

    public SystemPressureLevel Current
    {
        get
        {
            lock (_lock)
                return _current;
        }
    }

    public event EventHandler? Changed;

    public void Start()
    {
        if (_cts != null)
            return;

        _cts = new CancellationTokenSource();
        _ = Task.Run(() => SampleLoopAsync(_cts.Token));
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts?.Dispose();
        _cts = null;
    }

    public void Dispose() => Stop();

    private async Task SampleLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                SampleOnce();
            }
            catch
            {
                // Counter read failures — stay at Normal.
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private void SampleOnce()
    {
        var memoryPercent = ReadMemoryLoadPercent();
        var cpuPercent = ReadProcessCpuPercent();
        var sampleWindow = Math.Max(1, _options.PressureSampleWindowSeconds);
        var alpha = 1.0 / (sampleWindow / 5.0 + 1.0);

        EventHandler? handlers = null;

        lock (_lock)
        {
            if (memoryPercent.HasValue)
            {
                if (!_hasInitialEma)
                {
                    _emaMemoryPercent = memoryPercent.Value;
                    _emaCpuPercent = cpuPercent;
                    _hasInitialEma = true;
                }
                else
                {
                    _emaMemoryPercent = _emaMemoryPercent * (1 - alpha) + memoryPercent.Value * alpha;
                    _emaCpuPercent = _emaCpuPercent * (1 - alpha) + cpuPercent * alpha;
                }
            }

            if (!_hasInitialEma)
                return;

            var proposed = ProposeLevel(_emaMemoryPercent, _emaCpuPercent);
            var levelChanged = false;

            if (proposed > _current)
            {
                if (proposed == SystemPressureLevel.Elevated)
                    _consecutiveElevated++;
                else if (proposed == SystemPressureLevel.High)
                    _consecutiveHigh++;

                if ((proposed == SystemPressureLevel.Elevated && _consecutiveElevated >= 2)
                    || (proposed == SystemPressureLevel.High && _consecutiveHigh >= 2))
                {
                    if (proposed != _current)
                    {
                        _current = proposed;
                        levelChanged = true;
                    }
                }
            }
            else if (proposed < _current)
            {
                _consecutiveElevated = 0;
                _consecutiveHigh = 0;
                _current = proposed;
                levelChanged = true;
            }
            else
            {
                _consecutiveElevated = 0;
                _consecutiveHigh = 0;
            }

            if (levelChanged)
                handlers = Changed;
        }

        handlers?.Invoke(this, EventArgs.Empty);
    }

    private SystemPressureLevel ProposeLevel(double memoryPercent, double cpuPercent)
    {
        if (memoryPercent >= _options.PressureHighMemoryPercent
            || (memoryPercent >= _options.PressureElevatedMemoryPercent
                && cpuPercent >= _options.PressureHighCpuPercent))
        {
            return SystemPressureLevel.High;
        }

        if (memoryPercent >= _options.PressureElevatedMemoryPercent)
            return SystemPressureLevel.Elevated;

        return SystemPressureLevel.Normal;
    }

    private static double? ReadMemoryLoadPercent()
    {
        var status = new MemoryStatusEx { dwLength = (uint)Marshal.SizeOf<MemoryStatusEx>() };
        if (GlobalMemoryStatusEx(ref status))
            return status.dwMemoryLoad;

        var gcInfo = GC.GetGCMemoryInfo();
        if (gcInfo.TotalAvailableMemoryBytes > 0)
            return 100.0 * gcInfo.MemoryLoadBytes / (double)gcInfo.TotalAvailableMemoryBytes;

        return null;
    }

    private double ReadProcessCpuPercent()
    {
        var process = Process.GetCurrentProcess();
        var now = process.TotalProcessorTime;
        var utcNow = DateTime.UtcNow;

        if (_lastCpuSampleUtc == null)
        {
            _lastCpuSampleUtc = utcNow;
            _lastCpuTotal = now;
            return 0;
        }

        var elapsed = utcNow - _lastCpuSampleUtc.Value;
        if (elapsed.TotalMilliseconds < 100)
        {
            lock (_lock)
                return _emaCpuPercent;
        }

        var cpuDelta = (now - _lastCpuTotal).TotalMilliseconds;
        _lastCpuSampleUtc = utcNow;
        _lastCpuTotal = now;

        var percent = elapsed.TotalMilliseconds > 0
            ? cpuDelta / (Environment.ProcessorCount * elapsed.TotalMilliseconds) * 100.0
            : 0;

        return Math.Clamp(percent, 0, 100);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryStatusEx
    {
        public uint dwLength;
        public uint dwMemoryLoad;
        public ulong ullTotalPhys;
        public ulong ullAvailPhys;
        public ulong ullTotalPageFile;
        public ulong ullAvailPageFile;
        public ulong ullTotalVirtual;
        public ulong ullAvailVirtual;
        public ulong ullAvailExtendedVirtual;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx lpBuffer);
}
