#load "runtime-scripts/Microsoft.AspNetCore.App-7.0.5.fsx"
#r "nuget: Saturn"
#r "nuget: Feliz.ViewEngine.Htmx"

open Microsoft.AspNetCore.Builder
open Feliz.ViewEngine
open Feliz.ViewEngine.Htmx
open Giraffe
open Giraffe.EndpointRouting
open Saturn
open Saturn.Endpoint
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection


[<AutoOpen>]
module Domain =

    type Todo = { Id: int; Text: string; Completed: bool }

    type Model = { Todos: Todo list }


[<AutoOpen>]
module Services = 

    type TodoRepository() =

        let todos = new ResizeArray<Todo>()

        member this.GetTodos(?filterPredicate: Todo -> bool) =
            let filter = 
                if filterPredicate.IsSome then 
                    filterPredicate.Value
                else
                    (fun _ -> true) 

            todos |> Seq.filter(filter) |> List.ofSeq

        member this.AddTodo todoVal =
            let newTodo = { Id = todos.Count + 1; Text = todoVal ; Completed = false }
            todos.Add(newTodo)
            newTodo

        member this.DeleteTodo id =
            let foundTodo = todos.Find(fun t -> t.Id = id)
            todos.Remove(foundTodo)

        member this.ClearAllTodos() =
            todos.Clear()

        member this.UpdateTodo id todoVal completed =
            let foundTodo = todos.Find(fun t -> t.Id = id)
            let updatedTodo = { foundTodo with Text = todoVal ; Completed = completed }
            todos.Add(updatedTodo)
            todos.Remove(foundTodo) |> ignore
            updatedTodo

        
        member this.ToggleTodo id =
            let foundTodo = todos.Find(fun t -> t.Id = id)
            let updatedTodo = { foundTodo with Completed = ( foundTodo.Completed |> not) }
            todos.Add(updatedTodo)
            todos.Remove(foundTodo) |> ignore
            updatedTodo

        member this.ToggleAll() =

            let newTodos = 
                todos 
                |> Seq.map (fun foundTodo -> 
                    let notCompleted = foundTodo.Completed |> not
                    { foundTodo with Completed = notCompleted }
                )
                |> ResizeArray

            todos.Clear()
            todos.AddRange(newTodos)
            todos


