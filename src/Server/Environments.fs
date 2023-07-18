module FunPizzaShop.Server.Environments

open FunPizzaShop
open System
open System.Diagnostics.CodeAnalysis
open Microsoft.Extensions.Configuration
open FunPizzaShop.ServerInterfaces.Query

// pros: less runtime magic compared to DI container
// cons: becomes a god object, but it's the composition root, so it kinda has to be
[<ExcludeFromCodeCoverageAttribute>]
type AppEnv(config: IConfiguration) =

    interface IConfiguration with
        member _.Item
            with get (key: string) = config.[key]
            and set key v = config.[key] <- v
        member _.GetChildren() = config.GetChildren()
        member _.GetReloadToken() = config.GetReloadToken()
        member _.GetSection key = config.GetSection(key)

    interface IQuery with
        member this.Query(filter: Shared.Model.Predicate option, orderby: string option, orderbydesc: string option, thenby: string option, thenbydesc: string option, take: int option, skip: int option): Async<'t list> =
            failwith "Not Implemented"

    // helper to reset app state without reloading EVERYTHING
    member _.Reset() = ()
