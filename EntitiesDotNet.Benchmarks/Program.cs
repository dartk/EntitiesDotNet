using BenchmarkDotNet.Running;
using EntitiesDotNet.Benchmarks;


BenchmarkRunner.Run(typeof(CalculateWorldTransformBenchmark));
// BenchmarkRunner.Run(typeof(ComponentSystemBenchmark));
