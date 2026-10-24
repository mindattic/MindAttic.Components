using System.IO.Packaging;
using System.Runtime.CompilerServices;

namespace MindAttic.Components.Wpf;

/// <summary>
/// Registers the <c>pack://</c> URI scheme the moment this assembly loads, instead of relying on a WPF
/// <see cref="System.Windows.Application"/> to have done it first. <see cref="System.Windows.Application"/>'s own
/// static constructor registers the scheme as a side effect; without one running yet (tests, tools, or any code
/// that builds a <c>pack://</c> <see cref="Uri"/> before the app does), the first attempt throws
/// <see cref="UriFormatException"/>. Touching <see cref="PackUriHelper.UriSchemePack"/> registers it the same way
/// <see cref="System.Windows.Application"/> does, so doing that here, once, covers every type in this assembly:
/// a method marked <see cref="ModuleInitializerAttribute"/> is guaranteed to run before any other code in the
/// module, including every type's own static constructor and field initializers, so no type needs its own copy
/// of this touch (M.COMP-T114;
/// previously duplicated across call sites here and in MindAttic.Orchestrator's Palette, M.COMP-T54, M.ORCH-T703).
/// <para>
/// A consumer in another assembly (e.g. a test project building a <c>pack://</c> <see cref="Uri"/> directly, not
/// through any API here) still needs to load this assembly first for its module initializer to have run;
/// referencing any public type here (even just <c>typeof</c>) is enough.
/// </para>
/// </summary>
internal static class PackSchemeRegistration
{
    // CA2255 flags ModuleInitializer outside application/source-generator code because most libraries shouldn't
    // run work just from being referenced; this one deliberately does, to fix the exact problem that attribute
    // exists for (a static field initializer elsewhere in the module needing a scheme registered first).
#pragma warning disable CA2255
    [ModuleInitializer]
#pragma warning restore CA2255
    internal static void Register() => _ = PackUriHelper.UriSchemePack;
}
