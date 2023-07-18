module FunPizzaShop.MVU.PizzaItem

open Elmish
open FunPizzaShop.Shared.Model.Pizza

type Model = { PizzaSpecial: PizzaSpecial }

type Msg = NA

type Order = NoOrder

let init (ps: PizzaSpecial) () =
    { PizzaSpecial = ps } , NoOrder

let update msg model =
    model , NoOrder
