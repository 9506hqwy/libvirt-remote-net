namespace LibvirtRemote.Test;

public class DelayMemoryStream : MemoryStream
{
    private int delayRead;

    public DelayMemoryStream(int delayRead)
        : base()
    {
        this.delayRead = delayRead;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (this.delayRead != 0)
        {
            Thread.Sleep(this.delayRead);
            this.delayRead = 0;
        }

        return base.Read(buffer, offset, count);
    }
}
