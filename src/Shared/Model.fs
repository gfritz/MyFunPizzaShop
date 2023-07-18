// rec means we allow types in this module to reference types in this module out of the usual top-to-bottom ordering
module rec FunPizzaShop.Shared.Model

open System
open Fable.Validation
open FsToolkit.ErrorHandling
open Thoth.Json

// there are cases where we are CERTAIN that the Result is in Ok state
let inline forceValidate (e) =
    match e with
    | Ok x -> x
    | Error x ->
        let errors = x |> List.map (fun x -> x.ToString()) |> String.concat ", "
        invalidOp errors

type Predicate =
    | Greater of string * IComparable
    | GreaterOrEqual of string * IComparable
    | Smaller of string * IComparable
    | SmallerOrEqual of string * IComparable
    | Equal of string * obj
    | NotEqual of string * obj
    | And of Predicate * Predicate
    | Or of Predicate * Predicate
    | Not of Predicate

type Version =
    | Version of int64
    member this.Value: int64 = let (Version v) = this in v
    member this.Zero = Version 0L

type ShortStringError =
    | EmptyString
    | TooLongString

type ShortString =
    private | ShortString of string
    member this.Value = let (ShortString s) = this in s
    static member TryCreate (s:string) =
        single(fun t ->
            t.TestOne s
            |> t.MinLen 1 EmptyString
            |> t.MaxLen 255 TooLongString
            |> t.Map ShortString
            |> t.End
        )

    static member Validate(s: ShortString) =
        s.Value |> ShortString.TryCreate |> forceValidate

    override this.ToString() = this.Value

type LongString =
    private | LongString of string

    member this.Value = let (LongString s) = this in s
    static member TryCreate(s: string) =
        single (fun t -> t.TestOne s |> t.MinLen 1 EmptyString |> t.Map LongString |> t.End)

    static member Validate(s: LongString) =
        s.Value |> LongString.TryCreate |> forceValidate

    override this.ToString() = this.Value

type NumericError =
    | Negative

type Price =
    private
    | Price of decimal

    member this.Value = let (Price s) = this in s

    static member TryCreate(s: decimal) =
        single (fun t ->
            t.TestOne s
            |> t.Gte 0m Negative
            |> t.Map Price
            |> t.End)

    static member Validate(s: Price) =
        s.Value |> Price.TryCreate |> forceValidate

    override this.ToString() = this.Value.ToString("0.00")

