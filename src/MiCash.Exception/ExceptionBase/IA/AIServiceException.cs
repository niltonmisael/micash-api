using MiCash.Exception.ExceptionBase;

public abstract class AIServiceException : MiCashException
{
    protected AIServiceException ( string message )
            : base(message)
    {
    }

    public override List<string> GetErrors ()
    {
        return
        [
            Message
        ];
    }
}