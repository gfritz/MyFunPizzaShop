// specify serialization
// in config.hocon, we instruct akka in `serialization-bindings` what serializers to use
module Command.Serialization

// if you throw an exception in serialization from decoder, then akka knows to handle that

// every actor has one kind of event, one kind of state, one kind of command
// but commands are not persisted so we don't specify a serialization method.
// we don't have to do this but it is recommended
open Command
open Akkling
open Akka.Actor
open Akka.Serialization
open System.Text
open NodaTime
open Thoth.Json.Net
open System.Runtime.Serialization
open Serilog
open System
open FunPizzaShop.Command.Domain

module DefaultEncode =
    let instant (instant: Instant) =
        Encode.datetime (instant.ToDateTimeUtc())

module DefeaultDecode =
    let instant: Decoder<Instant> =
        Decode.datetimeUtc |> Decode.map (Instant.FromDateTimeUtc)


let extraThoth =
    Extra.empty
    |> Extra.withInt64
    |> Extra.withDecimal
    |> Extra.withCustom (DefaultEncode.instant) DefeaultDecode.instant

//Event encoding
// notice <Common.Event<>> vs just <User.State>
let userMessageEncode =
    Encode.Auto.generateEncoder<Common.Event<User.Event>> (extra = extraThoth)
let userMessageDecode =
    Decode.Auto.generateDecoder<Common.Event<User.Event>> (extra = extraThoth)


/// State encoding
/// // notice <Common.Event<>> vs just <User.State>
let userStateEncode = Encode.Auto.generateEncoder<User.State> (extra = extraThoth)
let userStateDecode = Decode.Auto.generateDecoder<User.State> (extra = extraThoth)


type ThothSerializer(system: ExtendedActorSystem) =
    inherit SerializerWithStringManifest(system)

    // TODO: why 1712?
    override _.Identifier = 1712

    override _.ToBinary(o) =
        match o with
        | :? Common.Event<User.Event> as mesg -> mesg |> userMessageEncode
        | :? User.State as mesg -> mesg |> userStateEncode
        | e ->
            Log.Fatal("shouldn't happen {e}", e)
            Environment.FailFast("shouldn't happen")
            failwith "shouldn't happen"
        |> Encode.toString 4
        |> Encoding.UTF8.GetBytes

    // akka Manifest: we specify what happens so that it is clearer instead of relying on internal convention.
    // maybe for people super familiar with akka this is unnecessary
    override _.Manifest(o: obj) : string =
        match o with
        | :? Common.Event<User.Event> -> "UserMessage"
        | :? User.State -> "UserState"
        | _ -> o.GetType().FullName

    override _.FromBinary(bytes: byte[], manifest: string) : obj =
        let decode decoder =
            Encoding.UTF8.GetString(bytes)
            |> Decode.fromString decoder
            |> function
                | Ok res -> res
                | Error er -> raise (new SerializationException(er))


        match manifest with
        | "UserState" -> upcast decode userStateDecode
        | "UserMessage" -> upcast decode userMessageDecode

        | _ ->
            Log.Fatal("manifest {manifest} not found", manifest)
            Environment.FailFast("shouldn't happen")
            raise (new SerializationException())

