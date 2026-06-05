# UserCircle

Upper-right avatar/initials circle that appears **only when authenticated**. Clicking it either opens
a small menu (with a Sign out item) or signs the user out directly, depending on `ClickAction`.

## Parameters
| Param | Type | Default | Notes |
|---|---|---|---|
| `Size` | int (px) | `40` | Sets `--uc-size`. |
| `Icon` | string? | initials | Optional raw SVG markup rendered as `.uc-icon` instead of initials. |
| `ClickAction` | string | `"menu"` | `"menu"` toggles the dropdown; `"logout"` makes clicking the circle sign out directly. |

## State binding
Reads the `ClaimsPrincipal` via `<AuthorizeView>` — display name from `ClaimTypes.Name`, role from
`IsInRole("Admin")` (`MaRoles.Admin`). It never calls authentication services.

## Logout
A native `<form method="post" action="/_ma-auth/logout">` with `<AntiforgeryToken/>`. `user-circle.js`
submits it via `requestSubmit()` — **never** `fetch` — so antiforgery + the `__Host-` cookie are preserved.

## Requirements
- `user-circle.js` loaded once per app (delegated handler; idempotent).
- Static-SSR rendering (no interactive render mode) so the `AntiforgeryToken` is valid for the POST.

## jsDelivr (pinned tag)
```
https://cdn.jsdelivr.net/gh/mindattic/MindAttic.UiUx@V5/Components/UserCircle/user-circle.css
https://cdn.jsdelivr.net/gh/mindattic/MindAttic.UiUx@V5/Components/UserCircle/user-circle.js
```

---
*Edit in this folder only. Downstream copies are derived artifacts.*
