// Arch keeps the component types of every World in one static registry that is not thread safe: tests that
// create entities at the same time crash the process with an access violation. The tests also use real
// sockets and timers, which a parallel run would load unevenly.
[assembly: DoNotParallelize]
