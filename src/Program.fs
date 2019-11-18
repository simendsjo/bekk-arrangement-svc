module arrangementSvc.App

open System
open Giraffe
open System.IO
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Builder
open Microsoft.IdentityModel.Tokens
open Microsoft.Extensions.Configuration
open Microsoft.AspNetCore.Cors.Infrastructure 
open Microsoft.Extensions.DependencyInjection
open Microsoft.AspNetCore.Authentication.JwtBearer

open arrangementSvc.Handlers
open arrangementSvc.Database
open arrangementSvc.Logging

let webApp = choose [ EventHandlers.EventRoutes; Health.healthCheck ]

let private configuration =
    let builder = ConfigurationBuilder()
    builder.AddJsonFile("appsettings.json") |> ignore
    builder.AddEnvironmentVariables() |> ignore
    builder.Build()

let configureCors (builder: CorsPolicyBuilder) =
    builder.AllowAnyMethod().AllowAnyHeader() |> ignore

let configureApp (app: IApplicationBuilder) =
    app.Use(fun context next -> 
        context.Request.Path <- context.Request.Path.Value.Replace(configuration.["VIRTUAL_PATH"], "")  |> PathString
        next.Invoke()) |> ignore
    app.UseAuthentication()
       .UseCors(configureCors)
       .UseGiraffe(createLoggingApp webApp config)

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
        .Configure(Action<IApplicationBuilder> configureApp)
        .ConfigureServices(configureServices)
        .Build()
        .Run()

    // TODO: FIX
    let foo = createDbContext ConnectionString
    foo.SaveContextSchema() |> ignore
    0