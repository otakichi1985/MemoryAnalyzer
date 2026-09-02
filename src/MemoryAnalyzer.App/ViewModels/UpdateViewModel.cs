namespace MemoryAnalyzer.App.ViewModels;

/// <summary>上部の更新バナーが表示する状態。</summary>
public sealed class UpdateViewModel : ObservableObject
{
    private bool _hasUpdate;
    private string _message = "";
    private string _progressText = "";
    private bool _isBusy;

    public bool HasUpdate { get => _hasUpdate; set => SetProperty(ref _hasUpdate, value); }
    public string Message { get => _message; set => SetProperty(ref _message, value); }
    public string ProgressText { get => _progressText; set => SetProperty(ref _progressText, value); }
    public bool IsBusy { get => _isBusy; set => SetProperty(ref _isBusy, value); }
}
