# Sakura Run (Unity)

3D anime-style endless runner. Everything (character, village, UI, music, SFX) is generated procedurally in code, so no art or audio assets are required.

## Run it
1. Create/open a Unity 2021.3+ project (Built-in or URP, 3D template).
2. Copy `Assets/Scripts` into the project's `Assets` folder.
3. Open any scene (an empty one is fine) and press Play. `GameBootstrap` builds the whole game at runtime.

## Controls
A/D or Left/Right: change lane. W/Up/Space: jump. S/Down: slide. Esc/P: pause. Touch: swipe.

## Script map
- GameBootstrap: builds camera, lighting, sky, fog and all systems at startup
- GameManager: state, score, speed ramp, best score
- PlayerController / CameraRig: runner, procedural animation, power-up state, follow camera
- WorldGenerator: endless segments, obstacle/flower/power-up patterns (always a safe lane), collisions, magnet
- Entities / Scenery / Effects / Utility: props, village scenery, particles, materials and input helpers
- AudioManager: procedural pentatonic music loop and synthesized SFX
- UIManager: menu, HUD, pause and game-over screens
