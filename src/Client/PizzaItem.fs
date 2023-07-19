module FunPizzaShop.Client.PizzaItem

open Elmish
open Elmish.HMR
open ElmishOrder
open Lit
open Lit.Elmish
open Browser.Types
open Fable.Core.JsInterop
open Fable.Core
open System
open Browser
open Elmish.Debug
open FsToolkit.ErrorHandling
open Browser.Types
open FunPizzaShop.MVU.PizzaItem
open Thoth.Json
open FunPizzaShop.Shared.Model.Pizza
open FunPizzaShop.Shared.Model
open FunPizzaShop.Shared.Constants

let extraEncoders = Extra.empty |> Extra.withInt64 |> Extra.withDecimal

// optional overall but needed for HMR
let private hmr = HMR.createToken ()

let rec execute (host: LitElement) order (dispatch: Msg -> unit) =
    match order with
    | Order.NoOrder -> ()

// hook - Fable.Lit; like a function. not a real WebComponent
[<HookComponent>]
let view (host:LitElement) (model:Model) dispatch =
    // only once per element rendering, attach the handler to do something with the DOM tree
    // the `?` is an operator to let us say that we know the object exists, but fable didn't generate it, so the compiler would
    // say it doesn't know about the object without `?`
    Hook.useEffectOnce (fun () ->
        host?addEventListener("click", (fun (e: MouseEvent) ->
            // browser event cycle
            // bubbles: we want the parent html elements to be able to capture it
            host.dispatchCustomEvent (Events.PizzaSelected ,model.PizzaSpecial, true, true, true)
        )) |> ignore
    )
    Lit.nothing

[<LitElement("fps-pizza-item")>]
let LitElement () =
// optional overall but needed for HMR
#if DEBUG
    Hook.useHmr (hmr)
#endif
    let host, prop = LitElement.init (fun config ->
        let split (str: string): PizzaSpecial option =
           let res = Decode.Auto.fromString<PizzaSpecial>(str, extra = extraEncoders)
           match res with
                | Ok x -> Some x
                | Error x -> console.error(x); Option.None

        config.useShadowDom <- false
        config.props <-
        {|
            // puts our PizzaSpecial into "special" html attribute <my-html-element special="deserialized-json-for-element />"
            // according to our `split` function.
            // we send the html for search engines/etc - certainly to see spans, headings, etc - maybe our encoded data won't make any impression.
            // we send the encoded data for the web-component element to populate itself.
            special = Prop.Of( Option.None , attribute="special", fromAttribute = split)
        |}
    )

    // WithOrder - mkHiddenProgramWithOrderExecute is how we adapt our `Order` concept into Elmish Cmd
    let program =
        Program.mkHiddenProgramWithOrderExecute
            (init (prop.special.Value.Value)) (update) (execute host)
#if DEBUG
        |> Program.withDebugger
        |> Program.withConsoleTrace
#endif

    // useeffect - to interact with the DOM tree in react; capture clicks and do something
    let model, dispatch = Hook.useElmish program
    view host model dispatch

// we create this so that tree-shaking doesn't kick out this code
let register () = ()
