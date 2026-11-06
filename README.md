# Sakura Run — 3D Anime-Style Endless Runner in Unity

![Engine](https://img.shields.io/badge/Engine-Unity%206-black?logo=unity)
![Language](https://img.shields.io/badge/Language-C%23-239120?logo=csharp&logoColor=white)
![Pipeline](https://img.shields.io/badge/Render-URP-ff8fbf)
![Assets](https://img.shields.io/badge/Imported%20Assets-None-cdb4f0)

A cherry blossom endless runner where a cheerful anime girl dashes through a magical Japanese village under a pink-lavender sunset. The whole game, including the character, village, UI, music and sound effects, is **generated in C# code at runtime**, so it runs from a single folder of scripts with no imported art or audio.

![Screenshot](docs/screenshot-gameplay.png)

---

## Features

- **Three-lane running** with smooth lane changes, jumping and sliding
- **Endless world** of recycled road segments with sakura trees, stone lanterns, torii gates, pagodas, temples and Mount Fuji
- **Fair obstacle patterns**: low (jump), high (slide) and full (change lane), with a safe lane always left open
- **Glowing sakura flowers** to collect, laid in lines and arcs over obstacles
- **Three power-ups**: flower magnet, protective shield and speed boost
- **Third-person follow camera** with menu orbit, speed-based field of view and shake
- **Particles**: falling cherry blossom petals, floating motes, pickup and smash bursts
- **Procedural audio**: a relaxing pentatonic music loop and eleven synthesized sound effects
- **Polished UI**: main menu, HUD with score and flower counter, pause menu and game-over screen
- **Pastel pink, lavender and peach** look across the world and the interface
- **Best score** saved between sessions

## Controls

| Action | Keyboard | Touch |
|---|---|---|
| Move left / right | `A` `D` or arrow keys | Swipe left / right |
| Jump | `W`, `Up` or `Space` | Swipe up |
| Slide | `S` or `Down` | Swipe down |
| Pause | `Esc` or `P` | Pause button |

## Power-ups

| Power-up | Effect | Duration |
|---|---|---|
| Flower magnet | Pulls nearby flowers toward you | 8 s |
| Shield | Absorbs one collision | Until hit |
| Speed boost | Faster and smashes through obstacles | 6 s |

## Quick Start

1. Install **Unity 6** (developed with `6000.5.9f1`) through Unity Hub.
2. Create a new project from the **Universal 3D** template.
3. Copy the `Assets/Scripts` folder from this repository into your project's `Assets` folder.
4. Open `SampleScene` and press **Play**. No scene setup is needed.

To use your own music, create `Assets/Resources` and add a file named `BGM.mp3` (or `.wav` / `.ogg`).

## Project Structure

```
Assets/Scripts/
├── GameBootstrap.cs     Builds camera, lighting, sky, world, player, audio and UI at startup
├── GameManager.cs       Game state, score, speed ramp, best score
├── PlayerController.cs  Runner input, physics, procedural animation, power-up state
├── CameraRig.cs         Follow camera, menu orbit, shake
├── WorldGenerator.cs    Endless segments, obstacle patterns, pooling, collisions
├── Entities.cs          Obstacle and pickup classes and factories
├── Scenery.cs           Trees, lanterns, torii, pagoda, temple, hills, Mount Fuji
├── Effects.cs           Particle textures and particle systems
├── AudioManager.cs      Procedural music and sound effects
├── UIManager.cs         Menu, HUD, pause and game-over screens
└── Utility.cs           Palette, materials, mesh helpers, input wrapper
```

## How It Works

- **The world moves, not the player.** The runner stays at the origin while road segments scroll backward and are recycled, which avoids floating-point drift at long distances.
- **Pooling.** Obstacles, pickups and scenery are reused instead of destroyed, so there are no allocation spikes while running.
- **Safe lane rule.** Each row of obstacles keeps one lane free of full obstacles, and that lane moves by at most one lane per row.
- **Difficulty ramp.** Speed rises from 13 toward 30 units per second and obstacle density grows with distance.
- **Procedural everything.** Textures, sprites, meshes, the sky gradient, the music and every sound effect are created in code.
- **Zero-setup startup.** `GameBootstrap` uses `RuntimeInitializeOnLoadMethod`, so the game builds itself in any scene.

## Tech Stack

- Unity 6, Universal Render Pipeline
- C#
- Unity Particle System, uGUI, AudioClip synthesis

## Screenshots

| Main menu | Gameplay | Game over |
|---|---|---|
| ![Menu](docs/screenshot-menu.png) | ![Gameplay](docs/screenshot-gameplay.png) | ![Game over](docs/screenshot-gameover.png) |

## Roadmap

- Replace primitive models with 3D assets and animation clips
- More environments (autumn temple, night festival)
- Online leaderboard
- Unlockable characters and missions
- Mobile builds
