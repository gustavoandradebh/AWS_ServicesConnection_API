using System;
using System.Net;

namespace NFD.Domain.Exceptions
{
    [Serializable]
    public class BadRequestException : CustomException
    {
        public BadRequestException(string message)
           : base(HttpStatusCode.BadRequest, message)
        {
        }
    }
}
