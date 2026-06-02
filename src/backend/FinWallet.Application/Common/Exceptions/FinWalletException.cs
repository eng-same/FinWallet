namespace FinWallet.Application.Common.Exceptions;

public class FinWalletException : Exception
{
    public string Code { get; }

    public FinWalletException(string code, string message) : base(message)
    {
        Code = code;
    }
}
