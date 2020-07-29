using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NFD.Domain.Exceptions;
using NFD.Infrastructure;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace NFD.Test.Fixtures
{
    public class ExceptionHandlingMiddlewareTest
    {
        [Fact]
        public async Task WhenANormalRequestIsMade()
        {
            // Arrange
            RequestDelegate next = (HttpContext hc) => Task.CompletedTask;
            var middleware = new ExceptionHandlingMiddleware(next);

            var context = new DefaultHttpContext();
            await AssertTest(context, middleware);

            context.Response.StatusCode
            .Should()
            .Be((int)HttpStatusCode.OK);
        }

        [Fact]
        public async Task WhenABadRequestExceptionIsRaised()
        {
            var middleware = new ExceptionHandlingMiddleware((innerHttpContext) =>
            {
                throw new BadRequestException("Bad request exception");
            });

            var context = new DefaultHttpContext();
            var streamText = await AssertTest(context, middleware);

            streamText.Should().BeEquivalentTo("Bad request exception");

            context.Response.StatusCode
            .Should()
            .Be((int)HttpStatusCode.BadRequest);
        }

        [Fact]
        public async Task WhenAnUnExpectedExceptionIsRaised()
        {
            var middleware = new ExceptionHandlingMiddleware(next: (innerHttpContext) =>
            {
                throw new Exception("Test");
            });

            var context = new DefaultHttpContext();
            var streamText = await AssertTest(context, middleware);

            streamText.Should().BeEquivalentTo("Internal Server Error");

            context.Response.StatusCode
            .Should()
            .Be((int)HttpStatusCode.InternalServerError);
        }

        private async Task<string> AssertTest(DefaultHttpContext context, ExceptionHandlingMiddleware middleware)
        {
            context.Response.Body = new MemoryStream();

            await middleware.Invoke(context);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(context.Response.Body);
            var streamText = reader.ReadToEnd();

            return streamText;
        }
    }
}
