This basic solution, attempts to highlight a difference in the handling of Cosmos resources under Aspire between normal web api projects and Azure function projects.

The Cosmos db, along with a database and container are defined within the app host as follows:

```#pragma warning disable ASPIRECOSMOSDB001

var cosmos = builder.AddAzureCosmosDB("cosmosdb").RunAsPreviewEmulator(
                     emulator =>
                     {
                         emulator.WithDataExplorer();
                     });

var database = cosmos.AddCosmosDatabase("database");
var container = database.AddContainer("container", "/id");
```
container is added as a reference to both our apiService and our azure functions project:
```
var apiService = builder.AddProject<Projects.AspireCosmosDb_ApiService>("apiservice").
                    WithReference(container);


builder.AddProject<Projects.AspireCosmosDb_Web>("webfrontend")
    .WithExternalHttpEndpoints()
    .WithReference(apiService)
    .WaitFor(apiService);

builder.AddAzureFunctionsProject<Projects.AspireCosmosDb_Functions>("aspirecosmosdb-functions").
    WithReference(container).
    WithExternalHttpEndpoints();
```


The ApiService references the container with a

```
// add the cosmos container
builder.AddAzureCosmosContainer("container");
```

and, it is added as a parameter to the app.MapGet call:
```
app.MapGet("/weatherforecast", (Microsoft.Azure.Cosmos.Container container) =>
{
    var forecast = Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");
```
The invocation of the '/weatherforecast' executes without error.

With an Azure Functions project however...

