module FunPizzaShop.Automation.Program

open System.Reflection
open TickSpec

[<EntryPointAttribute>]
let main _ =
    try
        try
            do
                let assembly = Assembly.GetExecutingAssembly()
                let definitions = StepDefinitions(assembly)

                // get PizzaMenu.feature from embedded resource
                ["PizzaMenu"]
                |> Seq.iter (fun source ->
                    let s = assembly.GetManifestResourceStream("Automation." + source + ".feature")
                    definitions.Execute(source, s))
            0
        with e ->
            printfn "%s" (e.ToString())
            // console exit code
            -1
    finally
        ()
        // host.StopAsync().Wait()
