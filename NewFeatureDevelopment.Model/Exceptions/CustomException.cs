using System;
using System.Net;

namespace NFD.Domain.Exceptions
{
    [Serializable]
    public class CustomException : Exception
    {
        public CustomException(HttpStatusCode code, string message)
            : base(message)
        {
            Code = code;
        }

        public HttpStatusCode Code { get; private set; }
    }
}
