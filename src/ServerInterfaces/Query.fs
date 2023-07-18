module FunPizzaShop.ServerInterfaces.Query

open FunPizzaShop.Shared.Model
open FunPizzaShop.Shared.Model.Pizza
open Akka.Streams
open Akka.Streams.Dsl

[<Interface>]
type IQuery =
    abstract Query<'t> : ?filter:Predicate * ?orderby:string * ?orderbydesc:string * ?thenby:string  * ?thenbydesc:string * ?take:int * ?skip:int -> list<'t> Async
