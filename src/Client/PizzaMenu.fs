module FunPizzaShop.Client.PizzaMenu

open Elmish
open Elmish.HMR
open Lit
open Lit.Elmish
open Browser.Types
open Fable.Core.JsInterop
open Fable.Core
open System
open Browser
open Elmish.Debug
open FsToolkit.ErrorHandling
open ElmishOrder
open Browser.Types
open FunPizzaShop.MVU.PizzaMenu
open Thoth.Json
open FunPizzaShop.Shared.Model.Pizza
open FunPizzaShop.Shared.Model
open FunPizzaShop.Shared
open FunPizzaShop.Shared.Constants

let extraEncoders = Extra.empty |> Extra.withInt64 |> Extra.withDecimal

let private hmr = HMR.createToken ()

let rec execute (host: LitElement) order (dispatch: Msg -> unit) =
    match order with
    | Order.NoOrder -> ()

[<HookComponent>]
let view (host:LitElement) (model: Model) dispatch =
    Hook.useEffectOnce (fun () ->
        let handleSelectedPizza (e: Event) =
            let customEvent = e :?> CustomEvent
            let pizzaSpecial = customEvent.detail :?> Pizza.PizzaSpecial
            let pizza = Pizza.CreatePizzaFromSpecial pizzaSpecial
            dispatch (PizzaSelected pizza)

        host?addEventListener (Events.PizzaSelected, handleSelectedPizza) |> ignore

        Hook.createDisposable (fun () -> host?removeEventListener (Events.PizzaSelected, handleSelectedPizza)))

    let topicOption (index: int) (topping: Topping) =
        // html: does NOT return string here! Lit framework thing
        html
            // this is html - so our int index will ultimately become a string. that is the html interface
            // HTMLOptionElement.value is a DOMString
            // you will get a headache if you ignore or forget about this
            $"""
            <option value={index}> {topping.Name.Value} - £{topping.FormattedBasePrice}</option>
        """

    let toppings =
        let toppingOptions =
            // these elements are going into a list, so react needs a unique key for each element
            // regular List.map works but is 'supoptimal' vs Lit.mapiUnique because ???
            model.Toppings |> Lit.mapiUnique (fun t -> t.Id.ToString()) topicOption

        html
            // use @input not just input: this is documented in Fable.Lit
            // https://fable.io/Fable.Lit
            $"""
            <select class="custom-select" @input={Ev(fun e -> dispatch (ToppingAdded(e.target?value |> System.Int32.Parse)))}>
                <option value="-1">Choose a topping</option>
                {toppingOptions}
            </select>
        """

    let toppingInstance (index: int) (topping: Topping) =
        html
            $"""
            <div class="topping">
                <span class="topping-name">{topping.Name.Value}</span>
                <span class="topping-price">{topping.FormattedBasePrice}</span>
                <button  class="delete-topping"
                    @click={Ev(fun _ -> dispatch (ToppingRemoved topping))}>x</button>
            </div>
        """

    // the red circular elements in the dialog
    let toppingInstances =
        match model.Pizza with
        | Some pizza ->
            let toppings =
                pizza.Toppings |> Lit.mapiUnique (fun t -> t.Id.ToString()) toppingInstance

            html
                $"""
                <div class="toppings">
                    {toppings}
                </div>
            """
        | None -> Lit.nothing

    let dialog (pizza: Pizza.Pizza) =
        // TODO: use dialog element instead
        html $"""
        <div class="dialog-container">
            <div class="dialog">
                <div class="dialog-title">
                    <h2> {pizza.Special.Name.Value} </h2>
                    {pizza.Special.Description.Value}
                </div>
                <form class="dialog-body">
                    <div>
                        <label>Toppings:</label>
                        {toppings}
                    </div>
                    {toppingInstances}
                    <div>
                        <label>Size:</label>
                        <input type="range" .value={ pizza.Size } min={Pizza.MinimumSize}
                            max= {Pizza.MaximumSize} step="1"
                                @input={Ev(fun e -> dispatch (SizeChanged e.target?value))}  />
                        <span class="size-label">
                            {pizza.Size}" (£{pizza.FormattedTotalPrice})
                        </span>
                    </div>
                </form>
                <div class="dialog-buttons">
                    <button id="cancelButton"
                        class="btn btn-secondary mr-auto"
                        @click={Ev(fun _ -> dispatch PizzaCancelled)}>Cancel</button>

                    <span class="mr-center">
                        Price: <span class="price">{pizza.FormattedTotalPrice}</span>

                    </span>
                    <button id="confirmButton"
                        class="btn btn-success ml-auto"
                        @click={Ev(fun _ ->
                            host.dispatchCustomEvent(Constants.Events.PizzaOrdered, model.Pizza.Value, true, true,true);
                            dispatch PizzaConfirmed)}>Order ></button>
                </div>
            </div>
        </div>
        """

    match model.Pizza with
    | Some pizza -> dialog pizza
    | None -> Lit.nothing

[<LitElement("fps-pizza-menu")>]
let LitElement () =
#if DEBUG
    Hook.useHmr (hmr)
#endif

    let host, prop =
        LitElement.init (fun config ->
            let split (str: string) : Topping list =
                let res = Decode.Auto.fromString<Topping list> (str, extra = extraEncoders)

                match res with
                | Ok x -> x
                | Error x -> []

            config.useShadowDom <- false

            config.props <- {|
                toppings = Prop.Of([], attribute = "toppings", fromAttribute = split)
            |})

    let program =
        Program.mkHiddenProgramWithOrderExecute (init (prop.toppings.Value)) (update) (execute host)
#if DEBUG
        |> Program.withDebugger
        |> Program.withConsoleTrace
#endif
    let model, dispatch = Hook.useElmish program
    view host model dispatch

let register () = ()