We add this in the program.cs
```
// add the cosmos container
builder.AddAzureCosmosContainer("container");
```
and in the Function1.cs we change it to inject the container as a constructor parameter
```
    public class Function1
    {
        private readonly ILogger<Function1> _logger;

        public Function1(ILogger<Function1> logger,Microsoft.Azure.Cosmos.Container container)
        {
            _logger = logger;
        }

        [Function("Function1")]
        public IActionResult Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            return new OkObjectResult("Welcome to Azure Functions!");
        }
    }
```
We get an exception when we try and trigger the Function1, along the lines of :
```
[2025-06-11T13:58:45.846Z] Host lock lease acquired by instance ID '0000000000000000000000007244AF65'.
[2025-06-11T13:58:47.523Z] Executing 'Functions.Function1' (Reason='This function was programmatically called via the host APIs.', Id=a71b30e7-004d-4156-93c4-7b793b239074)
[2025-06-11T13:58:48.073Z] Function 'Function1', Invocation id 'a71b30e7-004d-4156-93c4-7b793b239074': An exception was thrown by the invocation.
[2025-06-11T13:58:48.074Z] Result: Function 'Function1', Invocation id 'a71b30e7-004d-4156-93c4-7b793b239074': An exception was thrown by the invocation.
Exception: System.InvalidOperationException: The connection string 'container' does not exist or is missing the container name or database name.
[2025-06-11T13:58:48.076Z]    at Microsoft.Extensions.Hosting.AspireMicrosoftAzureCosmosExtensions.<>c__DisplayClass2_0.<AddAzureCosmosContainer>b__0(IServiceProvider sp) in /_/src/Components/Aspire.Microsoft.Azure.Cosmos/AspireMicrosoftAzureCosmosExtensions.cs:line 71
[2025-06-11T13:58:48.080Z]    at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitFactory(FactoryCallSite factoryCallSite, RuntimeResolverContext context)
[2025-06-11T13:58:48.081Z]    at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteVisitor`2.VisitCallSiteMain(ServiceCallSite callSite, TArgument argument)
[2025-06-11T13:58:48.083Z]    at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitRootCache(ServiceCallSite callSite, RuntimeResolverContext context)
[2025-06-11T13:58:48.085Z]    at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteVisitor`2.VisitCallSite(ServiceCallSite callSite, TArgument argument)
[2025-06-11T13:58:48.087Z]    at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.Resolve(ServiceCallSite callSite, ServiceProviderEngineScope scope)
[2025-06-11T13:58:48.097Z]    at Microsoft.Extensions.DependencyInjection.ServiceProvider.CreateServiceAccessor(ServiceIdentifier serviceIdentifier)
[2025-06-11T13:58:48.102Z]    at System.Collections.Concurrent.ConcurrentDictionary`2.GetOrAdd(TKey key, Func`2 valueFactory)
[2025-06-11T13:58:48.108Z]    at Microsoft.Extensions.DependencyInjection.ServiceProvider.GetService(ServiceIdentifier serviceIdentifier, ServiceProviderEngineScope serviceProviderEngineScope)
[2025-06-11T13:58:48.113Z]    at Microsoft.Extensions.DependencyInjection.ServiceLookup.ServiceProviderEngineScope.GetService(Type serviceType)
[2025-06-11T13:58:48.120Z]    at Microsoft.Extensions.DependencyInjection.ActivatorUtilities.ConstructorInfoEx.GetService(IServiceProvider serviceProvider, Int32 parameterIndex)
[2025-06-11T13:58:48.127Z]    at Microsoft.Extensions.DependencyInjection.ActivatorUtilities.ConstructorMatcher.CreateInstance(IServiceProvider provider)
[2025-06-11T13:58:48.132Z]    at Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateInstance(IServiceProvider provider, Type instanceType, Object[] parameters)
[2025-06-11T13:58:48.136Z]    at Microsoft.Azure.Functions.Worker.DefaultFunctionActivator.CreateInstance(Type instanceType, FunctionContext context) in D:\a\_work\1\s\src\DotNetWorker.Core\Invocation\DefaultFunctionActivator.cs:line 23
[2025-06-11T13:58:48.138Z]    at AspireCosmosDb.Functions.DirectFunctionExecutor.ExecuteAsync(FunctionContext context) in C:\Users\jamie\source\repos\AspireCosmosDb\AspireCosmosDb.Functions\obj\Debug\net8.0\Microsoft.Azure.Functions.Worker.Sdk.Generators\Microsoft.Azure.Functions.Worker.Sdk.Generators.FunctionExecutorGenerator\GeneratedFunctionExecutor.g.cs:line 37
[2025-06-11T13:58:48.140Z]    at Microsoft.Azure.Functions.Worker.OutputBindings.OutputBindingsMiddleware.Invoke(FunctionContext context, FunctionExecutionDelegate next) in D:\a\_work\1\s\src\DotNetWorker.Core\OutputBindings\OutputBindingsMiddleware.cs:line 13
[2025-06-11T13:58:48.143Z]    at Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore.FunctionsHttpProxyingMiddleware.Invoke(FunctionContext context, FunctionExecutionDelegate next) in D:\a\_work\1\s\extensions\Worker.Extensions.Http.AspNetCore\src\FunctionsMiddleware\FunctionsHttpProxyingMiddleware.cs:line 54
[2025-06-11T13:58:48.146Z]    at Microsoft.Azure.Functions.Worker.FunctionsApplication.InvokeFunctionAsync(FunctionContext context) in D:\a\_work\1\s\src\DotNetWorker.Core\FunctionsApplication.cs:line 96
Stack:    at Microsoft.Extensions.Hosting.AspireMicrosoftAzureCosmosExtensions.<>c__DisplayClass2_0.<AddAzureCosmosContainer>b__0(IServiceProvider sp) in /_/src/Components/Aspire.Microsoft.Azure.Cosmos/AspireMicrosoftAzureCosmosExtensions.cs:line 71
[2025-06-11T13:58:48.149Z]    at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitFactory(FactoryCallSite factoryCallSite, RuntimeResolverContext context)
[2025-06-11T13:58:48.150Z]    at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteVisitor`2.VisitCallSiteMain(ServiceCallSite callSite, TArgument argument)
[2025-06-11T13:58:48.156Z]    at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitRootCache(ServiceCallSite callSite, RuntimeResolverContext context)
[2025-06-11T13:58:48.158Z]    at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteVisitor`2.VisitCallSite(ServiceCallSite callSite, TArgument argument)
[2025-06-11T13:58:48.161Z]    at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.Resolve(ServiceCallSite callSite, ServiceProviderEngineScope scope)
[2025-06-11T13:58:48.164Z]    at Microsoft.Extensions.DependencyInjection.ServiceProvider.CreateServiceAccessor(ServiceIdentifier serviceIdentifier)
[2025-06-11T13:58:48.167Z]    at System.Collections.Concurrent.ConcurrentDictionary`2.GetOrAdd(TKey key, Func`2 valueFactory)
[2025-06-11T13:58:48.168Z]    at Microsoft.Extensions.DependencyInjection.ServiceProvider.GetService(ServiceIdentifier serviceIdentifier, ServiceProviderEngineScope serviceProviderEngineScope)
[2025-06-11T13:58:48.170Z]    at Microsoft.Extensions.DependencyInjection.ServiceLookup.ServiceProviderEngineScope.GetService(Type serviceType)
[2025-06-11T13:58:48.171Z]    at Microsoft.Extensions.DependencyInjection.ActivatorUtilities.ConstructorInfoEx.GetService(IServiceProvider serviceProvider, Int32 parameterIndex)
[2025-06-11T13:58:48.172Z]    at Microsoft.Extensions.DependencyInjection.ActivatorUtilities.ConstructorMatcher.CreateInstance(IServiceProvider provider)
[2025-06-11T13:58:48.174Z]    at Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateInstance(IServiceProvider provider, Type instanceType, Object[] parameters)
[2025-06-11T13:58:48.175Z]    at Microsoft.Azure.Functions.Worker.DefaultFunctionActivator.CreateInstance(Type instanceType, FunctionContext context) in D:\a\_work\1\s\src\DotNetWorker.Core\Invocation\DefaultFunctionActivator.cs:line 23
[2025-06-11T13:58:48.176Z]    at AspireCosmosDb.Functions.DirectFunctionExecutor.ExecuteAsync(FunctionContext context) in C:\Users\jamie\source\repos\AspireCosmosDb\AspireCosmosDb.Functions\obj\Debug\net8.0\Microsoft.Azure.Functions.Worker.Sdk.Generators\Microsoft.Azure.Functions.Worker.Sdk.Generators.FunctionExecutorGenerator\GeneratedFunctionExecutor.g.cs:line 37
[2025-06-11T13:58:48.178Z]    at Microsoft.Azure.Functions.Worker.OutputBindings.OutputBindingsMiddleware.Invoke(FunctionContext context, FunctionExecutionDelegate next) in D:\a\_work\1\s\src\DotNetWorker.Core\OutputBindings\OutputBindingsMiddleware.cs:line 13
[2025-06-11T13:58:48.179Z]    at Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore.FunctionsHttpProxyingMiddleware.Invoke(FunctionContext context, FunctionExecutionDelegate next) in D:\a\_work\1\s\extensions\Worker.Extensions.Http.AspNetCore\src\FunctionsMiddleware\FunctionsHttpProxyingMiddleware.cs:line 54
[2025-06-11T13:58:48.181Z]    at Microsoft.Azure.Functions.Worker.FunctionsApplication.InvokeFunctionAsync(FunctionContext context) in D:\a\_work\1\s\src\DotNetWorker.Core\FunctionsApplication.cs:line 96.
[2025-06-11T13:58:48.213Z] Executed 'Functions.Function1' (Failed, Id=a71b30e7-004d-4156-93c4-7b793b239074, Duration=720ms)
[2025-06-11T13:58:48.217Z] System.Private.CoreLib: Exception while executing function: Functions.Function1. System.Private.CoreLib: Result: Failure
Exception: The connection string 'container' does not exist or is missing the container name or database name.
Stack:    at Microsoft.Extensions.Hosting.AspireMicrosoftAzureCosmosExtensions.<>c__DisplayClass2_0.<AddAzureCosmosContainer>b__0(IServiceProvider sp) in /_/src/Components/Aspire.Microsoft.Azure.Cosmos/AspireMicrosoftAzureCosmosExtensions.cs:line 71
[2025-06-11T13:58:48.219Z]    at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitFactory(FactoryCallSite factoryCallSite, RuntimeResolverContext context)
[2025-06-11T13:58:48.221Z]    at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteVisitor`2.VisitCallSiteMain(ServiceCallSite callSite, TArgument argument)
[2025-06-11T13:58:48.222Z]    at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.VisitRootCache(ServiceCallSite callSite, RuntimeResolverContext context)
[2025-06-11T13:58:48.224Z]    at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteVisitor`2.VisitCallSite(ServiceCallSite callSite, TArgument argument)
[2025-06-11T13:58:48.226Z]    at Microsoft.Extensions.DependencyInjection.ServiceLookup.CallSiteRuntimeResolver.Resolve(ServiceCallSite callSite, ServiceProviderEngineScope scope)
[2025-06-11T13:58:48.228Z]    at Microsoft.Extensions.DependencyInjection.ServiceProvider.CreateServiceAccessor(ServiceIdentifier serviceIdentifier)
[2025-06-11T13:58:48.229Z]    at System.Collections.Concurrent.ConcurrentDictionary`2.GetOrAdd(TKey key, Func`2 valueFactory)
[2025-06-11T13:58:48.231Z]    at Microsoft.Extensions.DependencyInjection.ServiceProvider.GetService(ServiceIdentifier serviceIdentifier, ServiceProviderEngineScope serviceProviderEngineScope)
[2025-06-11T13:58:48.232Z]    at Microsoft.Extensions.DependencyInjection.ServiceLookup.ServiceProviderEngineScope.GetService(Type serviceType)
[2025-06-11T13:58:48.234Z]    at Microsoft.Extensions.DependencyInjection.ActivatorUtilities.ConstructorInfoEx.GetService(IServiceProvider serviceProvider, Int32 parameterIndex)
[2025-06-11T13:58:48.235Z]    at Microsoft.Extensions.DependencyInjection.ActivatorUtilities.ConstructorMatcher.CreateInstance(IServiceProvider provider)
[2025-06-11T13:58:48.236Z]    at Microsoft.Extensions.DependencyInjection.ActivatorUtilities.CreateInstance(IServiceProvider provider, Type instanceType, Object[] parameters)
[2025-06-11T13:58:48.237Z]    at Microsoft.Azure.Functions.Worker.DefaultFunctionActivator.CreateInstance(Type instanceType, FunctionContext context) in D:\a\_work\1\s\src\DotNetWorker.Core\Invocation\DefaultFunctionActivator.cs:line 23
[2025-06-11T13:58:48.238Z]    at AspireCosmosDb.Functions.DirectFunctionExecutor.ExecuteAsync(FunctionContext context) in C:\Users\jamie\source\repos\AspireCosmosDb\AspireCosmosDb.Functions\obj\Debug\net8.0\Microsoft.Azure.Functions.Worker.Sdk.Generators\Microsoft.Azure.Functions.Worker.Sdk.Generators.FunctionExecutorGenerator\GeneratedFunctionExecutor.g.cs:line 37
[2025-06-11T13:58:48.240Z]    at Microsoft.Azure.Functions.Worker.OutputBindings.OutputBindingsMiddleware.Invoke(FunctionContext context, FunctionExecutionDelegate next) in D:\a\_work\1\s\src\DotNetWorker.Core\OutputBindings\OutputBindingsMiddleware.cs:line 13
[2025-06-11T13:58:48.241Z]    at Microsoft.Azure.Functions.Worker.Extensions.Http.AspNetCore.FunctionsHttpProxyingMiddleware.Invoke(FunctionContext context, FunctionExecutionDelegate next) in D:\a\_work\1\s\extensions\Worker.Extensions.Http.AspNetCore\src\FunctionsMiddleware\FunctionsHttpProxyingMiddleware.cs:line 54
[2025-06-11T13:58:48.242Z]    at Microsoft.Azure.Functions.Worker.FunctionsApplication.InvokeFunctionAsync(FunctionContext context) in D:\a\_work\1\s\src\DotNetWorker.Core\FunctionsApplication.cs:line 96
[2025-06-11T13:58:48.244Z]    at Microsoft.Azure.Functions.Worker.Handlers.InvocationHandler.InvokeAsync(InvocationRequest request) in D:\a\_work\1\s\src\DotNetWorker.Grpc\Handlers\InvocationHandler.cs:line 89.
```

 
