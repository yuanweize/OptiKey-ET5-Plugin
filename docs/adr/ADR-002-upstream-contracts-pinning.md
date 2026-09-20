# ADR-002: Upstream Contracts Pinning and Rejection of Standalone Re-definitions

## Status
Accepted (Supercedes and removes initial draft Option B)

## Context
OptiKey defines its plugin contract in a separate assembly project: `JuliusSweetland.OptiKey.Contracts`.
The primary interfaces are:
- `JuliusSweetland.OptiKey.Contracts.IPointService`
- `JuliusSweetland.OptiKey.Contracts.INotifyErrors`

When designing the build pipeline for `OptiKey-ET5-Plugin`, two approaches were evaluated:
- **Option A**: Pin to upstream OptiKey source, compile `JuliusSweetland.OptiKey.Contracts.dll` in CI, and reference the compiled assembly as a binary dependency.
- **Option B (REJECTED)**: Re-create identical interface definitions inside the plugin project with matching namespaces and member signatures.

## Decision
**Option B is categorically rejected.**

Even if the namespace, interface name, and method signatures match character-for-character, the .NET Common Language Runtime (CLR) determines type identity by `[AssemblyQualifiedName]`:
`Namespace.InterfaceName, AssemblyName, Version, Culture, PublicKeyToken`

If the plugin defines its own `IPointService`, OptiKey's `DllLoader`:
```csharp
typeToLoad = assembly.GetTypes().FirstOrDefault(t => t.GetInterfaces().Contains(typeof(IPointService)));
```
will evaluate `Contains(typeof(JuliusSweetland.OptiKey.Contracts.IPointService, JuliusSweetland.OptiKey.Contracts))` to `false` because the loaded type implements `JuliusSweetland.OptiKey.Contracts.IPointService, OptiKey.ET5.Plugin`.
This results in OptiKey reporting:
`No IPointService implementation found in OptiKey.ET5.Plugin.dll`

### Pinning & CI Rules
1. **Pinned Stable Reference**: The production build links strictly against `JuliusSweetland.OptiKey.Contracts.dll` built from the pinned stable tag recorded in `OPTIKEY_CONTRACT_REF` (currently `v4.2.2`, commit `ebbbef7bbac5e2dab0e255e5a57acf163d515ca7`).
2. **Dual-Target CI Matrix**:
   - Job 1: Build and test against `PINNED_COMMIT` (`v4.2.2`). This is the gate for release artifacts.
   - Job 2: Build and test against upstream `main` (`HEAD`). This provides immediate alerts if upstream OptiKey changes contract definitions or reactive dependencies in unreleased commits.
3. **No Contract Bundling in Release ZIP**: Since OptiKey's host application already loads `JuliusSweetland.OptiKey.Contracts.dll` into the AppDomain, the plugin ZIP asset should not ship a duplicate copy unless proven strictly necessary to prevent version skew.

## Consequences
- 100% CLR type identity compatibility with OptiKey 4.x runtime.
- Automated warning pipeline for upcoming upstream breaking changes.
- Reproducible, auditable release binaries.
