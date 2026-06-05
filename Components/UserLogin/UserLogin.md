# UserLogin

A thin **styling wrapper** around the MindAttic.Authentication `MaLogin` static-SSR form. It does
**not** implement authentication — sign-in is owned by the library's `/_ma-auth/login` endpoint.

## Layout
- `index.htm` — standalone style preview (facsimile markup; does not authenticate)
- `user-login.css` — scopes every rule under `.ul-card`; styles only the library's existing
  `.ma-auth-login` / `.ma-auth-field` / `.ma-auth-submit` / `.ma-auth-error` classes
- `user-login.js` — tiny idempotent focus-first enhancer (no auth behavior)
- `UserLogin.razor` — Blazor wrapper that renders `<MaLogin>` inside `.ul-card`
- `UserLogin.md` — this file

## Usage — raw CSS subscriber
Link `user-login.css` and ensure the rendered form carries the `.ma-auth-login` classes (the
library's `MaLogin` already emits them). Wrap it in an element with class `ul-card`.

## Usage — Blazor
On a **static-SSR** `/login` page (requires a `PackageReference` to `MindAttic.Authentication` 1.0.0):
```razor
<UserLogin ReturnUrl="@returnUrl" Error="@hasError" />
```
**Static-SSR constraint:** the page must render static (no interactive render mode), or the
`AntiforgeryToken` inside `MaLogin` won't validate the POST. In a globally-interactive app, mark the
`/login` page `[ExcludeFromInteractiveRouting]`.

## Theming
CSS custom properties: `--ul-accent` (default `#e2231a`), `--ul-bg` (default `#14171c`).

## jsDelivr (pinned tag)
```
https://cdn.jsdelivr.net/gh/mindattic/MindAttic.UiUx@V5/Components/UserLogin/user-login.css
```

---
*Edit in this folder only. Downstream copies (synced wwwroot / source-copied .razor) are derived artifacts.*
