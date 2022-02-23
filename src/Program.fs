﻿module ArragementService.App

open System
open System.Threading.Tasks
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
open Thoth.Json.Net

open ArrangementService
open migrator
open Logging
open SendgridApiModels

let webApp =
    choose
        [ Health.healthCheck; Event.Handlers.routes; Participant.Handlers.routes ]

let private configuration =
    let builder = ConfigurationBuilder()
    builder.AddJsonFile("appsettings.json") |> ignore
    builder.AddEnvironmentVariables() |> ignore
    builder.Build()

let configureCors (builder: CorsPolicyBuilder) =
    builder.AllowAnyMethod()
           .AllowAnyHeader()
           .AllowAnyOrigin() |> ignore

let configureApp (app: IApplicationBuilder) =
    app.Use(fun context (next: Func<Task>) ->
        context.Request.Path <- context.Request.Path.Value.Replace (configuration.["VIRTUAL_PATH"], "") |> PathString
        next.Invoke())
    |> ignore
    app.UseAuthentication()
       .UseCors(configureCors)
       .UseGiraffe(createLoggingApp webApp config)

let configureServices (services: IServiceCollection) =
    services.AddCors() |> ignore
    services.AddGiraffe() |> ignore
    services.AddSingleton<SendgridOptions>
        ({ ApiKey = configuration.["Sendgrid:Apikey"]
           SendgridUrl = configuration.["Sendgrid:SendgridUrl"] })
    |> ignore
    let config =
        { isProd = configuration.["Auth0:Scheduled_Tasks_Audience"] = "https://api.bekk.no"
          requestId = ""
          permissionsAndClaimsKey = configuration.["Auth0:Permission_Claim_Type"]
          userIdClaimsKey = configuration.["Auth0:UserId_Claim"]
          adminPermissionClaim = configuration.["Auth0:Admin_Claim"]
          readPermissionClaim = configuration.["Auth0:Read_Claim"]
          noReplyEmail = configuration.["App:NoReplyEmail"]
          sendMailInDevEnvWhiteList =
              configuration.["Sendgrid:Dev_White_List_Addresses"].Split(',')
              |> Seq.toList
              |> List.map (fun s -> s.Trim())
          databaseConnectionString = configuration.["ConnectionStrings:EventDb"]
          currentConnection = null
          currentTransaction = null
          log = []
        }
    services.AddScoped<AppConfig> (fun _ -> { config with
                                                requestId = Guid.NewGuid().GetHashCode().ToString().[1..6]
                                                currentConnection = null
                                                currentTransaction = null }) |> ignore // For å sende mail: bytt ut = med <>
    services.AddAuthentication(fun options ->
            options.DefaultAuthenticateScheme <-
                JwtBearerDefaults.AuthenticationScheme
            options.DefaultChallengeScheme <-
                JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(fun options ->
            let audiences =
                [ configuration.["Auth0:Audience"]
                  configuration.["Auth0:Scheduled_Tasks_Audience"] ]
            options.Authority <-
                sprintf "https://%s" configuration.["Auth0:Issuer_Domain"]
            options.TokenValidationParameters <-
                TokenValidationParameters
                    (ValidateIssuer = false, ValidAudiences = audiences))
    |> ignore

    let extraEncoder =
        Extra.empty |> Extra.withInt64

    services.AddSingleton<Json.ISerializer>
        (Thoth.Json.Giraffe.ThothSerializer
            (caseStrategy = CamelCase, extra = extraEncoder, skipNullField = true))
    |> ignore

    ()


[<EntryPoint>]
let main _ =
    let contentRoot = Directory.GetCurrentDirectory()
    let webRoot = Path.Combine(contentRoot, "WebRoot")

    Dapper.FSharp.OptionTypes.register()
    Migrate.Run(configuration.["ConnectionStrings:EventDb"])

    WebHostBuilder()
        .UseKestrel()
        .UseContentRoot(contentRoot)
        .UseIISIntegration()
        .UseWebRoot(webRoot)
        .Configure(Action<IApplicationBuilder> configureApp)
        .ConfigureKestrel(fun _ options -> options.AllowSynchronousIO <- true)
        .ConfigureServices(configureServices)
        .Build()
        .Run()
    0
