namespace LibvirtRemote.Test;

public class DelayMemoryStream(int delayRead) : MemoryStream()
{
    private int delayRead = delayRead;

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