module Pizza =

    type SpecialId =
        private
        | SpecialId of int64

        member this.Value = let (SpecialId pizzaId) = this in pizzaId

        static member TryCreate(s: int64) =
            single (fun t -> t.TestOne s  |> t.Gte 0L Negative  |> t.Map SpecialId |> t.End)

        static member Validate(s: SpecialId) =
            s.Value |> SpecialId.TryCreate |> forceValidate

        override this.ToString() = this.Value.ToString()

    type ToppingId =
        private
        | ToppingId of int64

        member this.Value = let (ToppingId pizzaId) = this in pizzaId

        static member TryCreate(s: int64) =
            single (fun t -> t.TestOne s |> t.Gte 0L Negative |> t.Map ToppingId |> t.End)

        static member Validate(s: ToppingId) =
            s.Value |> ToppingId.TryCreate |> forceValidate

        override this.ToString() = this.Value.ToString()

    type PizzaId =
        | PizzaId of ShortString
        member this.Value = let (PizzaId pizzaId) = this in pizzaId

    /// <summary>
    /// Represents a pre-configured template for a pizza a user can order
    /// </summary>
    type PizzaSpecial = {
        Id: SpecialId
        Name: ShortString
        BasePrice: Price
        Description: ShortString
        ImageUrl: ShortString
    } with

        member this.FormattedBasePrice = this.BasePrice.ToString()

        static member Validate(s: PizzaSpecial) =
            s.BasePrice|> Price.Validate |> ignore
            s.Name |> ShortString.Validate |> ignore
            s.Description |> ShortString.Validate |> ignore
            s.ImageUrl |> ShortString.Validate |> ignore
            s.Id |> SpecialId.Validate |> ignore


    type Topping = {
        Id: ToppingId
        Name: ShortString
        Price: Price
    } with

        member this.FormattedBasePrice = this.Price.ToString()

        static member Validate(s: Topping) =
            s.Price |> Price.Validate |> ignore
            s.Name |> ShortString.Validate |> ignore
            s.Id |> ToppingId.Validate |> ignore

    type Pizza = {
        Id: PizzaId
        Special: PizzaSpecial
        SpecialId: SpecialId
        Size: int64
        Toppings: Topping list
    } with

        static member DefaultSize = 12
        static member MinimumSize = 9
        static member MaximumSize = 17

        member this.BasePrice =
            (this.Size |> decimal) / (Pizza.DefaultSize |> decimal) * this.Special.BasePrice.Value
            |> Price.TryCreate |> forceValidate

        member this.TotalPrice =
            this.BasePrice.Value + (this.Toppings |> List.sumBy (fun t -> t.Price.Value))
            |> Price.TryCreate |> forceValidate

        member this.FormattedTotalPrice = this.TotalPrice.ToString()

        static member CreatePizzaFromSpecial(special: PizzaSpecial) = {
            Id = Guid.NewGuid().ToString() |> ShortString.TryCreate |> forceValidate |> PizzaId
            Special = special
            SpecialId = special.Id
            Size = Pizza.DefaultSize
            Toppings = []
        }

        static member Validate(s: Pizza) =
            s.Special |> PizzaSpecial.Validate |> ignore
            s.Toppings |> List.iter (fun t -> t |> Topping.Validate |> ignore)
            s.Id.Value |> ShortString.Validate |> ignore
            s.SpecialId |> SpecialId.Validate |> ignore

    type Address =
        {
            Name: ShortString
            Line1: ShortString
            Line2: ShortString
            City: ShortString
            Region: ShortString
            PostalCode: ShortString
        }
        static member Validate(s: Address) =
            s.Name |> ShortString.Validate |> ignore
            s.Line1 |> ShortString.Validate |> ignore
            s.Line2 |> ShortString.Validate |> ignore
            s.City |> ShortString.Validate |> ignore
            s.Region |> ShortString.Validate |> ignore
            s.PostalCode |> ShortString.Validate |> ignore

    type LatLong =
        {
            Latitude: double
            Longitude: double
        }

    type DeliveryStatus =
        | NotDelivered
        | OutForDelivery
        | Delivered

    type OrderId =
        | OrderId of ShortString
        member this.Value =
            let (OrderId orderId) = this in orderId

        // prefixes here are optional but they help with Akka ???SOMEHOW???
        static member CreateNew() =
            "Order_" + Guid.NewGuid().ToString() |> ShortString.TryCreate |> forceValidate |> OrderId

    type DeliveryId =
        |DeliveryId of ShortString
        member this.Value =
            let (DeliveryId deliveryId) = this in deliveryId

    // prefixes here are optional but they help with Akka ???SOMEHOW???
        static member CreateNew() =
            "Delivery_" + Guid.NewGuid().ToString() |> ShortString.TryCreate |> forceValidate |> DeliveryId

    type Order =
        {
            OrderId: OrderId
            UserId: Authentication.UserId
            CreatedTime: DateTime
            DeliveryAddress: Address
            DeliveryLocation: LatLong
            Pizzas: Pizza list
            Version: Version
            CurrentLocation: LatLong
            DeliveryStatus: DeliveryStatus
        }
        member this.TotalPrice = this.Pizzas |> List.sumBy (fun p -> p.TotalPrice.Value) |> Price.TryCreate |> forceValidate
        member this.FormattedTotalPrice = this.TotalPrice.Value.ToString("0.00")
