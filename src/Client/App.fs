module FunPizzaShop.Client.App

open Elmish
open Lit
open Lit.Elmish
open Browser
open Elmish.UrlParser
open Elmish.Navigation

// make sure PizzaItem doesn't get kicked out by tree shaking
PizzaItem.register()
