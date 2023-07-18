module FunPizzaShop.Automation.Setup

open Hocon.Extensions.Configuration
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Hosting
open Microsoft.Playwright
open TickSpec

open System.IO

open FunPizzaShop.Server

let configBuilder =
    ConfigurationBuilder()
        .AddHoconFile("test-config.hocon")
        .AddEnvironmentVariables()

let config = configBuilder.Build()

Directory.SetCurrentDirectory("/workspaces/MyFunPizzaShop/src/Server")

let appEnv = Environments.AppEnv(config)

// use the same host as our Server but with test config and test AppEnv
let host = App.host appEnv [||]

host.Start()

let playwright = Playwright.CreateAsync().Result
let browser = playwright.Chromium.LaunchAsync().Result


// nunit/xunit setup for each test
[<BeforeScenario>]
let setupContext() =
    let context = browser.NewContextAsync(BrowserNewContextOptions(IgnoreHTTPSErrors = true)).Result

    // by returning this value to TickSpec, the value is available for any other TickSpec
    // integration that takes it as a function parameter by some internal dependency injection tracking
    context

// nunit/xunit teardown for each test
[<AfterScenario>]
let afterContext () =
    appEnv.Reset()

