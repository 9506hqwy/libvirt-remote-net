namespace LibvirtRemote;

public class VirtResponseTask
{
    private TaskCompletionSource<VirtResponse> source;

    public VirtResponseTask()
    {
        this.source = new TaskCompletionSource<VirtResponse>();
    }

    internal Task<VirtResponse> GetResultAsync() => this.source.Task;

    internal void SetCanceled() => this.source.SetCanceled();

    internal void SetException(Exception e) => this.source.SetException(e);

    internal void SetResult(VirtResponse response) => this.source.SetResult(response);
}
