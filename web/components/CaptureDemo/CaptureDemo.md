# CaptureDemo

Decides when a page should run its hands-free demo: when the URL carries `?demo=1`, or when the
MindAttic.Capture bridge (`window.mindattic.capture`) reports the session is being filmed
(`tapeRunning`, or an on-demand `recording`). The bridge starts asynchronously, so it is polled
every 500 ms for up to 20 s.

```js
MindAtticCaptureDemo.armed(function () { /* the page's own tour */ });
MindAtticCaptureDemo.forced();   // true when ?demo=1 is present
```

The tour is the page's: mindattic.com fires an occasional random Cyberspace effect,
mindatticcares.com scrolls through each page in turn.
