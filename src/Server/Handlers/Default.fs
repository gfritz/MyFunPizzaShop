module FunPizzaShop.Server.Handlers.Default

open System.Threading.Tasks
open Giraffe
open Microsoft.AspNetCore.Http
open FunPizzaShop.Server.Views

let webApp (env: #_) (layout: HttpContext -> (int -> Task<string>) -> string Task) =
    let viewRoute view =
        fun (next: HttpFunc) (ctx: HttpContext) ->
            task {
                let! lay = (layout ctx (view ctx))
                return! htmlString lay next ctx
            }

    let defaultRoute = viewRoute (Index.view env)

    choose [
        routeCi "/checkout" >=> defaultRoute
        routeCi "/" >=> defaultRoute
    ]

// Onur recalls there is some flexibility it offers... could be redundant in this project.
// int part is the heading level. see other comment about being nice to screen readers.
let webAppWrapper (env: #_) (layout: HttpContext -> (int -> Task<string>) -> string Task) =
    fun (next: HttpFunc) (context: HttpContext) -> task {
        return! webApp env layout next context
     }
