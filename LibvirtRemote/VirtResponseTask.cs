namespace LibvirtRemote;

public class VirtResponseTask
{
    private TaskCompletionSource<VirtResponse> source;

    public VirtResponseTask()
    {
        // https://devblogs.microsoft.com/premier-developer/the-danger-of-taskcompletionsourcet-class/
        this.source = new TaskCompletionSource<VirtResponse>(
            TaskCreationOptions.RunContinuationsAsynchronously);
    }

    internal bool IsCompleted => this.source.Task.IsCompleted;

    internal Task<VirtResponse> GetResultAsync() => this.source.Task;

    internal void SetCanceled() => this.source.SetCanceled();

    internal void SetException(Exception e) => this.source.SetException(e);

    internal void SetResult(VirtResponse response) => this.source.SetResult(response);
}
