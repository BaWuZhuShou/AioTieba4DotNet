# ProtoGenerator Guide

## OVERVIEW
This directory contains the handwritten protobuf generator for the maintained v3 library. Its job is to scan `AioTieba4DotNet/Api/**/*.proto` and regenerate adjacent C# outputs.

## RULES
- Treat `.proto` files as the source of truth and generated `*.cs` files as derived outputs.
- Keep generator changes focused on discovery, planning, protoc execution, and reproducible output behavior.
- Do not patch generated protobuf C# by hand to fix downstream issues. Fix the `.proto` or the generator, then regenerate.
- Preserve the self-contained bundled-`protoc` behavior wired through `Google.Protobuf.Tools` and the project build target.
- If generator behavior changes in a durable way, sync the cross-cutting rule back to `.junie/guidelines.md`.
