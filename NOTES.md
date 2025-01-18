# Notes
## Order Of Parameter Binding 
1. Parameters using [Bind/From] attributes
2. Special types like HttpContext and CancellationToken
3. Route parameters/query string
4. Services from the DI container
5. Body of the request

    [Source](https://schneidenbach.github.io/building-apis-with-csharp-and-aspnet-core/lessons/diving-into-aspnet-core/introduction-to-minimal-apis)

