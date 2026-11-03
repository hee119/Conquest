# Project working instructions

## Unity button events

- Connect button actions directly through the Button component's Inspector **On Click()** persistent events, saved in the scene or prefab. This is the user's explicit preference.
- Do not implement button activation by polling mouse position, checking pointer bounds, or checking mouse clicks in Update/LateUpdate.
- For authored buttons, do not replace visible Inspector wiring with runtime `onClick.AddListener` registrations. Expose a suitable public handler and wire it in the Inspector through Unity Editor APIs.
- When changing a button, verify its On Click() target and method, and avoid duplicate invocation from old polling code or runtime listeners.
- Pointer enter/exit handling for visual hover effects is separate from button activation and may remain when needed.

## Running Unity sessions

- Do not stop or restart an active game to perform edits unless the user explicitly authorizes it. Preserve the running session and use a separate editing workflow where possible; explain any limitation before taking an action that would interrupt play.
