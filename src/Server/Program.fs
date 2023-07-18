module FunPizzaShop.Server.App

open System
open System.IO
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Cors.Infrastructure
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.DependencyInjection
open Giraffe
open Giraffe.SerilogExtensions
open Microsoft.AspNetCore.SpaServices.ReactDevelopmentServer
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Configuration
open Serilog
open Hocon.Extensions.Configuration
open ThrottlingTroll
open FunPizzaShop.Server.Views
open FunPizzaShop.Server.Handlers.Default
open Http
open System.Globalization

// every server side project - FIRST THING - set the Culture
// e.g. see turkish dotless i vs english i (also azerbaijani)
// also, comma vs dot separators in your config files.
// e.g. 32.52 to double.Parse doesn't throw and becomes 3 thousand 252 seconds.
CultureInfo.DefaultThreadCurrentCulture <- CultureInfo.InvariantCulture
CultureInfo.DefaultThreadCurrentUICulture <- CultureInfo.InvariantCulture

// SECOND THING
// use our bootstrap logger so that we get logs even for startup failures
bootstrapLogger()

type Self = Self

let errorHandler (ex: Exception) (ctx: HttpContext) =
    Log.Error(ex, "Error Handler")
    match ex with
    | :? System.Text.Json.JsonException -> clearResponse >=> setStatusCode 400 >=> text ex.Message
    | _ -> clearResponse >=> setStatusCode 500 >=> text ex.Message

// cors, we follow the giraffe template.
// cors, it's not a restriction, it's a relaxation! by default, browsers are not allowed to to CORS requests.

let configureCors (builder: CorsPolicyBuilder) =
    #if DEBUG
    builder
        .WithOrigins("http://localhost:5010", "https://localhost:5011")
        .AllowAnyMethod()
        .AllowAnyHeader()
    |> ignore
    #else
        ()
    #endif

// the typical asp.net core configure app + our dependency injection via `appEnv`
let configureApp (app: IApplicationBuilder, appEnv) =
    let env = app.ApplicationServices.GetService<IWebHostEnvironment>()
    let isDevelopment = env.IsDevelopment()


    let app = if isDevelopment then app else app.UseResponseCompression()

    app
        .UseDefaultFiles()
        .UseAuthentication()
        .UseAuthorization()
        .UseMiddleware<LogUserNameMiddleware>()
        .Use(headerMiddleware)
    |> ignore

    let layout ctx = Layout.view ctx (appEnv) (env.IsDevelopment())
    let webApp  =
            webAppWrapper appEnv layout
    let sConfig = Serilog.configure errorHandler
    let handler = SerilogAdapter.Enable(webApp, sConfig)

    (match isDevelopment with
     | true -> app.UseDeveloperExceptionPage()
     | false -> app.UseHttpsRedirection())
        .UseCors(configureCors)
        .UseStaticFiles(staticFileOptions)
        // TODO add config for this to hocon.config
        // .UseThrottlingTroll(Throttling.setOptions)
        .UseWebSockets()
        .UseGiraffe(handler)

    if env.IsDevelopment() then
        app.UseSpa(fun spa ->
            let path = System.IO.Path.Combine(__SOURCE_DIRECTORY__, "../../.")
            spa.Options.SourcePath <- path
            spa.Options.DevServerPort <- 5173
            spa.UseReactDevelopmentServer(npmScript = "watch"))

        app.UseSerilogRequestLogging() |> ignore

// standard asp.net core configure services
let configureServices (services: IServiceCollection) =
    services
        .AddAuthorization()
        .AddResponseCompression(fun options -> options.EnableForHttps <- true)
        .AddCors()
        .AddGiraffe()
        .AddAntiforgery()
        .AddApplicationInsightsTelemetry()
        .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(
            CookieAuthenticationDefaults.AuthenticationScheme,
            fun options ->
                options.SlidingExpiration <- true
                // in iOS, Onur doesn't see this work right, but maybe something he is doing wrong
                options.ExpireTimeSpan <- TimeSpan.FromDays(7)
        )
    |> ignore

let configureLogging (builder: ILoggingBuilder) =
    builder.AddConsole().AddDebug() |> ignore

// host is setup in 2 steps because we have automation tests.
// also standard asp.net core setup; no f# specifics here
let host appEnv args =
    let contentRoot = Directory.GetCurrentDirectory()
    let webRoot = Path.Combine(contentRoot, "WebRoot")
    let host =
        Host
            .CreateDefaultBuilder(args)
            .UseSerilog(Serilog.configureMiddleware)
            .ConfigureWebHostDefaults(fun webHostBuilder ->
                webHostBuilder
    #if !DEBUG
                    .UseEnvironment(Environments.Production)
    #else
                    .UseEnvironment(Environments.Development)
    #endif
                    .UseContentRoot(contentRoot)
                    .UseWebRoot(webRoot)
                    .Configure(Action<IApplicationBuilder> (fun builder -> configureApp (builder, appEnv)))
                    .ConfigureServices(configureServices)
                    .ConfigureLogging(configureLogging)
                |> ignore)
            .Build()
    host

[<EntryPoint>]
let main args =

    let configBuilder =
        ConfigurationBuilder()
            .AddUserSecrets<Self>()
            .AddHoconFile("config.hocon", false)
            .AddHoconFile("secrets.hocon", true)
            .AddEnvironmentVariables()

    let config = configBuilder.Build()

    let mutable ret = 0

    let appEnv = new Environments.AppEnv(config)

    try
        try
            (host appEnv args).Run()
        with ex ->
            Log.Fatal(ex, "Host terminated unexpectedly")
            ret <- -1
    finally
        Log.CloseAndFlush()
    ret
