# UserTimeout

A 30-minute **idle-timeout warning + auto-logout**. After inactivity it shows a countdown modal;
on expiry it signs the user out. Renders (and arms) **only when authenticated**.

## Parameters
| Param | Type | Default | Notes |
|---|---|---|---|
| `IdleMs` | int (ms) | `1800000` (30m) | Keep equal to `AuthSessionOptions.IdleTimeout`. |
| `WarnMs` | int (ms) | `60000` (60s) | How long before expiry the warning modal appears. |

## Behavior
Activity (`mousemove`/`keydown`/`click`/`scroll`/`touchstart`, throttled) resets the idle clock and
hides the modal. At `IdleMs − WarnMs` the modal shows a live countdown; at `IdleMs` it submits the
logout form. "Stay signed in" resets; "Log out now" submits immediately. A backgrounded tab keeps
counting. Logout is a native `<form method="post" action="/_ma-auth/logout">` with `<AntiforgeryToken/>`,
submitted via `requestSubmit()` — never `fetch`.

## Authenticated-only guarantee
The host `#ut-root` element is rendered only inside `<AuthorizeView><Authorized>`, and `user-timeout.js`
no-ops unless `#ut-root` exists — so the timer never runs while signed out.

## Usage
Place **once** in the app shell/`MainLayout` (inside auth context). Requires `user-timeout.js` +
`user-timeout.css` loaded once. Static-SSR safe (the form + token render server-side).

## jsDelivr (pinned tag)
```
https://cdn.jsdelivr.net/gh/mindattic/MindAttic.UiUx@V5/Components/UserTimeout/user-timeout.css
https://cdn.jsdelivr.net/gh/mindattic/MindAttic.UiUx@V5/Components/UserTimeout/user-timeout.js
```

---
*Edit in this folder only. Downstream copies are derived artifacts.*
