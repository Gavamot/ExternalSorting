``` ini

BenchmarkDotNet=v0.13.1, OS=Windows 10.0.18363.1316 (1909/November2019Update/19H2)
AMD FX(tm)-8300, 1 CPU, 8 logical and 4 physical cores
.NET SDK=6.0.100
  [Host]     : .NET 6.0.0 (6.0.21.52210), X64 RyuJIT
  DefaultJob : .NET 6.0.0 (6.0.21.52210), X64 RyuJIT


```
|    Method |       Mean |    Error |    StdDev |     Median |      Gen 0 |     Allocated |
|---------- |-----------:|---------:|----------:|-----------:|-----------:|--------------:|
|  HeapSort |   413.1 ms |  6.25 ms |   5.85 ms |   415.2 ms |          - |         480 B |
| MergeSort | 1,581.3 ms | 21.20 ms |  17.70 ms | 1,587.5 ms | 30000.0000 | 183,611,848 B |
|   TimSort | 1,581.8 ms | 61.12 ms | 176.34 ms | 1,513.9 ms | 17000.0000 | 121,344,784 B |
