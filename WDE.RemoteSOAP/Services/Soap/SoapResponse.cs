namespace WDE.RemoteSOAP.Services.Soap
{
    public readonly struct SoapResponse
    {
        public readonly bool Success;
        public readonly string Message;

        public SoapResponse(bool success, string message)
        {
            Success = success;
            Message = message;
        }
    }
}