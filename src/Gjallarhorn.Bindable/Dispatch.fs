﻿namespace Gjallarhorn.Bindable

open Gjallarhorn

open System
open System.Threading

/// Represents a Dispatch of a single message
type Dispatch<'Msg> = 'Msg -> unit

/// Allows dispatching of a message via an Observable
type Dispatcher<'Msg> () =
    let evt = Event<'Msg>()
    let trigger: Dispatch<_> = fun msg -> evt.Trigger msg

    // Trigger our message
    member __.Dispatch msg = trigger msg

    interface IObservable<'Msg> with
        member __.Subscribe (o : IObserver<'Msg>) = evt.Publish.Subscribe o

/// Manages the execution of an operation that produces messages to be dispatched
type Executor<'Msg> (startExecuting : Dispatch<'Msg> -> CancellationToken -> unit) = 
    let executing = Mutable.create false
    
    let dispatch = Dispatcher<'Msg>()

    let mutable cts = null
    let changeState run =
        if run then
            cts <- new CancellationTokenSource()
            startExecuting dispatch.Dispatch cts.Token
        else
            cts.Cancel()
            cts <- null

    let subscription = executing |> Signal.Subscription.create changeState

    /// Used to watch our execution status
    member __.Executing with get () = executing :> ISignal<_>

    /// Attempt to start the operation
    member __.Start() = executing.Value <- true
    
    /// Attempt to stop the operation
    member __.Stop()  = executing.Value <- false

    interface IObservable<'Msg> with
        member __.Subscribe (o : IObserver<'Msg>) = (dispatch :> IObservable<'Msg>).Subscribe o
    interface IDisposable with
        member __.Dispose() = 
            subscription.Dispose()
            if not(isNull cts) then
                cts.Dispose()


