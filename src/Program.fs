module ArragementService.App

open System
open Giraffe
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open System.IO
open Microsoft.Extensions.Configuration
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.IdentityModel.Tokens
open Microsoft.AspNetCore.Hosting

open ArrangementService
open ArrangementService.Email.SendgridApiModels
open migrator
open Database
open Logging

let webApp = choose [ Event.Handlers.routes; Health.healthCheck; Participant.Handlers.routes ]

let private configuration =
    let builder = ConfigurationBuilder()
    builder.AddJsonFile("appsettings.json") |> ignore
    builder.AddEnvironmentVariables() |> ignore
    builder.Build()

let configureCors (builder: CorsPolicyBuilder) = builder.AllowAnyMethod().AllowAnyHeader().AllowAnyOrigin() |> ignore

let configureApp (app: IApplicationBuilder) =
    app.Use(fun context next ->
        context.Request.Path <- context.Request.Path.Value.Replace(configuration.["VIRTUAL_PATH"], "") |> PathString
        next.Invoke())
    |> ignore
    app.UseAuthentication().UseCors(configureCors).UseGiraffe(createLoggingApp webApp config)

let configureServices (services: IServiceCollection) =
    services.AddCors() |> ignore
    services.AddGiraffe() |> ignore
    let dbContext = createDbContext configuration.["ConnectionStrings:EventDb"]
    services.AddSingleton<ArrangementDbContext>(dbContext) |> ignore
    services.AddSingleton<SendgridOptions>
        ({ ApiKey = configuration.["Sendgrid:Apikey"]
           SendgridUrl = configuration.["Sendgrid:SendgridUrl"] })
    |> ignore
    services.AddSingleton<AppConfig>
        ({ isProd = configuration.["Auth0:Scheduled_Tasks_Audience"] = "https://api.bekk.no" }) |> ignore  // For å sende mail: bytt ut = med <>
    dbContext.SaveContextSchema() |> ignore
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
    Migrate.Run(configuration.["ConnectionStrings:EventDb"])

    WebHostBuilder()
        .UseKestrel()
        .UseContentRoot(contentRoot)
        .UseIISIntegration()
        .UseWebRoot(webRoot)
        .Configure(Action<IApplicationBuilder> configureApp)
        .ConfigureKestrel(fun context options -> options.AllowSynchronousIO <- true)
        .ConfigureServices(configureServices)
        .Build()
        .Run()
    0
