using OpenWorker.Batch;

namespace OpenWorker.Hotspot;

internal readonly record struct BatchResult(VBatchFile File, BatchType Type);