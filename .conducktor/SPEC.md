# Always generate aliased usings for plugin image registrations

## Context

The `XrmPluginCore.SourceGenerator` ships analyzers + code-fix providers that wire up type-safe Pre/Post image handler signatures. When a `RegisterStep<TEntity, TService>(...)` registration declares an image via `WithPreImage`/`WithPostImage`/`AddImage` but the referenced handler method's signature does not match, two diagnostics/code-fixes come into play:

- **`FixHandlerSignatureCodeFixProvider`** (`XrmPluginCore.SourceGenerator/CodeFixes/FixHandlerSignatureCodeFixProvider.cs`) — fixes an existing handler method's parameter list to accept the registered `PreImage`/`PostImage` wrapper types. Fires on `DiagnosticDescriptors.HandlerSignatureMismatch.Id` and `DiagnosticDescriptors.HandlerSignatureMismatchError.Id`.
- **`CreateHandlerMethodCodeFixProvider`** (`XrmPluginCore.SourceGenerator/CodeFixes/CreateHandlerMethodCodeFixProvider.cs`) — creates a missing handler method on the service interface with the correct image parameters. Fires on `DiagnosticDescriptors.HandlerMethodNotFound.Id`.

Each registration's image wrapper classes (`PreImage`, `PostImage`) are generated into an isolated namespace produced by `RegisterStepHelper.GetExpectedImageNamespace(...)` (`XrmPluginCore.SourceGenerator/Helpers/RegisterStepHelper.cs:17`), with the shape:

```
{PluginNamespace}.PluginRegistrations.{PluginClassName}.{EntityTypeName}{Operation}{Stage}
```

e.g. `Some.Namespace.Plugins.PluginRegistrations.SomePlugin.LeadUpdatePostOperation`. The shared marker for these namespaces is the substring `.PluginRegistrations.` (`SyntaxFactoryHelper.IsImageRegistrationNamespace`, `SyntaxFactoryHelper.cs:207`). The alias used is the last namespace segment (`GetLastNamespaceSegment`, `SyntaxFactoryHelper.cs:212`), e.g. `LeadUpdatePostOperation`. Both `PreImage` and `PostImage` are simple type names (`Constants.PreImageTypeName = "PreImage"`, `Constants.PostImageTypeName = "PostImage"`), so two registrations in the same file both expose a `PreImage`/`PostImage` type.

**The bug:** Today the code fixes try to be clever — they only convert to aliased usings *when an ambiguity is detected*, otherwise they add a plain (non-aliased) `using`. This is driven by `SyntaxFactoryHelper.DetectImageAmbiguity(...)` (`SyntaxFactoryHelper.cs:118`):

- In `FixHandlerSignatureCodeFixProvider.FixMethodDeclarationsAsync` (lines 199–235): `ambiguity = DetectImageAmbiguity(...)`; `CreateImageParameterList(hasPreImage, hasPostImage, ambiguity.needsAlias ? ambiguity.alias : null)`; then `if (ambiguity.needsAlias) ConvertToAliasedUsingsAndQualifyRefs(...) else AddUsingDirectiveIfMissing(...)`.
- In `CreateHandlerMethodCodeFixProvider.CreateMethodAsync` (lines 135–152): same `DetectImageAmbiguity` / conditional pattern via `CreateMethodDeclaration(..., needsAlias ? alias : null)` and the same `if (needsAlias) ConvertToAliasedUsingsAndQualifyRefs else AddUsingDirectiveIfMissing` branch.

When multiple plugins use the **same service** and more than one of those registrations declares Pre/Post images, the automatic generation of usings and modification of parameters fails, because the plain-using path produces bare `PreImage`/`PostImage` references that collide across the multiple image namespaces, and the on-demand conversion logic (`ImageAmbiguityRewriter`, `SyntaxFactoryHelper.cs:234`) assumes "there should be exactly one existing plain image using at this point" (`SyntaxFactoryHelper.cs:309`, the `break;` after taking the first entry of `_typeToExistingAlias`). That assumption breaks with multiple image usings.

**The decision:** Stop trying to convert on demand. **Always** emit aliased usings and **always** qualify the image parameter types with the alias. This makes every emitted reference unambiguous regardless of how many same-service registrations exist in the file.

