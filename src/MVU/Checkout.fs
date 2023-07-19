module FunPizzaShop.MVU.Checkout
open Elmish
open FunPizzaShop.Shared
open Model
open Pizza
open Authentication
open System

type OrderDetails = { Address : Address; Pizzas : Pizza list }

type Model = { Pizzas: Pizza list; UserId : UserId option; PendingOrder : OrderDetails option}

type Msg =
      SetPizzas of Pizza list
      | OrderCheckedOut of OrderDetails
      | SetLoginStatus of UserId option
      | OrderPlaced


type Order =
      | NoOrder
      | GetPizzas
      | PlaceOrder of Pizza.Order
      | SubscribeToLogin
      | OrderList of Order list
      | RequestLogin

let init () =
      { Pizzas = []; UserId = None; PendingOrder = None } ,
      // we want more than one thing to happen upon reaching Checkout
      [GetPizzas;SubscribeToLogin] |> OrderList

let rec update msg model =
      match msg with
      | OrderPlaced -> model, NoOrder
      | SetPizzas pizzas -> ({model with Pizzas = pizzas}: Model), NoOrder
      // TODO: rename to better align with the instigating UI action 'Place Order' button
      | OrderCheckedOut orderDetails ->
            if model.UserId.IsNone then
                  {model with PendingOrder = Some orderDetails}, RequestLogin
            else
                  let order :FunPizzaShop.Shared.Model.Pizza.Order = {
                        DeliveryAddress = orderDetails.Address
                        Pizzas = orderDetails.Pizzas
                        UserId = model.UserId.Value
                        OrderId =  OrderId.CreateNew()
                        CreatedTime = DateTime.UtcNow
                        DeliveryLocation =  {Latitude = 0.0; Longitude = 0.0 }
                        Version = Model.Version 0L
                        CurrentLocation = {Latitude = 0.0; Longitude = 0.0 }
                        DeliveryStatus=DeliveryStatus.NotDelivered

                  }
                  model, Order.PlaceOrder order

      | SetLoginStatus userId ->
            let model = {model with UserId = userId}
            if userId.IsSome then
                  match model.PendingOrder with
                  | Some orderDetails ->
                        let model = {model with PendingOrder = None}
                        // typical Elmish you do Cmd.ofMsg, but we call ourselves; try the ofMsg way for education
                        update (OrderCheckedOut orderDetails) model
                  | None -> model, NoOrder
            else
            model, NoOrder
