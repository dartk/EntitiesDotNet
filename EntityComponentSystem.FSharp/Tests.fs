module Tests
//
// #nowarn "3391"
//
//
// open System
// open System.ComponentModel
// open System.Numerics
// open System.Runtime.CompilerServices
// open Xunit
// open EntityComponentSystem
// open Xunit.Abstractions
//
//
// let inline foreach func (array: ComponentArray) =
//     let length = array.Count
//     let span0 = array.GetSpan<'a>()
//     let span1 = array.GetSpan<'b>()
//     for i = 0 to length do
//         func span0[i] span1[i]
//     ()
//     
//     
// type Foo =
//     static member inline ForEach (a: 'a byref -> unit) (array: ComponentArray) = ()
//     
// let execute (a: int byref) (b: float byref) =
//     ()
//
// let foo (array: ComponentArray) =
//     ()
//     
//
//
// [<Struct>]
// type Struct =
//     val mutable Int: int
//     new(i: int) = { Int = i }
//     
//     override this.ToString() = this.Int.ToString()
//     
//     
//
//
//
// [<IsReadOnly; Struct>]
// type Translation =
//     { Translation: Vector3 }
//
//     static member op_Implicit(value: Translation) : Vector3 = value.Translation
//     static member op_Implicit(value: Vector3) : Translation = { Translation = value }
//
//
//
// [<IsReadOnly; Struct>]
// type Velocity =
//     val Velocity: Vector3
//     new(v: Vector3) = { Velocity = v }
//     override this.ToString() = this.Velocity.ToString()
//
//
// type Tests(output: ITestOutputHelper) =
//
//     [<Fact>]
//     let ``struct equality test`` () =
//         let a = Struct 10
//         let mutable b = Struct 10
//         Assert.Equal(a, b)
//         b.Int <- 11
//         Assert.NotEqual(a, b)
//         $"%A{b}" |> output.WriteLine
//         ()
//
//
//     [<Fact>]
//     let ``My test`` () =
//         let array = ComponentArray(Archetype<Translation, Velocity>.Instance, 10)
//         array.Add 10
//
//         array.ForEach(fun (i: int) (velocity: Velocity byref) ->
//             velocity <- float32 i |> Vector3 |> Velocity)
//
//         array.ForEach(fun (v: Velocity inref) (t: Translation byref) ->
//             t <- { Translation = t.Translation + 1f / 60f * v.Velocity })
//
//         array.ToReadableString() |> output.WriteLine
