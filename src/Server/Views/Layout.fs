module FunPizzaShop.Server.Views.Layout

let inline private html (s: string) = s

open System
open System.IO
open Microsoft.AspNetCore.Http
open Microsoft.AspNetCore.Http.Extensions
open System.Threading.Tasks
open Common

// any js files in this folder (should be for production only)
let scriptFiles =
    let assetsDir = "WebRoot/dist/assets"

    if Directory.Exists assetsDir then
        Directory.GetFiles(assetsDir, "*.js", SearchOption.AllDirectories)
    else
        [||]

// get the relevant portion of the filepath so we can "link" the contents
let path =
    scriptFiles
    |> Array.map (fun x -> x.Substring(x.LastIndexOf(Path.DirectorySeparatorChar) + 1))

// giraffe pipeline integration.
// we must prever the order of tags, like h1 is the top and only 1 tag, h2s must be below h1, etc.
// trying to be screen reader friendly despite the html spec being wrong about that!
let view (ctx:HttpContext) (env:#_) (isDev) (body: int -> Task<string>) = task {
    let script =
        // in dev mode, fable stuff comes from local
        if isDev || path.Length = 0 then
            html
                $"""
                <script type="module" src="/dist/@vite/client"></script>
                <script type="module" src="/dist/build/App.js"></script>
                <script defer src="/_framework/aspnetcore-browser-refresh.js"></script>
            """
        // in prod mode, fable stuff comes from assets
        else
            let scripts =
                path
                |> Array.map (fun path ->
                    html
                        $"""
                    <script type="module" src="/dist/assets/{path}" ></script>
                    """)
            String.Join("\r\n", scripts)

    // there is no h1 so far
    let! body = body 0

    // extension 'alfonsogarciacaro.vscode-template-fsharp-highlight' lets f# interpolated string be colored like the actual code is here.
    // ?v=###### is for cache busting; you update this in the file manually, it's not so bad
    // <script> used to be the last thing. now, we can us `defer`. all should have EITHER `defer` or `async` unless you have a STRONG REASON, and you should loudly document that.
    //
    // this html below is exactly what you would see in the browser source view. no magic!
    return
        html $"""
    <!DOCTYPE html>
    <html theme=default-pizza lang="en">
        <head>
            <meta charset="utf-8" >
            <base href="/" />
            <title>Fun Pizza Shop </title>

            <meta name="description"
                content="Best Pizza in the Town" />
            <meta name="keywords" content="Order Pizza">

            <link rel="apple-touch-icon" href="/assets/icons/icon-512.png">
            <!-- This meta viewport ensures the webpage's dimensions change according to the device it's on. This is called Responsive Web Design.-->
            <meta name="viewport"
                content="viewport-fit=cover, width=device-width, initial-scale=1.0" />
            <meta name="theme-color"  content="#181818" />

            <!-- These meta tags are Apple-specific, and set the web application to run in full-screen mode with a black status bar. Learn more at https://developer.apple.com/library/archive/documentation/AppleApplications/Reference/SafariHTMLRef/Articles/MetaTags.html-->
            <meta name="apple-mobile-web-app-capable" content="yes" />
            <meta name="apple-mobile-web-app-title" content="Fun Pizza Shop" />
            <meta name="apple-mobile-web-app-status-bar-style" content="black" />

            <!-- Imports an icon to represent the document. -->
            <link rel="icon" href="/assets/icons/icon-512.svg" type="image/x-icon" />

            <!-- Imports the manifest to represent the web application. A web app must have a manifest to be a PWA. -->
            <link rel="manifest" href="/manifest.webmanifest" />
            <link rel="stylesheet" href="/css/index.css?v=202307101701"/>
            <link rel="stylesheet" href="https://unpkg.com/leaflet@1.9.4/dist/leaflet.css"
     integrity="sha256-p4NxAoJBhIIN+hmNHrzRCf9tD/miZyoHS5obTRR9BMY="
     crossorigin=""/>

            <script defer crossorigin="anonymous" type="text/javascript"
            src="https://cdnjs.cloudflare.com/ajax/libs/dompurify/3.0.1/purify.min.js"></script>
            <script defer src="/index.js"></script>
            <script defer src="https://unpkg.com/leaflet@1.9.4/dist/leaflet.js"
     integrity="sha256-20nQCchB9co0qIjJZRGuk2/Z9VM+kNiyxNV1lvTlZBo="
     crossorigin=""></script>
            {script}

        </head>

        <body>
            <header>
            </header>
            <main>
                {body}
            </main>
        </body>
    </html>"""
    }