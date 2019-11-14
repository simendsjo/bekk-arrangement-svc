module arrangementSvc.App

open System
open Giraffe
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Logging
open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.AspNetCore.Hosting
open System.IO
open Microsoft.IdentityModel.Tokens
open Microsoft.Extensions.Configuration
open Microsoft.AspNetCore.Http

open arrangementSvc.Handlers
open arrangementSvc.Database

let webApp = choose [ EventHandlers.EventRoutes; Health.healthCheck ]

let private configuration =
    let builder = ConfigurationBuilder()
    builder.AddJsonFile("appsettings.json") |> ignore
    builder.AddEnvironmentVariables() |> ignore
    builder.Build()

let errorHandler (ex: Exception) (logger: ILogger) =
    logger.LogError(ex, "An unhandled exception has occurred while executing the request.")
    clearResponse >=> setStatusCode 500 >=> text ex.Message

let configureCors (builder: CorsPolicyBuilder) =
    builder.WithOrigins("http://localhost:8080").AllowAnyMethod().AllowAnyHeader() |> ignore

let configureApp (app: IApplicationBuilder) =
    app.Use(fun context next -> 
        context.Request.Path <- context.Request.Path.Value.Replace(configuration.["VIRTUAL_PATH"], "")  |> PathString
        next.Invoke()) |> ignore
    (app.UseGiraffeErrorHandler errorHandler)
        .UseAuthentication()
        .UseCors(configureCors)
        .UseGiraffe(webApp)

let configureServices (services: IServiceCollection) =
    services.AddCors() |> ignore
    services.AddGiraffe() |> ignore
    services.AddSingleton<ArrangementDbContext>(createDbContext configuration.["ConnectionStrings:EventDb"]) |> ignore
    services.AddAuthentication(fun options ->
            options.DefaultAuthenticateScheme <- JwtBearerDefaults.AuthenticationScheme
            options.DefaultChallengeScheme <- JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(fun options ->
            let audiences =
                [ configuration.["Auth0:Audience"]
                  configuration.["Auth0:Scheduled_Tasks_Audience"] ]
            options.Authority <- sprintf "https://%s" configuration.["Auth0:Issuer_Domain"]
            options.TokenValidationParameters <-
                TokenValidationParameters(ValidateIssuer = false, ValidAudiences = audiences))
    |> ignore

[<EntryPoint>]
let main _ =
    let contentRoot = Directory.GetCurrentDirectory()
    let webRoot = Path.Combine(contentRoot, "WebRoot")
    WebHostBuilder()
        .UseKestrel()
        .UseContentRoot(contentRoot)
        .UseIISIntegration()
        .UseWebRoot(webRoot)
        .UseUrls(sprintf "http://0.0.0.0:%s/" configuration.["PORT"])
        .Configure(Action<IApplicationBuilder> configureApp)
        .ConfigureServices(configureServices)
        .Build()
        .Run()

    // TODO: FIX
    let foo = createDbContext ConnectionString
    foo.SaveContextSchema() |> ignore
    0
    