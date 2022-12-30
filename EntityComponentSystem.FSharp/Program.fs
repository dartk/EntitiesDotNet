namespace EntityComponentSystem.FSharp

open System
open System.Numerics
open System.Runtime.CompilerServices
open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Running
open EntityComponentSystem


[<IsReadOnly; Struct>]
type TranslationRecord = { Translation: Vector3 }

[<IsReadOnly; Struct>]
type VelocityRecord = { Velocity: Vector3 }

[<Struct>]
type TranslationStruct =
    val mutable Translation: Vector3

[<Struct>]
type VelocityStruct =
    val mutable Velocity: Vector3


type FSharpIterationBenchmark() =
    let N = 10000

    let array =
        ComponentArray(
            Archetype<VelocityRecord, TranslationRecord, VelocityStruct, TranslationStruct>
                .Instance,
            N
        )

    do array.Add N


    [<Benchmark>]
    member this.ForEachRecord() =
        array.ForEach(fun (v: VelocityRecord inref) (t: TranslationRecord byref) ->
            t <- { Translation = t.Translation + 1f / 60f * v.Velocity })


    [<Benchmark>]
    member this.ManualStruct() =
        let selection = array.Read<VelocityStruct>().Write<TranslationStruct>()
        let mutable velocity = ReadOnlySpan<VelocityStruct>()
        let mutable translation = Span<TranslationStruct>()
        let mutable count = 0
        selection.Deconstruct(&count, &velocity, &translation)

        for i = 0 to count - 1 do
            let t = &translation[i].Translation
            let v = &velocity[i].Velocity
            t <- t + 1f / 60f * v


    [<Benchmark>]
    member this.ForEachStruct() =
        array.ForEach(fun (v: VelocityStruct inref) (t: TranslationStruct byref) ->
            t.Translation <- t.Translation + 1f / 60f * v.Velocity)


module Program =
    [<EntryPoint>]
    let main _ =
        BenchmarkRunner.Run(typeof<FSharpIterationBenchmark>) |> ignore
        0