Existing tests assert the *old* behavior and must be updated. See:
- `XrmPluginCore.SourceGenerator.Tests/DiagnosticTests/FixHandlerSignatureCodeFixProviderTests.cs` — `Should_Fix_Signature_And_Add_Using_For_PreImage` (line ~61/64), `..._For_PostImage` (line ~114/117), `..._For_Both_Images` (line ~120), and `Should_Avoid_Ambiguous_Usings` (line 231).
- `XrmPluginCore.SourceGenerator.Tests/DiagnosticTests/CreateHandlerMethodCodeFixProviderTests.cs` (same directory).

## Goals

- Both code-fix providers **always** emit an **aliased** using directive of the form `using LeadUpdatePostOperation = Some.Namespace.Plugins.PluginRegistrations.SomePlugin.LeadUpdatePostOperation;` for the image namespace involved, never a plain `using Some.Namespace...PluginRegistrations...;`.
- Both code-fix providers **always** write image parameters with the alias prefix, e.g. `void OnUpdateLead(LeadUpdatePostOperation.PreImage preImage)` (and `LeadUpdatePostOperation.PostImage postImage`), regardless of whether any ambiguity currently exists.
- Applying either fix to a file where several same-service registrations have Pre/Post images produces compiling, unambiguous code (no `CS0104` ambiguous-reference errors, no collisions between `PreImage`/`PostImage` across registrations).
- On each run, the fix **re-evaluates** existing image-registration usings in the document: if a developer has "cleaned up" / hand-edited the imports in the meantime (e.g. removed an alias, reverted to a plain using, or left a stale bare reference), the fix rewrites those identified usings back to the consistent aliased form and re-qualifies their references.
- The existing test suite is updated to assert the always-aliased behavior and continues to pass.

## Non-goals

