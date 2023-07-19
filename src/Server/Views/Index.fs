module FunPizzaShop.Server.Views.Index

open Common
open Microsoft.AspNetCore.Http
open FunPizzaShop.ServerInterfaces.Query
open FunPizzaShop.Shared.Model.Pizza
open FunPizzaShop.Shared.Model
open Thoth.Json.Net

let extraEncoders = Extra.empty |> Extra.withInt64 |> Extra.withDecimal

// code review: if you want `view` to be more pure, you could pass in `pizzaSpecials`, `toppings`
// and then the view doesn't depend on `env`.
// the challenge may be to only fetch the data needed
let view (env:#_) (ctx:HttpContext) (dataLevel: int) = task {
    // IQuery: looks like a repository due to its name, but it is really just access to the Read side of things
    // we don't have any SaveChanges according to the interface
    let query = env :> IQuery
    let! pizzaSpecials = query.Query<PizzaSpecial> (filter = Greater("BasePrice", 1m), take = 10)
    let! toppings = query.Query<Topping> ()
    let serializedToppings = Encode.Auto.toString (toppings, extra = extraEncoders)

    let li =
        pizzaSpecials
        |> List.map (fun pizza ->

        // serialize and encode
        let serializedSpecials = Encode.Auto.toString(pizza, extra = extraEncoders)
        let serializedSpecials = System.Net.WebUtility.HtmlEncode serializedSpecials

        // note: this is plain .net strings
        html $"""
            <li>
                <fps-pizza-item special='{serializedSpecials}'>
                    <div class="pizza-info" style="background-image: url('/assets/{pizza.ImageUrl}')">
                        <span class=title>{pizza.Name}</span>
                        {pizza.Description}
                        <span class=price>{pizza.FormattedBasePrice}</span>
                    </div>
                </fps-pizza-item>
            </li>
        """)
        |> String.concat "\r\n"

    return
        html $"""
            <fps-pizza-menu toppings='{serializedToppings}'>
                <ul class="pizza-cards">
                    {li}
                </ul>
            </fps-pizza-menu>
            <div class="sidebar">
                <fps-side-bar></fps-side-bar>
            </div>
        """
}
