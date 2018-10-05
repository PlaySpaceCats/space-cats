namespace NKN.Client
{
    public enum ErrCodes
    {
        Success
    }

    public class Const
    {
        public const int ReconnectIntervalMin = 1000;
        public const int ReconnectIntervalMax = 64000;
        public const string SeedRpcServerAddr = "http://35.234.110.177:30003";
    }
}