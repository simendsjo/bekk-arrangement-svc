module kaSkjerSvc.App

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

open kaSkjerSvc.Handlers

let webApp = choose[
    EventHandlers.EventRoutes
]

let errorHandler (ex: Exception) (logger: ILogger) =
    logger.LogError(ex, "An unhandled exception has occurred while executing the request.")
    clearResponse >=> setStatusCode 500 >=> text ex.Message
    
let configureCors (builder: CorsPolicyBuilder) =
    builder.WithOrigins("http://localhost:8080").AllowAnyMethod().AllowAnyHeader() |> ignore
    
let configureApp (app: IApplicationBuilder) =
    (app.UseGiraffeErrorHandler errorHandler)
        .UseAuthentication()        
        .UseCors(configureCors)
        .UseGiraffe(webApp)
    
let configureServices (services: IServiceCollection) =
    services.AddCors() |> ignore
    services.AddGiraffe() |> ignore
    services
        .AddAuthentication(fun options ->
            options.DefaultAuthenticateScheme <- JwtBearerDefaults.AuthenticationScheme
            options.DefaultChallengeScheme <- JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(fun options ->
            let audiences = ["QHQy75S7tmnhDdBGYSnszzlhMPul0fAE"; "https://api.dev.bekk.no"]
            options.Authority <- "https://bekk-dev.eu.auth0.com"
            options.TokenValidationParameters <- TokenValidationParameters(
                ValidateIssuer = false,
                ValidAudiences = audiences))
        |> ignore

[<EntryPoint>]
let main argv =
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
    0
