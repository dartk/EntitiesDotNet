using BenchmarkDotNet.Running;
using EntitiesDotNet.Benchmarks;


// var benchmark = new CalculateFloatTranslationBenchmark()
// {
//     N = 10
// };
// benchmark.GlobalSetup();
// benchmark.ReadWriteNative();

BenchmarkRunner.Run(typeof(TranslationBenchmark));