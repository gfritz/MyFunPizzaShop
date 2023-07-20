/// The API for whoever wants to communicate with our Actors
module FunPizzaShop.Command.API

// NOTE: our customized error handling happens in here Error { ... }
// module User =
// subscribe:
// If no message ever happens, then it waits indefinitely.
// For each subscription, Common.fs creates a new Actor, assigns a correlationId per command
// verify:
// ...
//
// Is all this trouble worth it? Onur says for him yes.
// Once you setup your project with the akka foundation, it is easy to integrate additional actors.
// However, the boilerplate gives weight to an argument to use Marten or use CRUD for simple cases.
// Of course, simple cases grow into not simple and it may be HARDER to change at that point while this
// akka actor setup is rather extensible.
//
// TODO: would have been better to include Order
//  when 2 actors need to communicate with eachother, you really want a saga
//  but that is not done here; possibly to show the ease of adding additional

open Command
open Common
open Serilog
open Actor
open NodaTime
open System
open Microsoft.Extensions.Configuration
open FunPizzaShop.Command.Domain.API
open FunPizzaShop.Shared.Command.Authentication
open FunPizzaShop.Shared.Command.Pizza
open FunPizzaShop.Command.Domain
open FunPizzaShop.Shared.Model.Pizza

let createCommandSubscription (domainApi: IDomain) factory (id: string) command filter =
    let corID = id |> Uri.EscapeDataString |> SagaStarter.toNewCid
    let actor = factory id

    let commonCommand: Command<_> = {
        CommandDetails = command
        CreationDate = domainApi.Clock.GetCurrentInstant()
        CorrelationId = corID
    }

    let e = {
        Cmd = commonCommand
        EntityRef = actor
        Filter = filter
    }

    let ex = Execute e
    ex |> domainApi.ActorApi.SubscribeForCommand

module User =
    open FunPizzaShop.Shared.Model.Authentication

    let login (createSubs) : Login =
        fun userId ->
            async {
                Log.Debug("Inside login {@userId}", userId)

                let subscribA =
                    createSubs (userId.Value) (User.Login) (function
                        | User.LoginFailed
                        | User.LoginSucceeded _ -> true
                        | _ -> false)

                let! subscrib = subscribA

                match subscrib with
                | {
                      EventDetails = User.LoginSucceeded _
                      Version = v
                  } -> return Ok()

                | {
                      EventDetails = User.LoginFailed _
                      Version = v
                  } -> return Error [ "Login failed" ]

                | other -> return failwithf "unexpected event %A" other
            }

    let verify (createSubs) : Verify =
        fun (userId, verCode) ->
            async {
                Log.Debug("Inside Verify {@userId} {@verCode}", userId, verCode)

                let subscribA =
                    createSubs (userId.Value) (User.VerifyLogin verCode) (function
                        | User.VerificationFailed
                        | User.VerificationSucceeded _ -> true
                        | _ -> false)

                let! subscrib = subscribA

                match subscrib with
                | {
                      EventDetails = User.VerificationSucceeded
                      Version = v
                  } -> return Ok()

                | {
                      EventDetails = User.VerificationFailed
                      Version = v
                  } -> return Error [ VerificationError.InvalidVerificationCode ]

                | other -> return failwithf "unexpected event %A" other
            }

[<Interface>]
type IAPI =
    abstract ActorApi: IActor
    abstract Login: Login
    abstract Verify: Verify

let api (env: _) (clock: IClock) =
    let config = env :> IConfiguration
    let actorApi = Actor.api config
    let domainApi = Domain.API.api env clock actorApi
    let userSubs = createCommandSubscription domainApi domainApi.UserFactory

    { new IAPI with
        member this.Login: Login =
            User.login userSubs
        member this.Verify: Verify =
            User.verify userSubs
        member _.ActorApi = actorApi
    }
