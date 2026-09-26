# Models, Mapping, and Protobuf Generation

## Public Model Boundary

[Library policy](../../../AioTieba4DotNet/AGENTS.md) requires protocol-independent public DTOs with properties rather than public data fields. Reuse models for the same consumer concept; split only for meaningful semantic differences. [UserInfoT](../../../AioTieba4DotNet/Models/Threads/UserInfoT.cs) extends shared user data with thread-author fields.

Keep conversion under `AioTieba4DotNet/Internal/Mapping/`; mapper namespaces remain `AioTieba4DotNet.Internal.Mapping` despite deeper directories. Avoid protobuf types, `JObject`, and endpoint decoding in public DTO constructors/signatures.

[ThreadsMapper](../../../AioTieba4DotNet/Internal/Mapping/Models/Threads/ThreadsMapper.cs) joins users, attaches forum identity, and initializes required properties of [Threads](../../../AioTieba4DotNet/Models/Threads/Threads.cs). Preserve documented null/default behavior; endpoint fields differ.

## Shared Conversion Rules

[UserProtoMapping](../../../AioTieba4DotNet/Internal/Mapping/Models/Shared/UserProtoMapping.cs) centralizes portrait normalization, invariant numeric parsing, icons, and flags. Its numeric fallback is intentional:

```csharp
return long.TryParse(tiebaUid, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
    ? parsed
    : 0;
```

Reuse these conversions rather than culture-dependent parsing or new substring heuristics. Do not spread tolerant parsing to fields requiring an error. [MappingCoverageContractTests](../../../AioTieba4DotNet.Tests.Governance/Contracts/MappingCoverageContractTests.cs) asserts user fields, invalid numeric defaults, portrait normalization, and missing-link fallbacks. Assert results, not only absence of exceptions.

## Generated Output Contract

- API `.proto` files are editable source. Shared imports live in `AioTieba4DotNet/Api/Protobuf/`; family assets live beside endpoints, such as [FrsPageReqIdl.proto](../../../AioTieba4DotNet/Api/GetThreads/Protobuf/FrsPageReqIdl.proto).
- Generated adjacent C# is derived. Change schemas/generator and regenerate; never patch generated classes or expose them publicly.
- [ProtoGenerationPlanner](../../../ProtoGenerator/ProtoGenerationPlanner.cs) finds the exact `AioTieba4DotNet.sln` root marker, recursively scans API `.proto` files, and sorts normalized relative paths ordinally. Outputs go beside source schemas.
- [ProtocExecutor](../../../ProtoGenerator/ProtocExecutor.cs) uses `ProcessStartInfo.ArgumentList`, local/shared import paths, and `--csharp_opt=serializable,internal_access`. Preserve argument boundaries and visibility.
- [ProtoGenerator.csproj](../../../ProtoGenerator/ProtoGenerator.csproj) bundles the `Google.Protobuf.Tools` compiler; current paths select x64 tools for Windows/Linux/macOS. Do not promise architecture support beyond this wiring. [ProtoGeneratorApp](../../../ProtoGenerator/ProtoGeneratorApp.cs) prefers the bundled executable but falls back to PATH if absent.

After authorized schema/generator changes, run from the repository root:

```bash
dotnet run --project ProtoGenerator/ProtoGenerator.csproj
```

This writes generated source and requires the repository SDK/restored dependencies. Review diffs and rebuild affected projects. The app returns nonzero for failed targets and propagates cancellation. Do not generate for prose-only changes.

The [generator README](../../../ProtoGenerator/README.md) summarizes only `serializable` and generic solution discovery; code additionally requires `internal_access` and the exact solution marker. Sync [generator policy](../../../ProtoGenerator/AGENTS.md) and cross-cutting guidance when durable behaviors change.
