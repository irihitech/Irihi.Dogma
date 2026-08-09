namespace Irihi.Dogma.Docs;

/// <summary>
/// 极简 behavior 语义的 <see cref="IObservable{T}"/> 实现（零 System.Reactive 依赖）：
/// 订阅时立即推送当前值，之后在值变化时推送。
/// 用于 Lingua 键缺失或未设置 <see cref="DocSite.LinguaManager"/> 时的标题兜底。
/// </summary>
internal sealed class ObservableValue<T> : IObservable<T>
{
    private readonly object _gate = new();
    private readonly HashSet<IObserver<T>> _observers = [];
    private T _value;

    public ObservableValue(T value) => _value = value;

    public T Value
    {
        get
        {
            lock (_gate)
            {
                return _value;
            }
        }
    }

    public void SetValue(T value)
    {
        IObserver<T>[]? toNotify = null;
        lock (_gate)
        {
            _value = value;
            if (_observers.Count > 0)
            {
                toNotify = [.. _observers];
            }
        }

        if (toNotify is not null)
        {
            foreach (var observer in toNotify)
            {
                observer.OnNext(value);
            }
        }
    }

    public IDisposable Subscribe(IObserver<T> observer)
    {
        lock (_gate)
        {
            _observers.Add(observer);
            observer.OnNext(_value);
        }

        return new Unsubscriber(this, observer);
    }

    private sealed class Unsubscriber(ObservableValue<T> owner, IObserver<T> observer) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            lock (owner._gate)
            {
                owner._observers.Remove(observer);
            }

            _disposed = true;
        }
    }
}
