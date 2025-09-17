namespace OpenWorker.Hotspot.Handler;

internal readonly record struct Handler(Type Class, HandlerDelegate Delegate);

// https://youtu.be/ZJrEXthnycE