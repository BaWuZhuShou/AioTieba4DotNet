# Library and Tooling Guidelines

This directory covers the maintained v3 .NET 10 client library, its protobuf generator, and test support. The `backend` name is a Trellis routing convention; this is not an ASP.NET service and has no database layer. Documentation-site work belongs in the [frontend guidelines](../frontend/index.md).

## Guidelines Index

| Guide | Read when changing |
| --- | --- |
| [Directory Structure](./directory-structure.md) | Package boundaries, composition, public contracts, or feature placement |
| [Transport and Requests](./transport-guidelines.md) | Packing, HTTP/WebSocket selection, signing, or retries |
| [Models and Code Generation](./mapping-and-codegen.md) | DTOs, response mapping, `.proto` files, or ProtoGenerator |
| [Session and Cache Ownership](./session-and-cache.md) | Authentication, lifecycle state, resources, or forum lookups |
| [Error Handling](./error-handling.md) | Validation, server errors, cancellation, or fallback |
| [Logging Guidelines](./logging-guidelines.md) | Optional file logging or diagnostic output |
| [Quality Guidelines](./quality-guidelines.md) | Code style, tests, local verification, CI, or documentation updates |

## Pre-Development Checklist

- Read [library policy](../../../AioTieba4DotNet/AGENTS.md) and relevant guides above; for generator changes also read [ProtoGenerator policy](../../../ProtoGenerator/AGENTS.md).
- Check [cross-cutting policy](../../../.junie/guidelines.md), public contracts, and the upstream family in the [parity ledger](../../../docs/related/parity.md). The ledger owns implementation mappings and auth notes; the historical backlog does not.
- Trace the nearest contract/module/protocol/request/mapper path before adding helpers. Use the [reuse](../guides/code-reuse-thinking-guide.md) and [cross-layer](../guides/cross-layer-thinking-guide.md) guides when relevant.
- Distinguish handwritten and generated changes. Keep `.proto` and generated output updates together when generation is required.
- Choose verification matching the change. `safe` is online with real side effects; a documentation edit does not justify running it.

## Quality Check

- Preserve the six modules, .NET 10-only support, protocol-independent DTOs, and shared direct/DI/factory composition.
- Verify auth, cancellation, fallback, and mutation order where touched; do not broaden retries or normalization accidentally.
- Run applicable [quality checks](./quality-guidelines.md) and record actual results/prerequisites. Builds do not prove live parity or coverage.
- Align public usage docs, parity/auth notes, consumer skills, and release/migration notes when contracts change.
- For spec-only edits, check references, indexes, scaffold text, and whitespace; do not generate protocols or execute live fixtures for prose validation.