[<AutoOpen>]
module View = 

    // useful! https://thisfunctionaltom.github.io/Html2Feliz/

    let toHtml (view: ReactElement) = 
        view
        |> Render.htmlView 
        |> htmlString

    let listToHtml (view: ReactElement list) = 
        view
        |> Render.htmlView 
        |> htmlString
    
    let inputForm =
        Html.form [
            hx.indicator "todo-form"
            hx.post "/todos"
            hx.swap "afterbegin"
            //hx.trigger "keyup[keyCode==13]"
            hx.target "#todo-list"
            prop.id "todo-form"
            prop.children [
                Html.input [
                    prop.id "new-todo"
                    prop.className "new-todo"
                    prop.name "title"
                    prop.placeholder "What needs to be done?"
                    prop.autoFocus true
                ]
            ]
        ]

    let filters =
        Html.ul [
            prop.className "filters"
            prop.children [
                Html.li [
                    Html.a [
                        prop.href "/todos"
                        hx.swap "outerHTML"
                        hx.target "#todo-list"
                        prop.text "All"
                    ]
                ]
                Html.li [
                    Html.a [
                        prop.href "/todos/active"
                        hx.swap "outerHTML"
                        hx.target "#todo-list"
                        prop.text "Active"
                    ]
                ]
                Html.li [
                    Html.a [
                        prop.href "/todos/completed"
                        hx.swap "outerHTML"
                        hx.target "#todo-list"
                        prop.text "Completed"
                    ]
                ]
            ]
        ]

    let todosCount count =
        Html.span [
            prop.id "todos-count"
            prop.text 0
        ]

    let todoListFooter =
        Html.footer [
            prop.className "footer"
            hx.get "/todos"
            hx.target "#todo-list"
            prop.children [
                Html.span [
                    prop.className "todo-count"
                    hx.indicator "footer"
                    hx.swap "outerHTML"
                    prop.children [
                        todosCount 0
                    ]
                ]
                filters
                Html.button [
                    prop.className "clear-completed"
                    hx.confirm "Are you sure?"
                    hx.delete "/todos/completed"
                    hx.swap "outerHTML"
                    hx.target "#todo-list"
                    prop.text "Clear completed"
                ]
            ]
        ]

    let body =
        [
            Html.section [
                    prop.className "todoapp"
                    prop.children [
                        Html.header [
                            prop.className "header"
                            prop.children [
                                Html.h1 "todos"
                                inputForm
                            ]
                        ]
                        Html.section [
                            prop.className "main"
                            prop.children [
                                Html.input [
                                    prop.id "toggle-all"
                                    prop.className "toggle-all"
                                    prop.type' "checkbox"
                                    hx.post "/todos/toggle-all"
                                    hx.target "#todo-list"
                                ]
                                Html.label [
                                    prop.for' "toggle-all"
                                    prop.text "Mark all as complete"
                                ]
                                Html.ul [
                                    prop.id "todo-list"
                                    prop.className "todo-list"
                                ]
                            ]
                        ]
                        todoListFooter
                    ]
                ]
            Html.footer [
                    prop.className "info"
                    prop.children [
                        Html.p "Double-click to edit a todo"
                        Html.p [
                            Html.text "Created by "
                            Html.a [
                                prop.href "https://todomvc.com"
                                prop.text "TodoMVC"
                            ]
                        ]
                    ]
                ]
            // Html.script [ 
            //     prop.text " htmx.on('htmx:configRequest', function (evt) { var headers = evt.detail.headers || {}; headers['X-Requested-With'] = 'XMLHttpRequest'; evt.detail.headers = headers; });"
            // ]
        ]

    let mainLayout = 
        Html.html [
            Html.head [
                Html.title "F# ♥ Htmx - TODO MVC"
                Html.script [ prop.src "https://unpkg.com/htmx.org@1.6.0" ]
                Html.meta [
                    prop.charset "utf-8"
                ]
                Html.meta [
                    prop.name "viewport"
                    prop.content "width=device-width, initial-scale=1"
                ]
                Html.title "TodoMVC"
                Html.link [
                    prop.rel "stylesheet"
                    prop.href "https://unpkg.com/todomvc-common/base.css"
                ]
                Html.link [
                    prop.rel "stylesheet"
                    prop.href "https://unpkg.com/todomvc-app-css/index.css"
                ]
            ]
            Html.body body
        ]
        |> toHtml
        


        (**
        

        li(id='todo-' + todo.id, class={completed: todo.done === true})
    .view
        input.toggle(hx-patch='/todos/' + todo.id, type='checkbox', checked=todo.done, hx-target='#todo-' + todo.id, hx-swap="outerHTML")
        label(hx-get='/todos/edit/' + todo.id, hx-target="#todo-" +  todo.id, hx-swap="outerHTML") #{todo.name}
        button.destroy(hx-delete='/todos/' + todo.id, _="on htmx:afterOnLoad remove #todo-" + todo.id )
        
        *)

    let todoLi (todo: Todo) = 
        Html.li [
            prop.className (if todo.Completed then "completed" else "todo")
            prop.id $"todo-{todo.Id}"
            hx.trigger "load"
            hx.get "todos/count"
            hx.target "#todos-count"
            prop.children [
                Html.div [
                    prop.className "view"
                    prop.children [
                        Html.input [
                            prop.className "toggle"
                            // prop.custom ("hx-patch", $"/todos/{todo.Id}")
                            hx.post $"/todos/{todo.Id}/toggle"
                            prop.type' "checkbox"
                            prop.isChecked true
                            hx.target $"#todo-{todo.Id}"
                            hx.swap "outerHTML"
                        ]
                        Html.label [
                            hx.post $"/todos/edit/{todo.Id}"
                            hx.swap "outerHTML"
                            hx.target $"#todo-{todo.Id}"
                            prop.text todo.Text
                        ]
                        Html.button [
                            prop.className "destroy"
                            hx.swap "outerHTML"
                            hx.delete $"/todos/{todo.Id}"
                            hx.target $"#todo-{todo.Id}"
                            //hx.hyperscript  $"on htmx:afterOnLoad remove #todo-{todo.Id}"
                        ]
                    ]
                ]
            ]
        ]


    let currentTodos (repository : TodoRepository) = 

        let todos = repository.GetTodos()
        if todos.Length = 0 then
            [ ] 
        else [
                for item in todos do
                    todoLi item
            ] 
        |> listToHtml


