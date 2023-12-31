// could still choose a different implementation! this does not force akka or cqs or cqrs
module FunPizzaShop.ServerInterfaces.Command
open FunPizzaShop.Shared.Command.Authentication
open FunPizzaShop.Shared.Command.Pizza

[<Interface>]
type IAuthentication =
    abstract Login: Login
    abstract Logout: Logout
    abstract Verify: Verify


[<Interface>]
type IPizza =
    abstract Order: OrderPizza


[<Interface>]
type IMailSender =
    abstract SendVerificationMail: SendVerificationMail
