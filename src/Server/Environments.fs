module FunPizzaShop.Server.Environments

open FunPizzaShop
open System
open System.Diagnostics.CodeAnalysis
open Microsoft.Extensions.Configuration
open FunPizzaShop.ServerInterfaces.Query
open FunPizzaShop.Shared.Model.Pizza
open FunPizzaShop.Shared.Model
open FunPizzaShop.ServerInterfaces.Command
open FunPizzaShop.Shared.Command.Authentication

// pros: less runtime magic compared to DI container
// cons: becomes a god object, but it's the composition root, so it kinda has to be
[<ExcludeFromCodeCoverageAttribute>]
type AppEnv(config: IConfiguration) as self =
    let mutable commandApi =
        lazy(FunPizzaShop.Command.API.api self NodaTime.SystemClock.Instance)

    interface IConfiguration with
        member _.Item
            with get (key: string) = config.[key]
            and set key v = config.[key] <- v
        member _.GetChildren() = config.GetChildren()
        member _.GetReloadToken() = config.GetReloadToken()
        member _.GetSection key = config.GetSection(key)


    interface IAuthentication with
        member _.Login: Login =
            commandApi.Value.Login

        member _.Logout: Logout =
            fun () ->
                async {
                    return  Ok()
                }
        member _.Verify: Verify =
            commandApi.Value.Verify


    interface IQuery with
        member this.Query<'t>(filter: Shared.Model.Predicate option, orderby: string option, orderbydesc: string option, thenby: string option, thenbydesc: string option, take: int option, skip: int option): Async<'t list> =
            let res =
                if typeof<'t> = typeof<PizzaSpecial> then
                    let special1 : PizzaSpecial = {
                        Id = 1L |> SpecialId.TryCreate |>forceValidate
                        Name = "Basic Cheese Pizza" |> ShortString.TryCreate |> forceValidate
                        BasePrice = 9.99m |> Price.TryCreate |> forceValidate
                        Description = "It's cheesy and delicious. Why wouldn't you want one?" |> ShortString.TryCreate |> forceValidate
                        ImageUrl = "img/pizzas/cheese.jpg" |> ShortString.TryCreate |> forceValidate
                    }
                    let special2 : PizzaSpecial = {
                        Id = 2L |> SpecialId.TryCreate |>forceValidate
                        Name = "The Baconatorizor" |> ShortString.TryCreate |> forceValidate
                        BasePrice =11.99m |> Price.TryCreate |> forceValidate
                        Description = "It has EVERY kind of bacon"|> ShortString.TryCreate |> forceValidate
                        ImageUrl = "img/pizzas/bacon.jpg" |> ShortString.TryCreate |> forceValidate
                    }
                    let special3 : PizzaSpecial = {
                        Id = 3L |> SpecialId.TryCreate |>forceValidate
                        Name = "Classic pepperoni" |> ShortString.TryCreate |> forceValidate
                        BasePrice = 10.50m |> Price.TryCreate |> forceValidate
                        Description = "It's the pizza you grew up with, but Blazing hot!" |> ShortString.TryCreate |> forceValidate
                        ImageUrl = "img/pizzas/pepperoni.jpg" |> ShortString.TryCreate |> forceValidate
                    }
                    let special4 : PizzaSpecial = {
                        Id = 4L |> SpecialId.TryCreate |>forceValidate
                        Name = "Buffalo chicken" |> ShortString.TryCreate |> forceValidate
                        BasePrice = 12.75M |> Price.TryCreate |> forceValidate
                        Description = "Spicy chicken, hot sauce and bleu cheese, guaranteed to warm you up" |> ShortString.TryCreate |> forceValidate
                        ImageUrl = "img/pizzas/meaty.jpg"|> ShortString.TryCreate |> forceValidate
                    }
                    [special1; special2; special3; special4]
                    |> List.ofSeq
                    |> box
                elif typeof<'t> = typeof<Topping> then
                    let topping1 : Topping = {
                        Id = 1L |> ToppingId.TryCreate |>forceValidate
                        Name = "Mushrooms" |> ShortString.TryCreate |> forceValidate
                        Price = 1m |> Price.TryCreate |> forceValidate
                    }
                    let topping2 : Topping = {
                        Id = 2L |> ToppingId.TryCreate |>forceValidate
                        Name = "Duck sausage" |> ShortString.TryCreate |> forceValidate
                        Price = 1m |> Price.TryCreate |> forceValidate
                    }
                    [topping1; topping2]
                    |> List.ofSeq
                    |> box
                else
                    failwith $"Query {typeof<'t>} Not Implemented"

            async { return res :?> list<'t> }

    // helper to reset app state without reloading EVERYTHING
    member _.Reset() = ()
