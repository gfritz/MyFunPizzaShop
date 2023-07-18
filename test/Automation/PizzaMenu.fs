module FunPizzaShop.Automation.PizzaMenu

open FunPizzaShop
open Microsoft.Playwright
open TickSpec
open Microsoft.Extensions.Hosting

open type Microsoft.Playwright.Assertions

// these When/Then names must exactly match the *.feature file
[<When>]
let ``I get the main menu`` (context: IBrowserContext) =
    (task {
        let! page = context.NewPageAsync()
        let! _ = page.GotoAsync("http://localhost:5010")
        return page
    }).Result

// page comes from the When above, but if a previous method does not return it,
// then TickSpec will try to create an instance and fail
[<Then>]
let ``pizza items should be fetched`` (page: IPage) =
    (task {
        // admittedly, checking for specific elements is brittle
        // you should check for semantics by roles
        // but checking for css class selector is a pragmatic check
        // note: by itself this obviously doesn't guarantee
        // note: playwright typically waits to find elements that it can't find
        // but, for some reason, when we check for count, so it doesn't wait
        let! pizzaItems = page.QuerySelectorAllAsync("fps-pizza-item")
        if pizzaItems.Count = 0 then
            failwith "No pizza items found"
        else
            return ()
    }).Wait()
