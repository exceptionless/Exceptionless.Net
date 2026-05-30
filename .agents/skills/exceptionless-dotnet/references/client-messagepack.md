# Exceptionless.MessagePack Storage Serializer


## When To Use

Use `Exceptionless.MessagePack` when the local persisted queue needs a MessagePack storage serializer instead of the default JSON storage serializer. This affects local storage serialization, not the public collector API.

## Install

```bash
dotnet add package Exceptionless.MessagePack
```

## Setup

```csharp
using Exceptionless;
using Exceptionless.MessagePack;

var client = new ExceptionlessClient(c => {
    c.ApiKey = "VALID_API_KEY_12345";
    c.UseFolderStorage("exceptionless-queue");
    c.UseMessagePackSerializer();
});

client.SubmitLog("MessagePack queue serializer configured.");
await client.ProcessQueueAsync();
```

## Tradeoffs

- MessagePack can reduce local queue size and serialization cost.
- It makes queued files less human-readable than JSON.
- It should be configured before any queue storage is used.

## Best Practices

- Use with folder storage for durable queues where performance or size matters.
- Avoid switching serializers on an existing queue directory that contains old unsent JSON payloads.
- Keep serializer configuration near storage configuration so future maintainers understand the queue format.
