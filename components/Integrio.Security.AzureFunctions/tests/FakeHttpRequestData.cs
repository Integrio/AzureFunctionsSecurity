using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Security.Claims;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace Integrio.Security.AzureFunctions.Tests;

[ExcludeFromCodeCoverage]
public class FakeHttpRequestData : HttpRequestData
{
    public FakeHttpRequestData(FunctionContext functionContext, Uri url, Stream? body = null) : base(functionContext)
    {
        Url = url;
        Body = body ?? new MemoryStream();
    }

    public override Stream Body { get; } = new MemoryStream();

    public override HttpHeadersCollection Headers { get; } = new HttpHeadersCollection();

    public override IReadOnlyCollection<IHttpCookie> Cookies => throw new NotImplementedException();

    public override Uri Url { get; }

    public override IEnumerable<ClaimsIdentity> Identities => throw new NotImplementedException();

    public override string Method => throw new NotImplementedException();

    public override HttpResponseData CreateResponse()
    {
        return new FakeHttpResponseData(FunctionContext);
    }
}