module arrangementSvc.App

open System
open Giraffe
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Microsoft.AspNetCore.Hosting
open System.IO
open Microsoft.IdentityModel.Tokens
open Microsoft.Extensions.Configuration
open Microsoft.AspNetCore.Http

open arrangementSvc.Handlers
open arrangementSvc.Database

// let webApp = choose [ EventHandlers.EventRoutes; Health.healthCheck ]

let webApp =
    choose [
        GET >=>
            choose [
                route "/" >=> text "world"
                route "/hello" >=> text "hello you"
                route "/health" >=> text "hello you"
            ]
        setStatusCode 404 >=> text "Not Found" ]


let private configuration =
    let builder = ConfigurationBuilder()
    builder.AddJsonFile("appsettings.json") |> ignore
    builder.AddEnvironmentVariables() |> ignore
    builder.Build()

let errorHandler (ex: Exception) (logger: ILogger) =
    logger.LogError(ex, "An unhandled exception has occurred while executing the request.")
    clearResponse >=> setStatusCode 500 >=> text ex.Message

let configureCors (builder : CorsPolicyBuilder) =
    builder.WithOrigins("http://localhost:8080")
           .AllowAnyMethod()
           .AllowAnyHeader()
           |> ignore

let configureApp (app: IApplicationBuilder) =
    app.Use(fun context next -> 
        context.Request.Path <- context.Request.Path.Value.Replace(configuration.["VIRTUAL_PATH"], "")  |> PathString
        next.Invoke()) |> ignore
    (app.UseGiraffeErrorHandler errorHandler)
        .UseCors(configureCors)
        .UseStaticFiles()
        .UseGiraffe(webApp)

let configureServices (services: IServiceCollection) =
    services.AddSingleton<ArrangementDbContext>(createDbContext configuration.["ConnectionStrings:EventDb"]) |> ignore
    services.AddCors() |> ignore
    services.AddGiraffe() |> ignore


[<EntryPoint>]
let main _ =
    let contentRoot = Directory.GetCurrentDirectory()
    let webRoot = Path.Combine(contentRoot, "WebRoot")
    WebHostBuilder()
        .UseKestrel()
        .UseContentRoot(contentRoot)
        .UseIISIntegration()
        .UseWebRoot(webRoot)
        .Configure(Action<IApplicationBuilder> configureApp)
        .ConfigureServices(configureServices)
        .Build()
        .Run()

    // TODO: FIX
    let foo = createDbContext ConnectionString
    foo.SaveContextSchema() |> ignore
    0
    