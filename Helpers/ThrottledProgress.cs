using System;

using System.Threading;

namespace VstDeleter.Helpers;

public class ThrottledProgress<T> : IProgress<T>, IDisposable
{
    private readonly IProgress<T> _underlying;
    private readonly TimeSpan _delay;
    private DateTime _lastReport = DateTime.MinValue;
    private T? _lastValue;
    private bool _hasPending = false;
    private readonly object _lock = new();
    private Timer? _timer;

    public ThrottledProgress(IProgress<T> underlying, TimeSpan delay)
    {
        _underlying = underlying;
        _delay = delay;
        _timer = new Timer(Flush, null, Timeout.Infinite, Timeout.Infinite);
    }

    public void Report(T value)
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            if (now - _lastReport >= _delay)
            {
                _lastReport = now;
                _hasPending = false;
                _underlying.Report(value);
                _timer?.Change(Timeout.Infinite, Timeout.Infinite);
            }
            else
            {
                _lastValue = value;
                _hasPending = true;
                _timer?.Change(_delay, Timeout.InfiniteTimeSpan);
            }
        }
    }

    private void Flush(object? state)
    {
        lock (_lock)
        {
            if (_hasPending)
            {
                _lastReport = DateTime.UtcNow;
                _hasPending = false;
                _underlying.Report(_lastValue!);
            }
        }
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }
}
