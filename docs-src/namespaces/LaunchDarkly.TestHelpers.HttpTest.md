A simple portable HTTP test server with response mocking.

This namespace provides a simple abstraction for setting up embedded HTTP test servers that return programmed responses, and verifying that the expected requests have been made in tests.

The underlying implementation is `System.Net.HttpListener`. There are other HTTP mock server libraries for .NET, most of which also use `System.Net.HttpListener` either directly or indirectly, but they were generally not suitable for LaunchDarkly's testing needs due to either platform compatibility limitations or their API design.

An <xref:LaunchDarkly.TestHelpers.HttpTest.HttpServer> is an HTTP server that starts listening on an arbitrarily chosen port as soon as you create it. You should normally do this inside a `using` block to ensure that the server is shut down when you're done with it. The server's <xref:LaunchDarkly.TestHelpers.HttpTest.HttpServer.Uri> property gives you the address for making your test requests.

You configure the server with a single <xref:LaunchDarkly.TestHelpers.HttpTest.Handler> that receives all requests. The library provides a variety of handler implementations and combinators.

## Examples

### Invariant response with error status

```csharp
    var server = HttpServer.Start(Handlers.Status(500));
```

### Invariant response with status, headers, and body

```csharp
    var server = HttpServer.Start(
        Handlers.Status(202)
            .Then(Handlers.Header("Etag", "123"))
            .Then(Handlers.BodyString("text/plain", "thanks"))
    );
```

### Verifying requests made to the server

```csharp
    using (var server = HttpServer.Start(Handlers.Status(200)))
    {
        DoSomethingThatMakesARequest(server.Uri);
        DoSomethingElseThatMakesARequest(server.Uri);

        var request1 = server.Recorder.RequireRequest();
        Assert.Equals("/path1", request1.Path);

        var request2 = server.Recorder.RequireRequest();
        Assert.Equals("/path2", request2.Path);
    }
```

### Response with custom logic depending on the request

```csharp
    var server = HttpServer.Start(
        async ctx =>
        {
            if (ctx.RequestInfo.Headers.Get("Header-Name") == "good-value")
            {
                await Handlers.Status(200)(ctx);
            }
            else
            {
                await Handlers.status(400)(ctx);
            }
        }
    );
```

### Simple routing to simulate two endpoints

```csharp
    var server = HttpServer.Start(Handlers.Router(out var router));
    router.AddPath("/path1", Handlers.Status(200));
    router.AddPath("/path2", Handlers.Status(500));
```

### Programmed sequence of responses

```csharp
    var server = HttpServer.Start(
        Handlers.Sequential(
            Handlers.Status(200), // first request gets a 200
            Handlers.Status(500)  // next request gets a 500
        )
    );
```

### Changing server behavior during a test

```csharp
    using (var server = HttpServer.Start(Handlers.Switchable(out var switcher)))
    {
        switcher.Target = Handlers.Status(200);
        // Now the server returns 200 for all requests

        switcher.Target = Handlers.Status(500);
        // Now the server returns 500 for all requests
    }
```

### Using a customized HttpClient instead of a server

```csharp
    var handler = Handlers.Record(out var recorder)
        .Then(Handlers.BodyString("text/plain", "hello"));
    var client = new HttpClient(handler.AsMessageHandler());
    // Now all requests made with this client to any URI will receive the canned
    // response, and can be inspected with the recorder.
```
