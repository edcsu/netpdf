using Xunit;

// Run tests serially. Several tests are CPU-bound PDF/Avalonia rendering
// (heaviest on net10.0, where the previewer MainViewModel tests also run).
// Under xUnit's default parallelism on small CI runners those saturate the
// ThreadPool, starving PreviewerClient.Ping()'s short-timeout localhost HTTP
// call so it spuriously reports the previewer as not running. Serial execution
// removes the contention and keeps these tests deterministic in CI.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
