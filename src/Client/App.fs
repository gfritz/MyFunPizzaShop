module FunPizzaShop.Client.App

open Elmish
open Lit
open Lit.Elmish
open Browser
open Elmish.UrlParser
open Elmish.Navigation

// make sure PizzaItem doesn't get kicked out by tree shaking
// how to tell? in the browser devtools, if you don't see in
// localhost:5010/dist/ a corresponding Thing.js,
PizzaItem.register()
PizzaMenu.register()
Sidebar.register()
