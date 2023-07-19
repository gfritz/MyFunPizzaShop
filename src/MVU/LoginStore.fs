// an MVU that other MVUs can subscribe to
module FunPizzaShop.MVU.LoginStore

open FunPizzaShop.Shared.Model.Authentication

type Model = { UserId: UserId option }

type Msg =
    | LoggedIn of UserId
    | LoggedOut

[<RequireQualifiedAccessAttribute>]
type Order =
    | NoOrder

let init () = { UserId = None }, Order.NoOrder

let update (msg: Msg) (model: Model) =
    match msg with
    | LoggedIn userId -> { model with UserId = Some userId },Order.NoOrder
    | LoggedOut -> { model with UserId = None },Order.NoOrder

let dispose _ = ()
