﻿namespace CollectionSample

open Gjallarhorn
open CollectionSample.RequestModel
open CollectionSample.External
    

// Create an application wide model+ msg + update which composes 
// multiple models
type Model = { Requests : Requests ; AddingRequests : ExecutionStatus ; Processing : ExecutionStatus }
    
type Msg =
    | AddRequests of SetExecutionState
    | ProcessRequests of SetExecutionState
    | Update of Operations.Update

// Type that allows us to manage the state external of the basic application framework.
type StateManagement (fnAccepted : Request -> unit , fnRejected : Request -> unit, add, proc) =

    let updateRequests = Operations.update fnAccepted fnRejected 
    let updateAdd (c : Model) msg = Execution.update add msg c.AddingRequests
    let updateProcess (c : Model) msg = Execution.update proc msg c.Processing
    
    let update (msg : Msg) (current : Model) = 
        match msg with
        | Msg.AddRequests b -> { current with AddingRequests = updateAdd current b }
        | Msg.ProcessRequests b -> { current with Processing = updateProcess current b }
        | Msg.Update u -> { current with Requests = updateRequests u current.Requests }

    let initialModel = 
        { 
            Requests = [] 
            AddingRequests = { Operating = None } 
            Processing = { Operating = None } 
        }

    let state = new AsyncMutable<Model>(initialModel)

    // Update the current state given a message
    member __.Update msg = 
        update msg |> state.Update |> ignore

    // Gets the state as a Signal
    member __.ToSignal () = state :> ISignal<_> 

    // Initialization function - Kick off our routines to add and remove data
    member this.Initialize () =
        // Start updating and processing
        Executing true |> AddRequests |> this.Update
        Executing true |> ProcessRequests |> this.Update
