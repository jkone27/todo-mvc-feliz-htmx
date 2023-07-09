#load "runtime-scripts/Microsoft.AspNetCore.App-7.0.5.fsx"
#r "nuget: Saturn"
#r "nuget:Feliz.ViewEngine.Htmx"

open Feliz.ViewEngine
open Feliz.ViewEngine.Htmx
open Microsoft.AspNetCore.Builder
open System
open Giraffe.EndpointRouting
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.DependencyInjection.Extensions
open Saturn
open Saturn.Endpoint

let body = 
    [
        Html.h1 "HTMX is COOL"
        Html.button [
            hx.get "/clicked"
            hx.swap.outerHTML
            hx.trigger "click"
            hx.target "#result"
            prop.text "HTTP GET TO SERVER HTML RESPONSE"
        ]
        Html.div [
            prop.id "result"
        ]
    ]

let mainLayout = 
    Html.html [
        Html.head [
            Html.title "F# ♥ Htmx"
            Html.script [ prop.src "https://unpkg.com/htmx.org@1.6.0" ]
            Html.meta [ prop.charset.utf8 ]
        ]
        Html.body body
    ]
    |> Render.htmlView 

let ssr = 
    Html.ul [
        Html.li "H"
        Html.li "T"
        Html.li "M"
        Html.li "X"
    ]
    |> Render.htmlView

// giraffe endpoints
let endpointList = 
    [
        GET [ 
            route "/" (Giraffe.Core.htmlString mainLayout)
            route "/clicked" (Giraffe.Core.htmlString ssr)
        ]
    ]

//saturn routes
let saturnRouter = router {
        get "/" (Giraffe.Core.htmlString mainLayout)
        get "/clicked" (Giraffe.Core.htmlString ssr)
    }

let appRouter = router { forward "" saturnRouter } 

//let builder = WebApplication.CreateBuilder()
//let app = builder.Build()

// STANDARD minimal api
//app.MapGet("/", Func<_>(fun () -> mainLayout)) |> ignore
//app.MapGet("/clicked", Func<_>(fun () -> ssr)) |> ignore

// Giraffe
//app.UseRouting()
//app.UseGiraffe(endpointList)

//app.Run()

// saturn
let app = application {
        
        use_endpoint_router appRouter 
    }
    
    // once the app is built, just run it
run app