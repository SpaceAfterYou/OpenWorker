# OpenWorker

A server emulator for **SoulWorker**, written in C# on .NET 10. It answers what game client **1.4.2.3** sends: the
client decides every wire format, and the server is built to match it.

> **Status: in development.** The servers run, a character can be made and played through districts and mazes, but the
> project has not reached alpha: features are missing and data is not kept between releases.

```bash
dotnet build OpenWorker.slnx
dotnet test --solution OpenWorker.slnx
```
