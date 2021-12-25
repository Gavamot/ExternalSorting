``` ini

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.18363.1316 (1909/November2019Update/19H2)
AMD FX(tm)-8300, 1 CPU, 8 logical and 4 physical cores
.NET SDK=6.0.100
  [Host]     : .NET 6.0.0 (6.0.21.52210), X64 RyuJIT
  DefaultJob : .NET 6.0.0 (6.0.21.52210), X64 RyuJIT


```
|        Method |     Mean |     Error |    StdDev | Allocated |
|-------------- |---------:|----------:|----------:|----------:|
| TestBenchMark | 2.895 ms | 0.0231 ms | 0.0181 ms |       2 B |
