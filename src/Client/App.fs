module FunPizzaShop.Client.App

open Elmish
open Lit
open Lit.Elmish
open Browser
open Elmish.UrlParser
open Elmish.Navigation
open CustomNavigation

// make sure PizzaItem doesn't get kicked out by tree shaking
// how to tell? in the browser devtools, if you don't see in
// localhost:5010/dist/ a corresponding Thing.js,
PizzaItem.register()
PizzaMenu.register()
Sidebar.register()
Checkout.register()
SignIn.register()

// for the checkout page, we introduce direct handling so that
// we control how to show the page
// note: Lit never removes an element unlike React. Lit appends.
//      you will see in the broswer source view that clicking checkout makes fps-checkout element
//      appear from nowhere and it does not go away when you leave the checkout page.
type Model = Page option

let init (result: Option<Page>) = result, Cmd.none //CustomNavigation.newUrl (toPage Home) 1

let update msg (model: Model) = model, Cmd.none

[<HookComponent>]
let view (model: Model) dispatch =
    Hook.useEffectOnChange (model, fun model ->
        let nonCheckout = document.querySelectorAll "main > *:not(fps-checkout)"
        match model with
        | Some Checkout ->
            for i = 0 to nonCheckout.length - 1 do
                nonCheckout.item(i).toggleAttribute("hidden", true) |> ignore
        | _ ->
            for i = 0 to nonCheckout.length - 1 do
                nonCheckout.item(i).toggleAttribute("hidden", false) |> ignore
    )
    match model with
    | Some page ->
        match page with
        | Home -> Lit.nothing
        | Checkout ->
            html $"""
             <fps-checkout></fps-checkout>
            """
    | None -> Lit.nothing

let pageParser: Parser<Page -> Page, Page> =
    oneOf [
        map Home (s "")
        map Home (s "/")
        map Checkout (s "checkout")
    ]

let urlUpdate (result: Option<Page>) model =
    printfn "urlUpdate %A" result
    match result with
    | None -> model, Cmd.none
    | Some page -> Some page, Cmd.none


Program.mkProgram init (update) view
|> Program.withLitOnElement (document.querySelector "main")
|> Program.withConsoleTrace
|> Program.toNavigable (parsePath pageParser) urlUpdate
|> Program.run
