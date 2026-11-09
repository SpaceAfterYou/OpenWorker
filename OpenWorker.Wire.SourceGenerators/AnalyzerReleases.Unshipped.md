; Unshipped analyzer release
; https://github.com/dotnet/roslyn/blob/main/src/RoslynAnalyzers/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
OWW001 | OpenWorker.Wire | Error | A wire message must be partial
OWW002 | OpenWorker.Wire | Error | A field has no wire format
OWW003 | OpenWorker.Wire | Error | A wire message writes its own bridge
OWW004 | OpenWorker.Wire | Error | A wire message has a shape the generator does not take
