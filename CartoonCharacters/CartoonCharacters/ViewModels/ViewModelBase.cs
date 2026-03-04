using System;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using MyProjectBase.Services;

namespace CartoonCharacters.ViewModels;

public abstract class ViewModelBase : ObservableObject, IDisposable
{
    private readonly CancellationTokenSource _cts = new();
    internal ScannerManager? MyScanner;
    
    public void Dispose()
    {
        MyScanner?.ClosePort();
        _cts.Cancel();
        _cts.Dispose();
    }

}