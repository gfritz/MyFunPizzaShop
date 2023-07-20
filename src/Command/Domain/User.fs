module FunPizzaShop.Command.Domain.User

open Command
open Akkling
open Akkling.Persistence
open AkklingHelpers
open Akka
open Common
open Serilog
open System
open Akka.Cluster.Tools.PublishSubscribe
open Actor
open Microsoft.Extensions.Configuration
open FunPizzaShop.Shared.Model.Authentication
open FunPizzaShop.Shared.Model
open Akka.Logger.Serilog
open Akka.Event
open FunPizzaShop.ServerInterfaces.Command

type Command =
    | Login
    | VerifyLogin of VerificationCode option

type Event =
    | LoginSucceeded of VerificationCode option
    | LoginFailed
    | VerificationFailed
    | VerificationSucceeded

// new in net7.0 or so, random wasn't thread safe
let random = System.Random.Shared


type State = {
    Verification: VerificationCode option
    Version: int64

} with

    interface IDefaultTag

///
/// mediator: distributed pub/sub
/// mailbox: hello
/// JUST STICK TO OBJECT. Type-safe is tough because we deal with many types of messages with different payloads.
let actorProp (env:_ ) toEvent (mediator: IActorRef<Publish>) (mailbox: Eventsourced<obj>) =
    let config  = env :> IConfiguration
    let mailSender = env:> IMailSender
    // integrate with serilog
    let log = mailbox.UntypedContext.GetLogger()
    // an akkling thing; maybe no longer necessary (but what's the alternative)
    let mediatorS = retype mediator
    // some actors can init sagas, our pizza one probably does not need but best to always do so we don't forget
    let sendToSagaStarter = SagaStarter.toSendMessage mediatorS mailbox.Self

    let apply (event: Event) (state:State) =
        log.Debug("Apply Message {@Event}, State: @{State}", event, state)

        match event with
        | LoginSucceeded(code) ->
            {
                state with
                    Verification = code
            }

        | _ -> state

    let rec set (state: State) =
        actor {
            let! msg = mailbox.Receive()
            // large payloads may frustrate this!
            log.Debug("Message {MSG}, State: {@State}", box msg, state)

            match msg with
            // these are actor level events that we aren't interested in
            | PersistentLifecycleEvent _
            | :? Persistence.SaveSnapshotSuccess
            | LifecycleEvent _ -> return! state |> set

            // snapshot restored - it is loaded from persistence to application
            | SnapshotOffer(snapState: obj) -> return! snapState |> unbox<_> |> set

            // handling for the Akkling.Persist command
            | Persisted mailbox (:? Common.Event<Event> as event) ->
                let version = event.Version
                SagaStarter.publishEvent mailbox mediator event event.CorrelationId

                let state = {
                    (apply event.EventDetails state) with
                        Version = version
                }

                if (version >= 30L && version % 30L = 0L) then
                    // note: advanced case probably not needed in FunPizzaShop
                    // scenario: system crashes. actor SHOULD reapply all events.
                    //  what if you have 1 million events. that's a lot to reprocess.
                    //  applying a snapshot sets a starting point
                    // <@>: think of this operator as like elmish Cmd.ofBatch
                    // how?: see `Recovering`
                    return! state |> set <@> SaveSnapshot(state)
                else
                    return! state |> set

            | Recovering mailbox (:? Common.Event<Event> as event) ->
                return!
                    {
                        (apply event.EventDetails state) with
                            Version = event.Version
                    }
                    |> set

            | _ ->
                match msg with
                // can execute POST STARTUP behavior here, if any
                // kind of like an object's constructor
                | :? Persistence.RecoveryCompleted -> return! state |> set


                | :? (Common.Command<Command>) as userMsg ->

                    // for clarity, this is a development tracking number
                    let ci = userMsg.CorrelationId
                    let commandDetails = userMsg.CommandDetails
                    let v = state.Version

                    match commandDetails with
                    | (VerifyLogin incomingCode) ->
                        // TODO: we should delegate mail sending to another service
                        // note: this is just a plain function. no actor stuff going on.
                        let verficiationEvent =
                            if mailbox.Pid.Contains("@" |> Uri.EscapeDataString) then
                                if incomingCode.IsNone then VerificationSucceeded
                                else
                                match state.Verification with
                                | Some(code) when code = incomingCode.Value -> VerificationSucceeded
                                | _ -> VerificationFailed
                            else
                                // sharded actors should have a slash but why ???
                                let lastSlash = mailbox.Pid.LastIndexOf("/")

                                let id =
                                    mailbox.Pid
                                        .Substring(lastSlash + 1)
                                        // akka escapes @ symbol
                                        |> Uri.UnescapeDataString

                                if
                                    //(BestFitBox.Command.MailSender.checkSMSVerification config id incomingCode.Value.Value) = "approved"
                                    false
                                then
                                    VerificationSucceeded
                                else
                                    VerificationFailed

                        let verficiationOutcome =
                            // maybe this event will start a saga, but the actor doesn't care either way
                            // scenario: we submit an event then the system crashes. we have an orphan event.
                            //  With a saga started before handling the event, there is greater safety. the saga persists to the database before handling the event.
                            //  Can the saga start but the event isn't persisted? Yes but it's not an issue because:
                            //  1. orphaned message will be accompanied by an abort message
                            //  2. an already started saga will restart once the system restarts
                            // WORST CASE: saga in started state is aware of crash. it can issue an abort message. 'I crashed sorry I will abort'.
                            // An orphaned message will be accompanied by an abort message always.
                            toEvent ci (v + 1L) verficiationEvent |> sendToSagaStarter ci |> box |> Persist



                        return! verficiationOutcome


                    | (Login) ->
                        try
                            let verificationCode =
                                VerificationCode.TryCreate(random.Next(100000, 999999).ToString())
                                |> forceValidate

                            // sharded actors should have a slash but why ???
                            let lastSlash = mailbox.Pid.LastIndexOf("/")

                            let email =
                                mailbox.Pid
                                    .Substring(lastSlash + 1)
                                    // akka escapes @ symbol
                                    |> Uri.UnescapeDataString
                                    |> UserId.TryCreate
                                    |> forceValidate
                            let body =
                                $"Your verification code is <b>{verificationCode.Value}</b>"
                                |> LongString.TryCreate |> forceValidate
                            let subject = "Verification Code" |> ShortString.TryCreate |> forceValidate
                            (mailSender.SendVerificationMail email subject body) |> Async.Start


                            let e = LoginSucceeded( Some verificationCode)
                            // TODO does this change state version? it does so let's increment
                            return! toEvent ci (v + 1L) e |> box |> Persist
                        with ex ->
                            log.Error(ex, "Error sending verification code")
                            let e2 = LoginFailed
                            return! toEvent ci v e2 |> box |> Persist

                | _ ->
                    log.Debug("Unhandled Message {@MSG}", box msg)
                    // akkling knows about this and it should report this in logs
                    // TODO: connect handling of this message to an opentelemetry reporter?
                    return Unhandled
        }

    set {
        Version = 0L
        Verification = None
    }

// all "User" aggregates will come through this actor
let init (env: _) toEvent (actorApi: IActor) =
    AkklingHelpers.entityFactoryFor actorApi.System shardResolver "User"
    <| propsPersist (actorProp env toEvent (typed actorApi.Mediator))
    <| false

// for "User" entityId could be email
// TODO: link to criteria for a good akka entityId
let factory (env: _) toEvent actorApi entityId =
    (init env toEvent actorApi).RefFor DEFAULT_SHARD entityId
