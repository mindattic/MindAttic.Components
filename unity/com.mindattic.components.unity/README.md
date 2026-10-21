# MindAttic Components (Unity)

Themed uGUI + TextMesh Pro menus and controls for MindAttic's Unity 6 games, sharing the palette and token names of the MindAttic web themes: `MenuStack`/`MenuScreen`, `MaModal` (alert, confirm, busy), `MaToast`, `MaTooltip`, `MaButton`, `MaIconButton`, `MaToggle`, `MaTextbox`, `MaSelect`, `MaRange`, `MaProgressBar`, `MaSpinner`, `MaToolbar`, `MaFormRow`, `MaCodeDisplay`, `MaList`, `MaTag`, themed by `ThemeTokens` through `MaTheme`.

```csharp
var menu = MenuScreen.Create(canvas, "Multiplayer");
menu.AddButton("Host", OnHost, ButtonVariant.Primary);
var code = menu.AddTextbox("Join code", "ABC123").AsCode(6);
menu.AddButton("Join", () => Join(code.Text));
```

Full guide: [docs/unity/COMPONENTS.md](../../docs/unity/COMPONENTS.md) in the MindAttic.Components repo.
