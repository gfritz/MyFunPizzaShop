module FunPizzaShop.Server.Views.Index

open Common
open Microsoft.AspNetCore.Http

let view (env:#_) (ctx:HttpContext) (dataLevel: int) = task {
    return
        html $""" Hello world! """
}
