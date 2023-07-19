module FunPizzaShop.MVU.SignIn
open Elmish
open FunPizzaShop.Shared.Model
open Authentication

type Status = NotLoggedIn | LoggedIn of UserId | AskEmail | AskVerification

type Model = {
    Status: Status
    UserId: UserId option
    IsBusy : bool;
}
type Msg =
    | LoginRequested // Ask for email
    | LoginCancelled  // cancel login
    | EmailSubmitted of UserId //email entered
    | VerificationSubmitted of VerificationCode //verification code entered
    | EmailSent
    | EmailFailed of string
    | VerificationSuccessful
    | VerificationFailed
    | LogoutRequested
    | LogoutSuccess
    | LogoutError of string

type Order =
    | NoOrder
    | Login of UserId // to our Server
    | Verify of UserId * VerificationCode // to our Server
    | Logout of UserId // to our Server
    | ShowError of string
    | PublishLogin of UserId // successful

let init (userName:string option) () =
    match userName with
    // what if user is already logged in and the server knows it
    | Some name ->
        let userId = name |> UserId.TryCreate |> forceValidate |> Some
        {
            Status = LoggedIn (userId.Value)
            UserId = userId ;
            IsBusy = false
        } , (PublishLogin (userId.Value))

    | None ->
        {
            Status = NotLoggedIn;
            UserId = None ;
            IsBusy = false
        } , NoOrder

let update msg model =
    match msg with
    | LoginRequested -> { model with Status =Status.AskEmail }, NoOrder
    | LoginCancelled -> { model with Status =Status.NotLoggedIn }, NoOrder
    | EmailSubmitted email ->
        {model with UserId =  Some email }, Order.Login email
    | EmailSent -> { model with Status =Status.AskVerification }, NoOrder
    | VerificationSubmitted code ->
        model, Order.Verify (model.UserId.Value, code)
    | EmailFailed ex -> model, Order.ShowError ex
    // onur says: does this notify the ???
    | VerificationSuccessful -> { model with Status =Status.LoggedIn model.UserId.Value }, NoOrder
    | VerificationFailed -> model,  Order.ShowError "Verification failed"
    | LogoutSuccess -> { model with Status =Status.NotLoggedIn }, NoOrder
    | LogoutError ex -> { model with Status =Status.NotLoggedIn }, Order.ShowError ex
    | LogoutRequested ->
        model, Order.Logout model.UserId.Value
