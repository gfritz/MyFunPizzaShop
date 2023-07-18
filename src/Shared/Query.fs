// dubious about this being in Shared because we aren't going to query these things through the client
// so why expose these to the client at all

module FunPizzaShop.Shared.Query

module Pizza =
    open Model
    open Pizza

    // see blog.ploeh.dk IO Surrogates
    // unit -> * is impure, so too is * -> unit
    // it would be nice to decorate things that we know are impure
    //
    // why are these Async? isn't the domain supposed to be agnostic of that?
    // for browser code which this is, we cannot make the browser wait.
    // further, Async helps decorate/indicate these are impure methods.
    // it is also an impure function because a pure function cannot take in unit.
    //
    // why Async not Task?
    // for server side, either should be fine.
    // Async is more f# idiomatic. Async does not automatically start. Task does.
    // Async is pure as long as it run inside the Async compute. Async<T> is an expression.
    //
    // for client side, and dogma aside, we know we will be using Fable, and Task just won't work
    type GetSpecials = unit -> Async<PizzaSpecial list>
    type GetToppings = unit -> Async<Topping option>
