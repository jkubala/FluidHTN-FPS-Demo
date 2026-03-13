# FluidHTN FPS Demo

A Unity FPS demo built for the [Fluid HTN](https://github.com/ptrefall/fluid-hierarchical-task-network) library, demonstrating how HTN-driven enemy AI behaves in a practical first-person shooter context. The demo features a feature-rich **player controller**, a **baked cover point system**, and an **AI sensor** to give the HTN planner a realistic environment to operate in.

---

## Features

### Player Controller

The player controller is the centrepiece of the demo, covering a broad set of movement and interaction mechanics:

- **Movement** — standard first-person movement via Unity's `CharacterController`
- **Mouse look / camera** — smoothed look with configurable sensitivity
- **Crouching** — toggleable crouch that adjusts the character capsule height
- **Leaning** — left/right lean for peeking around corners without fully exposing the player
- **Ladder climbing** — detects and traverses ladder volumes
- **Environment climbing** — physics raycast scanning determines climbable surfaces, allowing the player to vault or climb geometry dynamically based on what the raycasts report rather than hand-tagged objects
- **Shooting / weapons** — weapon firing with configurable parameters
- **Health / damage** — player health management and damage reception

### Cover Point Generator

A custom editor tool that **bakes cover positions at edit time**. Cover points are stamped into the scene as precomputed data, so enemy agents can query valid positions cheaply at runtime without any per-frame spatial analysis. The baked points are what enemy AI uses when deciding where to seek cover relative to the player.

### AI Sensor

Enemies use a dedicated sensor system to perceive the player. The sensor tracks whether the player is visible, at what range, and crucially **from which direction** they are being observed. This feeds directly into the world-state blackboard so the HTN planner can make informed decisions — e.g. only taking cover when the player has line of sight, or flanking from a blind side.

A built-in **direction updater GUI** visualises the sensor state in the editor and at runtime, displaying a directional indicator of where the player is currently being seen from. This makes it easy to debug and tune perception behaviour without needing to read raw blackboard values.

### Enemy AI

Enemies are driven by an **HTN (Hierarchical Task Network) planner** via the [Fluid HTN](https://github.com/ptrefall/fluid-hierarchical-task-network) library. The planner queries the baked cover points and the player controller's state (health, visibility, position) through a shared world-state blackboard to produce coherent multi-step plans — patrolling, engaging, seeking cover, and so on.

> ⚠️ **Work in progress** — the AI agent is in its early stages and only the basics are implemented so far. The HTN planner is not yet meaningfully showcased through agent behaviour. That said, the demo already has a lot of substance in the player controller, cover system, and sensor — these are the main things worth exploring right now.

---

## Project Structure

```
Assets/
├── Scripts/
│   ├── Player/
│   │   └── PlayerController.cs     # Movement, look, crouch, lean, climb, shoot, health
│   ├── Cover/
│   │   └── CoverPointGenerator.cs  # Editor tool — bakes cover points into the scene
│   ├── AI/                         # HTN context, domain, operators, conditions, effects
│   └── ...
├── Scenes/
├── Prefabs/
└── Shaders/
Packages/
└── manifest.json                   # Includes Fluid HTN as a Git package dependency
```

---

## Getting Started

### Prerequisites

- **Unity 2022.x or later** (check `ProjectSettings/ProjectVersion.txt` for the exact version)
- Fluid HTN is pulled automatically via `Packages/manifest.json` — no manual install needed

### Setup

1. Clone the repository:
   ```bash
   git clone https://github.com/jkubala/FluidHTN-FPS-Demo.git
   ```
2. Open the project folder in **Unity Hub** and let package resolution complete.
3. Open the demo scene from `Assets/Scenes/`.
4. Press **Play**.

### Baking Cover Points

Cover points must be baked before enemy AI can use them. With the scene open in the editor, select the `CoverPointGenerator` object and run the bake from its Inspector. Re-bake whenever the level geometry changes.

> ⚠️ **Work in progress** — the cover point generator is still in development. Proper horizontal spacing is not yet supported, so baked results may not be reliable in all environments.

---

## Dependencies

| Dependency | Source |
|---|---|
| [Fluid HTN](https://github.com/ptrefall/fluid-hierarchical-task-network) | Git URL in `Packages/manifest.json` |
| Unity NavMesh | Built-in Unity package |
| Unity Input System | Built-in Unity package |

---

## License

MIT — see [LICENSE](LICENSE) for details.
