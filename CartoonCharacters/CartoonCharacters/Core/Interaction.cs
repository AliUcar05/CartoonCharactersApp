using System;
using System.Threading.Tasks;

namespace CartoonCharacters.Core;

public sealed class Interaction<TInput, TOutput>
{
    private Func<TInput, Task<TOutput>>? _handler;

    public IDisposable RegisterHandler(Func<TInput, Task<TOutput>> handler)
    {
        _handler = handler;
        return new DisposeAction(() =>
        {
            if (_handler == handler)
                _handler = null;
        });
    }

    public Task<TOutput> HandleAsync(TInput input)
    {
        if (_handler is null)
            throw new InvalidOperationException("No handler registered for this Interaction.");

        return _handler(input);
    }

    private sealed class DisposeAction : IDisposable
    {
        private Action? _dispose;
        public DisposeAction(Action dispose) => _dispose = dispose;
        public void Dispose() { _dispose?.Invoke(); _dispose = null; }
    }
}