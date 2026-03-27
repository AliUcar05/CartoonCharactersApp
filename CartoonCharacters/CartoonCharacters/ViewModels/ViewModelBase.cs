using System;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CartoonCharacters.Services;

namespace CartoonCharacters.ViewModels;

public abstract class ViewModelBase : ObservableObject, IDisposable
{
    private readonly CancellationTokenSource _cts = new();

    protected ScannerManager? MyScanner;

    public virtual void Dispose()
    {
        MyScanner?.ClosePort();
        _cts.Cancel();
        _cts.Dispose();
    }
}