[<AutoOpen>]
module Controllers =
    let addTodo (httpFunc: HttpFunc) (ctx: HttpContext) =
        task {
            let! formCollection = ctx.Request.ReadFormAsync()
            let v = formCollection |> System.Text.Json.JsonSerializer.Serialize
            printfn $"got: {v}"
            let value = formCollection["title"] |> Seq.head

            let repository = ctx.GetService<TodoRepository>()

            let newTodo = repository.AddTodo(value)
            
            let singleTodo = todoLi newTodo |> toHtml
            
            return! singleTodo httpFunc ctx
        }


    let updateTodo (id: int) (httpFunc: HttpFunc) (ctx: HttpContext) =
        task {
            let! formCollection = ctx.Request.ReadFormAsync()
            let v = formCollection |> System.Text.Json.JsonSerializer.Serialize
            printfn $"got: {v}"

            let value = formCollection["title"] |> Seq.head

            let completed = bool.Parse(formCollection["completed"] |> Seq.head)

            let repository = ctx.GetService<TodoRepository>()
            
            let updatedTodo = repository.UpdateTodo id value completed
            
            let updatedTodoHtml = todoLi updatedTodo |> toHtml
            
            return! updatedTodoHtml httpFunc ctx
        }

    let toggle (id: int) (httpFunc: HttpFunc) (ctx: HttpContext) =
        task {
            
            let repository = ctx.GetService<TodoRepository>()

            let updatedTodo = repository.ToggleTodo id

            let updatedTodoHtml = todoLi updatedTodo |> toHtml
            
            return! updatedTodoHtml httpFunc ctx
        }


    let toggleAll (httpFunc: HttpFunc) (ctx: HttpContext) =
        task {

            let repository = ctx.GetService<TodoRepository>()

            repository.ToggleAll() |> ignore
            
            let todosHtml = currentTodos repository
            
            return! todosHtml httpFunc ctx
        }

    let deleteTodo (id: int) (httpFunc: HttpFunc) (ctx: HttpContext) =
        task {

            let repository = ctx.GetService<TodoRepository>()

            repository.DeleteTodo id |> ignore

            printf $"count: {repository.GetTodos().Length}"
            
            let empty = [] |> listToHtml
            
            return! empty httpFunc ctx
        }

    let getTodos (httpFunc : HttpFunc) (ctx: HttpContext) = 
        task {
            let repository = ctx.GetService<TodoRepository>()

            let todosHtml = currentTodos repository
            
            return! todosHtml httpFunc ctx
        }

    let getActiveTodos (httpFunc : HttpFunc) (ctx: HttpContext) = 
        task {
            
            let repository = ctx.GetService<TodoRepository>()

            let todosHtml = 
                repository.GetTodos(fun t -> t.Completed = false) 
                |> List.map todoLi
                |> listToHtml
            
            return! todosHtml httpFunc ctx
        }

    let getCount (httpFunc : HttpFunc) (ctx: HttpContext) = 
        task {
            let repository = ctx.GetService<TodoRepository>()

            let totalTodos = repository.GetTodos().Length

            printfn $"todos: {totalTodos}"

            let resultSpan = View.todosCount totalTodos |> toHtml
            
            return! resultSpan httpFunc ctx
        }


//saturn routes
let endpoints = 
    router {
        get "/" mainLayout
        post "/todos" addTodo
        get "/todos" getTodos
        get "/todos/count" getCount
        get "/todos/active" getActiveTodos
        postf "/todos/%i/toggle" toggle
        post "/todos/toggle-all" toggleAll
        postf "/todos/edit/%i" updateTodo
        deletef "/todos/%i" deleteTodo
    }

let app =
    application {
        use_endpoint_router endpoints
        service_config (fun s -> s.AddSingleton(new TodoRepository()))
    }

run app