- Changing the wrapper-class source generation itself (`WrapperClassGenerator`, `PluginImageGenerator`) — namespaces, class shapes, and the `PreImage`/`PostImage` type names stay as-is. The generators emit each wrapper class into its own isolated namespace (`WrapperClassGenerator` writes `namespace {metadata.RegistrationNamespace}` at `WrapperClassGenerator.cs:32` and never emits `using` directives into the consumer's source files), so generated output is already unambiguous by construction and needs no change. The ambiguity this spec addresses is introduced only by the code-fix providers' edits to *user* code.
- Changing the diagnostics/analyzers (`HandlerSignatureMismatchAnalyzer`, `HandlerMethodNotFoundAnalyzer`) — the namespace is still passed via `Constants.PropertyImageNamespace`; only the code-fix output format changes.
- Touching non-image usings. Only usings whose namespace contains `.PluginRegistrations.` are rewritten; all other usings are left untouched.
- Rewriting `member access` expressions like `obj.PreImage`, the alias-name side of a `using X = ...` (`NameEqualsSyntax`), or already-qualified references — these exclusions in `ImageAmbiguityRewriter.VisitIdentifierName` (`SyntaxFactoryHelper.cs:284–300`) must be preserved.

## Requirements

- The aliased using format is exactly: `using {alias} = {fullNamespace};` where `alias = GetLastNamespaceSegment(fullNamespace)` (last dot-separated segment) and `fullNamespace` is the value from `Constants.PropertyImageNamespace`. Example: `using AccountUpdatePostOperation = TestNamespace.PluginRegistrations.TestPlugin.AccountUpdatePostOperation;`.
- The qualified parameter format is exactly: `{alias}.PreImage preImage` and/or `{alias}.PostImage postImage`, in that order (PreImage before PostImage), matching `CreateImageParameterList`'s ordering (`SyntaxFactoryHelper.cs:43–57`).
- Idempotency: applying the fix to a file that already has the correct aliased using must not duplicate it. The existing duplicate-guard in `ConvertToAliasedUsingsAndQualifyRefs` (`SyntaxFactoryHelper.cs:189–192`, checks `u.Alias?.Name.ToString() == newAlias || u.Name?.ToString() == newImageNamespace`) covers this; verify it still holds when the always-alias path is the only path.
- Multiple same-service registrations: when two registrations (e.g. `AccountUpdatePostOperation` and `AccountDeletePostOperation`) both declare images and both handlers are fixed in the same document, each gets its own aliased using and each parameter is prefixed with its own alias. No bare `PreImage`/`PostImage` remains in fixed signatures.
- Re-evaluation of stale imports: when the fix runs, every plain (non-aliased) using whose namespace `IsImageRegistrationNamespace` is true must be converted to its aliased form, and any bare `PreImage`/`PostImage` *type references* under that plain using must be re-qualified to the matching alias. This is the existing `ConvertToAliasedUsingsAndQualifyRefs` + `ImageAmbiguityRewriter` machinery — but it must now correctly handle **more than one** plain image using at a time (the current `break;`-after-first assumption at `SyntaxFactoryHelper.cs:306–310` is insufficient).
- Resolving which alias a bare `PreImage`/`PostImage` reference belongs to (the multi-using case): **use the semantic model to resolve each bare reference's symbol to its declaring namespace, then map that namespace back to its alias.** Do not guess from the bare name alone and do not fall back to "exactly one plain image using" — attempt to resolve every bare reference uniquely via its symbol. If the semantic model resolves the reference to a type whose containing namespace is one of the (now-aliased) image namespaces, qualify the reference with that namespace's alias (`GetLastNamespaceSegment`). If a reference cannot be resolved uniquely to a single image namespace (e.g. the symbol is missing/erroneous, or genuinely ambiguous), leave that reference unqualified rather than guessing — it will surface as a normal compile error for the developer to resolve. This requires threading a `SemanticModel`/`Compilation` (or per-tree `SemanticModel`) into `ConvertToAliasedUsingsAndQualifyRefs` / `ImageAmbiguityRewriter`, which today operate purely on syntax.
- The `DetectImageAmbiguity` method and the conditional `if (needsAlias) ... else AddUsingDirectiveIfMissing(...)` branches in both providers should no longer gate behavior — the aliased path becomes unconditional. `DetectImageAmbiguity` / `AddUsingDirectiveIfMissing` may become dead code; remove them if unused, or keep with updated callers — implementer's discretion, but no caller should rely on the plain-using branch for image namespaces.
- Both providers (`FixHandlerSignatureCodeFixProvider`, `CreateHandlerMethodCodeFixProvider`) must share the always-alias logic rather than each carrying a near-duplicate call site. Today each independently calls `DetectImageAmbiguity` + `CreateImageParameterList` + the using branch. Extract the common steps — compute alias from `imageNamespace`, build the alias-qualified parameter list, and run the always-aliased using conversion/requalification — into a single shared helper (on `SyntaxFactoryHelper`, or a new small helper type) that both providers call. No image-using/alias logic should be duplicated across the two providers.
- `BuildSignatureDescription` (`SyntaxFactoryHelper.cs:66`) is used only for code-action titles (e.g. `Fix signature to 'HandleUpdate(PreImage preImage)'`) and uses unqualified type names; titles may stay unqualified — they are human-facing labels, not emitted code.
- **FixAll must produce the same always-aliased, unambiguous result as a single fix.** Both `FixHandlerSignatureCodeFixProvider` and `CreateHandlerMethodCodeFixProvider` currently return `WellKnownFixAllProviders.BatchFixer` from `GetFixAllProvider()` (`FixHandlerSignatureCodeFixProvider.cs:27–28`, `CreateHandlerMethodCodeFixProvider.cs:24–25`). The batch fixer is unsafe here: it computes each diagnostic's `CodeAction` independently against the *original* document and then merges text changes, so when one document holds several image registrations (the exact same-service-multiple-images scenario this spec targets), the per-diagnostic fixes each add their own aliased using and rewrite the shared `using` block / overlapping references without seeing each other's edits — producing merge conflicts or non-convergent, still-ambiguous output. The goal is to be as unambiguous as possible, so FixAll must be handled explicitly rather than left to `BatchFixer`. See the dedicated phase below: a custom `FixAllProvider` consolidates *all* fixable diagnostics in a document into a single pass that fixes every target signature with its own alias and runs one consolidated always-aliased using rewrite (adding every needed aliased using and requalifying every reference once). This shared FixAll logic must not be duplicated between the two providers.

## Open questions

- None outstanding. (Resolved: bare-reference alias resolution uses the semantic model; the two providers share one always-alias helper; FixAll is handled by a custom consolidating provider; generators need no change.)

## Phases

### Phase 1: Build the shared always-aliased helper in `SyntaxFactoryHelper` (with semantic-model requalification)

This phase creates the single shared engine that both code-fix providers will call (resolving the "no duplication" decision) and makes the using-rewrite correct for multiple plain image usings via the semantic model (resolving the "which alias" decision). Edit `XrmPluginCore.SourceGenerator/Helpers/SyntaxFactoryHelper.cs`. Three parts:

1. **Expose the alias helper.** Add `public static string GetAliasForImageNamespace(string ns) => GetLastNamespaceSegment(ns);` (or make `GetLastNamespaceSegment` accessible). Keep `GetLastNamespaceSegment` semantics: substring after the last `.`, or the whole string if no dot (`SyntaxFactoryHelper.cs:212–216`).

2. **Add the shared always-alias entry point.** Add a single helper that both providers call, e.g.:
   ```csharp
   // Always emits the aliased using for imageNamespace and re-qualifies/aliases every
   // existing image-registration using in the tree. `semanticModel` is the model for `root`'s tree.
   public static SyntaxNode ApplyAliasedImageUsings(SyntaxNode root, string imageNamespace, SemanticModel semanticModel)
   ```
   It computes `alias = GetAliasForImageNamespace(imageNamespace)`, then delegates to `ConvertToAliasedUsingsAndQualifyRefs(root, imageNamespace, semanticModel)` (see part 3). Guard for null/empty `imageNamespace` (return `root` unchanged / fall back to no-op — the analyzer only sets `Constants.PropertyImageNamespace` when an expected namespace exists; `HandlerSignatureMismatchAnalyzer.cs:119–121`). The alias-qualified **parameter list** is still produced by the existing `CreateImageParameterList(bool, bool, string qualifier)` (`SyntaxFactoryHelper.cs:41–58`), which emits `QualifiedName(IdentifierName(qualifier), IdentifierName(typeName))` (`SyntaxFactoryHelper.cs:218–228`) — both providers pass `alias` as the qualifier (never `null`/conditional).

3. **Thread the semantic model into `ConvertToAliasedUsingsAndQualifyRefs` + `ImageAmbiguityRewriter` and fix the multi-using requalification.** Today both operate purely on syntax. Change the signature of `ConvertToAliasedUsingsAndQualifyRefs` (`SyntaxFactoryHelper.cs:152`) to also accept a `SemanticModel`, and pass it into the `ImageAmbiguityRewriter` constructor (`SyntaxFactoryHelper.cs:242`). The rewriter currently (a) collects all plain image namespaces into `plainNamespaceToAlias` (lines 159–173 of `ConvertToAliasedUsingsAndQualifyRefs`), (b) adds the new namespace (lines 175–179), (c) runs the rewriter, (d) appends the new aliased using with a duplicate guard checking `u.Alias?.Name.ToString() == newAlias || u.Name?.ToString() == newImageNamespace` (lines 186–202). That collection + dedup logic is correct and stays.

   The broken part is `ImageAmbiguityRewriter.VisitIdentifierName` (`SyntaxFactoryHelper.cs:274–325`), which today picks the alias by iterating `_typeToExistingAlias` and `break`-ing on the first entry with the comment "There should be exactly one existing plain image using at this point" (lines 305–310). Replace this guess with **semantic resolution**: for each bare `PreImage`/`PostImage` identifier (after the existing exclusions at lines 284–300 — already-qualified right side of a `QualifiedNameSyntax`, member-access name like `obj.PreImage`, and the `NameEqualsSyntax` alias side — which MUST be preserved), use the threaded `SemanticModel` to resolve the identifier's symbol (`GetSymbolInfo`/`GetTypeInfo`) to its containing namespace's full name. If that namespace is one of the image namespaces being aliased (present in `plainNamespaceToAlias`, i.e. `IsImageRegistrationNamespace` true and known), qualify the reference with **that** namespace's alias (`GetLastNamespaceSegment`). If the symbol cannot be resolved uniquely to a single image namespace (missing/erroneous symbol, or ambiguous `CS0104`-style binding), leave the reference unqualified — do not guess — so it surfaces as a normal compile error. This makes requalification correct regardless of how many plain image usings exist in the file. Remove the misleading `break;` and its comment.

   Note: because the rewriter converts the plain usings to aliased form in the same pass, resolve symbols against the **original** (pre-rewrite) tree's semantic model, which is what the providers supply (the model for the tree as it was when the diagnostic fired). The bare references were valid under the original plain usings, so the symbols resolve there.

Verifiable when: `SyntaxFactoryHelper` exposes `GetAliasForImageNamespace` and `ApplyAliasedImageUsings(...)`; `ConvertToAliasedUsingsAndQualifyRefs` accepts a `SemanticModel`; and a unit-level invocation over a tree with two plain image usings (`AccountUpdatePostOperation`, `AccountDeletePostOperation`) plus bare `PreImage` references under each correctly qualifies each reference to its own alias.

### Phase 2: Wire `FixHandlerSignatureCodeFixProvider` to the shared helper (always-aliased, no duplication)

Edit `XrmPluginCore.SourceGenerator/CodeFixes/FixHandlerSignatureCodeFixProvider.cs`, method `FixMethodDeclarationsAsync` (lines 124–241), specifically the block at lines 198–235.

Today it does:
```csharp
var ambiguity = SyntaxFactoryHelper.DetectImageAmbiguity(methodRoot, imageNamespace);
var newParameters = SyntaxFactoryHelper.CreateImageParameterList(hasPreImage, hasPostImage, ambiguity.needsAlias ? ambiguity.alias : null);
// ... replace method decls ...
if (ambiguity.needsAlias)
    newRoot = SyntaxFactoryHelper.ConvertToAliasedUsingsAndQualifyRefs(newRoot, imageNamespace);
else
    newRoot = SyntaxFactoryHelper.AddUsingDirectiveIfMissing(newRoot, imageNamespace);
```

Change it to **always** use the alias via the shared helper. Compute the alias once and always pass it to `CreateImageParameterList`; then call `ApplyAliasedImageUsings` for the using rewrite:
```csharp
var alias = SyntaxFactoryHelper.GetAliasForImageNamespace(imageNamespace);
var newParameters = SyntaxFactoryHelper.CreateImageParameterList(hasPreImage, hasPostImage, alias);
// ... replace method decls (existing loop at lines 216–225 unchanged) ...
newRoot = SyntaxFactoryHelper.ApplyAliasedImageUsings(newRoot, imageNamespace, treeSemanticModel);
```
`ApplyAliasedImageUsings` needs the `SemanticModel` for the tree being rewritten. This provider processes potentially multiple documents grouped by syntax tree (`locationsByTree`, lines 144–162); for each `tree`/`methodDocument`, obtain the model via `compilation.GetSemanticModel(tree)` (the `compilation` is already available as `semanticModel.Compilation`, passed into `FixMethodDeclarationsAsync` at line 119/126). Guard `imageNamespace` null/empty (no-op fallback). Remove the `DetectImageAmbiguity`/conditional branch.

Verifiable when: applying the fix to a single PreImage registration produces both `void HandleUpdate(AccountUpdatePostOperation.PreImage preImage)` in the signature AND `using AccountUpdatePostOperation = TestNamespace.PluginRegistrations.TestPlugin.AccountUpdatePostOperation;` in the usings — i.e. the previously-named test `Should_Fix_Signature_And_Add_Using_For_PreImage` now sees the aliased form. And the multi-registration case (two same-service registrations both with PreImages) yields one aliased using per namespace, each signature alias-qualified, no `CS0104`.

### Phase 3: Wire `CreateHandlerMethodCodeFixProvider` to the shared helper (always-aliased, no duplication)

Edit `XrmPluginCore.SourceGenerator/CodeFixes/CreateHandlerMethodCodeFixProvider.cs`, method `CreateMethodAsync` (lines 135–154) and `CreateMethodDeclaration` (lines 157–166).

Today it does:
```csharp
var (needsAlias, alias) = SyntaxFactoryHelper.DetectImageAmbiguity(interfaceRoot, imageNamespace);
var methodDeclaration = CreateMethodDeclaration(methodName, hasPreImage, hasPostImage, needsAlias ? alias : null);
// ... add member ...
if (needsAlias)
    newRoot = SyntaxFactoryHelper.ConvertToAliasedUsingsAndQualifyRefs(newRoot, imageNamespace);
else
    newRoot = SyntaxFactoryHelper.AddUsingDirectiveIfMissing(newRoot, imageNamespace);
```

Change to always-alias via the same shared helper:
```csharp
var alias = SyntaxFactoryHelper.GetAliasForImageNamespace(imageNamespace);
var methodDeclaration = CreateMethodDeclaration(methodName, hasPreImage, hasPostImage, alias);
// ... add member (existing AddMembers/ReplaceNode at lines 141–142 unchanged) ...
newRoot = SyntaxFactoryHelper.ApplyAliasedImageUsings(newRoot, imageNamespace, interfaceSemanticModel);
```
The interface may live in a different document/tree than the trigger; obtain its semantic model via `compilation.GetSemanticModel(interfaceRoot.SyntaxTree)` (the `compilation` is `semanticModel.Compilation`, available in `CreateMethodAsync`, line 76). Apply the same null/empty `imageNamespace` guard. `CreateMethodDeclaration` already forwards the `qualifier` into `CreateImageParameterList` (line 162), so the only change there is passing `alias` instead of the `needsAlias ? alias : null` conditional. Remove the `DetectImageAmbiguity`/conditional branch. After this phase, no image-using/alias logic is duplicated between the two providers — both go through `SyntaxFactoryHelper.ApplyAliasedImageUsings` + `CreateImageParameterList`.

Verifiable when: invoking the "Create method" fix for a missing handler with a PreImage registration emits `void HandleUpdate(AccountUpdatePostOperation.PreImage preImage);` on the interface plus the aliased using directive, even when no other image using exists in the file; and both providers reference the single shared helper (grep shows no remaining `DetectImageAmbiguity` call sites unless intentionally retained).

### Phase 4: Replace `BatchFixer` with a custom consolidating `FixAllProvider` for both providers

Both `FixHandlerSignatureCodeFixProvider` and `CreateHandlerMethodCodeFixProvider` currently return `WellKnownFixAllProviders.BatchFixer` (`FixHandlerSignatureCodeFixProvider.cs:27–28`, `CreateHandlerMethodCodeFixProvider.cs:24–25`). As described in Requirements, `BatchFixer` applies each diagnostic's fix independently against the original document and merges — which breaks when one document has several image registrations, because each fix touches the shared `using` block / overlapping references without seeing the others, yielding merge conflicts or non-convergent ambiguous output.

Implement a single shared custom `FixAllProvider` (a new type, e.g. `XrmPluginCore.SourceGenerator/CodeFixes/AliasedImageUsingsFixAllProvider.cs`, used by both providers via `GetFixAllProvider()`) that:
1. Collects **all** fixable diagnostics in the relevant scope (Document/Project/Solution — at minimum Document scope; group remaining work by document). For each document, gather every diagnostic and read each one's `Constants.PropertyServiceType`, `Constants.PropertyMethodName`, `Constants.PropertyHasPreImage`, `Constants.PropertyHasPostImage`, `Constants.PropertyImageNamespace` (same property keys the per-fix path reads, e.g. `FixHandlerSignatureCodeFixProvider.cs:41–49`).
2. Applies all signature/method edits for that document in **one** pass: for each diagnostic, fix its target method signature (or create its interface method) with its **own** alias-qualified parameter list (`SyntaxFactoryHelper.CreateImageParameterList(hasPreImage, hasPostImage, GetAliasForImageNamespace(imageNamespace))`).
3. Runs **one** consolidated always-aliased using rewrite over the resulting tree that adds every distinct aliased image using needed and requalifies every reference once. Prefer extending the shared helper from Phase 1 to accept multiple image namespaces in a single call (e.g. an overload `ApplyAliasedImageUsings(SyntaxNode root, IEnumerable<string> imageNamespaces, SemanticModel semanticModel)` that loops the existing collection/dedup/requalify logic of `ConvertToAliasedUsingsAndQualifyRefs`), so the FixAll path reuses the exact same engine as the single-fix path. No FixAll-specific alias/using logic may be duplicated — it must funnel through the same `SyntaxFactoryHelper` engine that Phases 1–3 use.

The result must be order-independent and convergent: fixing N registrations in a file via FixAll produces exactly the same unambiguous output as fixing them one-by-one in any order — each namespace aliased exactly once, each signature alias-qualified, no `CS0104`.

Verifiable when: a FixAll over a document with two (or more) same-service image registrations produces, in a single operation, one aliased `using ... = ...;` per distinct image namespace (no duplicates) and every fixed signature alias-qualified, and the document compiles cleanly; and `GetFixAllProvider()` on both providers returns the new shared provider rather than `WellKnownFixAllProviders.BatchFixer`.

### Phase 5: Update and extend the code-fix tests

Edit `XrmPluginCore.SourceGenerator.Tests/DiagnosticTests/FixHandlerSignatureCodeFixProviderTests.cs` and `XrmPluginCore.SourceGenerator.Tests/DiagnosticTests/CreateHandlerMethodCodeFixProviderTests.cs`.

In `FixHandlerSignatureCodeFixProviderTests.cs`:
- `Should_Fix_Signature_And_Add_Using_For_PreImage` (assertions at lines 61, 64): change expected signature from `void HandleUpdate(PreImage preImage)` to `void HandleUpdate(AccountUpdatePostOperation.PreImage preImage)` and the using from plain `using TestNamespace.PluginRegistrations.TestPlugin.AccountUpdatePostOperation;` to aliased `using AccountUpdatePostOperation = TestNamespace.PluginRegistrations.TestPlugin.AccountUpdatePostOperation;`.
- `Should_Fix_Signature_And_Add_Using_For_PostImage` (lines 114, 117): expect `void HandleUpdate(AccountUpdatePostOperation.PostImage postImage)` and the aliased using.
- `Should_Fix_Signature_And_Add_Using_For_Both_Images` (line ~120): expect `void HandleUpdate(AccountUpdatePostOperation.PreImage preImage, AccountUpdatePostOperation.PostImage postImage)` and the aliased using.
- `Should_Avoid_Ambiguous_Usings` (line 231): this already expects the aliased form (`void HandleUpdate(AccountUpdatePostOperation.PreImage preImage)`, `void HandleDelete(AccountDeletePostOperation.PreImage preImage)`, and the two aliased `using ... = ...;` directives counted once each via `CountOccurrences`, lines 285–292). It should keep passing unchanged — verify it does. Consider renaming it (e.g. `Should_Use_Aliased_Usings_For_Multiple_Registrations`) since aliasing is no longer "avoidance of ambiguity" but the default.
- Add a new test asserting idempotency: applying the fix to source that already contains the correct aliased using and aliased signature does not duplicate the using (use the existing `CountOccurrences` helper at lines 295–304, expect count `1`).
- Add a new test for the "cleaned-up imports" re-evaluation: source where a developer reverted one registration's using to plain form (or removed it) — after fix, all image usings are aliased exactly once.
- Add a new test for **multi-using semantic requalification**: two same-service registrations (`AccountUpdatePostOperation` + `AccountDeletePostOperation`) where pre-existing handler bodies/signatures contain bare `PreImage` references under two plain image usings. After fix, each bare reference is qualified to its own alias (`AccountUpdatePostOperation.PreImage` vs `AccountDeletePostOperation.PreImage`) — never cross-qualified — and the result compiles with no `CS0104`. This exercises the Phase 1 semantic-model resolution path (where the old `break`-on-first logic would have mis-qualified one of them).

- Add a new **FixAll** test (Phase 4): a document with two or more same-service image registrations, all triggering the diagnostic, fixed via the FixAll/batch path. Assert the consolidated single-pass result — one aliased `using ... = ...;` per distinct image namespace (count `1` each via `CountOccurrences`), every signature alias-qualified, no `CS0104`. If `CodeFixTestBase` only exercises single-diagnostic fixes today, extend it (or add a sibling helper) to invoke the provider's `GetFixAllProvider()` across all diagnostics in the document so the custom `FixAllProvider` is actually exercised (not just the per-diagnostic `RegisterCodeFixesAsync`).

In `CreateHandlerMethodCodeFixProviderTests.cs`: apply the analogous expectation changes — generated interface methods must now use alias-qualified parameter types and the file must gain an aliased using — plus an analogous FixAll test for multiple missing handlers on the same service interface.

Use the shared `CodeFixTestBase` / `ApplyCodeFixAsync` harness already present (`FixHandlerSignatureCodeFixProviderTests.cs:307–309` wires `HandlerSignatureMismatchAnalyzer` + `FixHandlerSignatureCodeFixProvider` with diagnostic ids `HandlerSignatureMismatch.Id` and `HandlerSignatureMismatchError.Id`).

Verifiable when: `dotnet test --configuration Release` passes for `XrmPluginCore.SourceGenerator.Tests`, including the updated and newly-added tests, across all target frameworks.
