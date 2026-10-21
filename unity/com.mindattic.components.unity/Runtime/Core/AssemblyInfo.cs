// MindAttic.Components.Unity.Core: engine-free (noEngineReferences).
// The theme token model and the parser that reads web/themes CSS, the control colour rules, the
// navigation graph, the menu stack model, menu validation, the toast queue and motion timing live
// here, so `dotnet test unity/tests/Core.Tests` covers them without an editor.
// Nothing in this assembly may reference UnityEngine.
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("MindAttic.Components.Unity")]
[assembly: InternalsVisibleTo("MindAttic.Components.Unity.Core.Tests")]
[assembly: InternalsVisibleTo("MindAttic.Components.Unity.Tests")]
