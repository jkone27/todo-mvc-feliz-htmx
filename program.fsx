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


type Todo = { Id: int; Text: string; Completed: bool }

type Model = { Todos: Todo list }


let toHtml (view: ReactElement) = 
    view
    |> Render.htmlView 
    |> htmlString

let listToHtml (view: ReactElement list) = 
    view
    |> Render.htmlView 
    |> htmlString


type TodoRepository() =

    let todos = new ResizeArray<Todo>()

    member this.GetTodos() =
        todos |> List.ofSeq

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

let todoRepository = new TodoRepository()


// useful! https://thisfunctionaltom.github.io/Html2Feliz/

let body =
    [
        Html.section [
                prop.className "todoapp"
                prop.children [
                    Html.header [
                        prop.className "header"
                        prop.children [
                            Html.h1 "todos"
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
                    Html.footer [
                        prop.className "footer"
                        hx.get "/todos"
                        hx.target "#todo-list"
                        prop.children [
                            Html.span [
                                prop.className "todo-count"
                                hx.indicator "footer"
                                hx.swap "outerHTML"
                                prop.text (todoRepository.GetTodos().Length)
                            ]
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

// let oldTodoLi item =
//     Html.li [
//         prop.id $"todo-{item.Id}"
//         prop.className (if item.Completed then "completed" else "not-completed")
//         prop.children [
//             Html.input [ 
//                 prop.type' "checkbox"
//                 prop.value true
//                 prop.text "Completed" 
//                 hx.trigger "click"
//                 hx.post $"/todos/{item.Id}"
//                 //prop.onClick (fun _ -> dispatch (ToggleTodo todo.Id))
//             ]
//             Html.div item.Text
//                 // prop.onBlur (fun text -> dispatch (UpdateTodoText (todo.Id, text))) 
//             Html.button [ 
//                 //prop.onClick (fun _ -> dispatch (RemoveTodo todo.Id))
//                 prop.text "Delete"
//                 hx.delete $"/todos/{item.Id}"
//             ]
//         ]
//     ]


let currentTodos () = 
    if todoRepository.GetTodos().Length = 0 then
        [ ] 
    else [
            for item in todoRepository.GetTodos() do
                todoLi item
        ] 
    |> listToHtml

let addTodo (httpFunc: HttpFunc) (ctx: HttpContext) =
    task {
        let! formCollection = ctx.Request.ReadFormAsync()
        let v = formCollection |> System.Text.Json.JsonSerializer.Serialize
        printfn $"got: {v}"
        let value = formCollection["title"] |> Seq.head
        let newTodo = todoRepository.AddTodo(value)
        
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
        
        let updatedTodo = todoRepository.UpdateTodo id value completed
        
        let updatedTodoHtml = todoLi updatedTodo |> toHtml
        
        return! updatedTodoHtml httpFunc ctx
    }

let toggle (id: int) (httpFunc: HttpFunc) (ctx: HttpContext) =
    task {
        
        let updatedTodo = todoRepository.ToggleTodo id

        let updatedTodoHtml = todoLi updatedTodo |> toHtml
        
        return! updatedTodoHtml httpFunc ctx
    }


let toggleAll (httpFunc: HttpFunc) (ctx: HttpContext) =
    task {
        
        todoRepository.ToggleAll() |> ignore
        
        let todosHtml = currentTodos()
        
        return! todosHtml httpFunc ctx
    }

let deleteTodo (id: int) (httpFunc: HttpFunc) (ctx: HttpContext) =
    task {

        todoRepository.DeleteTodo id |> ignore

        printf $"count: {todoRepository.GetTodos().Length}"
        
        let empty = [] |> listToHtml
        
        return! empty httpFunc ctx
    }

let getTodos (httpFunc : HttpFunc) (ctx: HttpContext) = 
    task {
        let todosHtml = currentTodos()
        
        return! todosHtml httpFunc ctx
    }


//saturn routes
let endpoints = 
    router {
        get "/" mainLayout
        post "/todos" addTodo
        get "/todos" getTodos
        postf "/todos/%i/toggle" toggle
        post "/todos/toggle-all" toggleAll
        postf "/todos/edit/%i" updateTodo
        deletef "/todos/%i" deleteTodo
    }

let app =
    application {
        use_endpoint_router endpoints
    }

run app
