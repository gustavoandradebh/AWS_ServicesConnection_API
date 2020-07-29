using Microsoft.AspNetCore.Http;
using NFD.Domain.Exceptions;
using System;
using System.Net;
using System.Threading.Tasks;

namespace NFD.Infrastructure
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;

        public ExceptionHandlingMiddleware(RequestDelegate next)
        {
            this._next = next;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                await ExceptionHandlerAsync(context, ex);
            }
        }

        private async Task ExceptionHandlerAsync(HttpContext context, Exception exception)
        {
            var handleException = ExceptionFactoryHandler.CreateHandleException(exception);
            var responseData = handleException.GetResponseData(exception);
            context.Response.StatusCode = responseData.StatusCode;
            context.Response.ContentType = ResponseData.CONTENT_TYPE;

            await context.Response.WriteAsync(responseData.ExceptionMessage);

        }
    }

    public static class ExceptionFactoryHandler
    {
        public static IExceptionHandler CreateHandleException(Exception exception)
        {
            if (exception is CustomException)
                return new CustomExceptionHandler();

            return new ExceptionHandler();
        }
    }

    public class ResponseData
    {
        public const string CONTENT_TYPE = "application/json";

        public int StatusCode { get; set; }

        public string ExceptionMessage { get; set; }

    }

    public interface IExceptionHandler
    {
        ResponseData GetResponseData(Exception ex);
    }

    public class ExceptionHandler : IExceptionHandler
    {
        public virtual ResponseData GetResponseData(Exception ex)
        {
            var responseData = new ResponseData {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                ExceptionMessage = "Internal Server Error"
            };
            return responseData;
        }
    }

    public class CustomExceptionHandler : ExceptionHandler
    {
        public override ResponseData GetResponseData(Exception ex)
        {
            var responseData = base.GetResponseData(ex);
            var customException = ex as CustomException;
            responseData.StatusCode = (int)customException.Code;
            responseData.ExceptionMessage = ex.Message;
            return responseData;
        }
    }
}