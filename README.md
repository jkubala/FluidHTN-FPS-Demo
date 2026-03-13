# FluidHTN FPS Demo

A Unity FPS demo built for the [Fluid HTN](https://github.com/ptrefall/fluid-hierarchical-task-network) library, demonstrating how HTN-driven enemy AI behaves in a practical first-person shooter context. The demo features a feature-rich **player controller**, a **baked cover point system**, and an **AI sensor** to give the HTN planner a realistic environment to operate in.

---

## Features

### Player Controller

The player controller is the centrepiece of the demo, covering a broad set of movement and interaction mechanics:

- **Movement** — first-person movement with a floating capsule, spring-damped ground detection, and slope handling
- **Mouse look / camera** — smoothed look with configurable sensitivity
- **Crouching** — toggleable crouch that adjusts the character capsule height
- **Leaning** — left/right lean for peeking around corners without fully exposing the player
- **Ladder climbing** — detects and traverses ladder volumes
- **Environment climbing** — physics raycast scanning determines climbable surfaces, allowing the player to vault or climb geometry dynamically based on what the raycasts report rather than hand-tagged objects
- **Shooting / weapons** — weapon firing with configurable parameters
- **Health / damage** — player health management and damage reception

### Tactical Cover Point Generator

A custom editor tool that **bakes tactical cover positions at edit time**. The generator detects low cover positions, low corners, and high corners by scanning geometry with raycasts, then validates each candidate for ground level, cover continuity, and firing clearance. Cover points are stamped into scene-specific data assets so enemy agents can query valid positions cheaply at runtime without any per-frame spatial analysis.

### AI Sensor

Enemies use a dedicated sensor system to perceive the player. The sensor tracks whether the player is visible, at what range, and crucially **from which direction** they are being observed. This feeds directly into the world-state blackboard so the HTN planner can make informed decisions — e.g., only taking cover when the player has line of sight, or flanking from a blind side.

A built-in **direction updater GUI** visualises the sensor state in the editor and at runtime, displaying a directional indicator of where the player is currently being seen from. This makes it easy to debug and tune perception behaviour without needing to read raw blackboard values.

### Enemy AI

Enemies are driven by an **HTN (Hierarchical Task Network) planner** via the [Fluid HTN](https://github.com/ptrefall/fluid-hierarchical-task-network) library. The planner queries the baked cover points and the player's state (health, visibility, position) through a shared world-state blackboard to produce coherent multi-step plans — patrolling, engaging, seeking cover, flanking, and so on.

> ⚠️ **Work in progress** — the AI agent is in its early stages and only the basics are implemented so far. The HTN planner is not yet meaningfully showcased through agent behaviour. That said, the demo already has a lot of substance in the player controller, cover system, and sensor — these are the main things worth exploring right now.

---

## Project Structure

```
FluidHTN-FPS-Demo/
├── Assets/
│   ├── Scripts/
│   │   ├── Core/                        # Game manager singleton, AI settings
│   │   ├── Editor/                      # Custom inspectors (ThirdPersonController)
│   │   ├── FSM/                         # Generic finite state machine
│   │   ├── Input/                       # Input manager (Unity Input System)
│   │   ├── NPC/
│   │   │   ├── Conditions/              # HTN conditions (world state checks)
│   │   │   ├── Domains/                 # HTN domain + task definitions
│   │   │   ├── Effects/                 # HTN effects (world state mutations)
│   │   │   ├── FSMs/                    # NPC weapon FSM and states
│   │   │   ├── Operators/               # HTN operators (move to cover, shoot, flank, etc.)
│   │   │   ├── Sensors/                 # Vision, enemy, cover position sensors
│   │   │   ├── AIContext.cs             # HTN world-state blackboard
│   │   │   ├── AIDomainBuilder.cs       # HTN domain builder
│   │   │   ├── AIWorldState.cs          # World state enum
│   │   │   ├── NPC.cs                   # NPC entry point
│   │   │   └── ThirdPersonController.cs # NPC movement controller (NavMeshAgent + IK)
│   │   ├── NPCUtils/
│   │   │   ├── Classifiers/             # Position change classifiers (added/modified/removed)
│   │   │   ├── Core/                    # Corner finder, position validator, tactical position generator
│   │   │   ├── Data Containers/         # Tactical position data and grid spawner data
│   │   │   ├── Debug/                   # Gizmo and debug visualisers for tactical positions
│   │   │   ├── Editor/                  # Editor window for baking tactical positions
│   │   │   └── Settings/                # Generator and scan settings
│   │   ├── Player/
│   │   │   ├── CameraMovement.cs        # Mouse look / camera
│   │   │   ├── DetectionDirectionFiller.cs
│   │   │   ├── DetectionDirectionUpdater.cs  # GUI showing from which direction player is seen
│   │   │   ├── EnvironmentScanner.cs    # Physics raycast scanning for climbable surfaces
│   │   │   ├── Player.cs                # Core player character (floating capsule, grounding, events)
│   │   │   ├── PlayerClimbing.cs        # Environment and ladder climbing
│   │   │   ├── PlayerCrouching.cs
│   │   │   ├── PlayerJumping.cs
│   │   │   ├── PlayerLeaning.cs
│   │   │   ├── PlayerSprinting.cs
│   │   │   ├── PlayerWeaponController.cs
│   │   │   ├── PlayerWeaponMover.cs
│   │   │   └── PlayerWeaponUI.cs
│   │   ├── Shooting/                    # Weapon logic, collision detection, UI
│   │   ├── Target/                      # Health system, hit registration, visible body parts
│   │   └── Utils/                       # Pooling, physics helpers, layer manager, logging
│   └── ThirdParty/
│       ├── BloodDecalsAndEffects/       # Blood decal visual effects
│       ├── JMO Assets/                  # Particle effects
│       ├── Ishikawa1116/                # Low-poly guns pack
│       ├── LowPolySoldiers_demo/        # NPC character models
│       ├── Mixamo/                      # Additional animations
│       └── SFX/                         # Sound effects
├── Packages/
│   ├── Fluid-HTN/                       # Fluid HTN local package
│   └── manifest.json                    # Unity package dependencies
├── ProjectSettings/
└── README.md
```

---

## Getting Started

### Prerequisites

- **Unity 6** (6000.3.9f1)
- All Unity package dependencies are resolved automatically via `Packages/manifest.json` — no manual install needed
- Fluid HTN is included directly as a local package under `Packages/Fluid-HTN/`

### Setup

1. Clone the repository:
   ```bash
   git clone https://github.com/jkubala/FluidHTN-FPS-Demo.git
   ```
2. Open the project folder in **Unity Hub** and let package resolution complete.
3. Open a demo scene from `Assets/Scenes/DemoMaps/`.
4. Press **Play**.

### Baking Cover Points

Cover points must be baked before enemy AI can use them. With a demo scene open, use the **Tactical Position Generator** editor window to run the bake. Re-bake whenever the level geometry changes.

---

## Dependencies

| Dependency | Source |
|---|---|
| [Fluid HTN](https://github.com/ptrefall/fluid-hierarchical-task-network) | Local package at `Packages/Fluid-HTN/` |
| Unity AI Navigation (NavMesh) | `com.unity.ai.navigation` |
| Unity Input System | `com.unity.inputsystem` |
| Unity Animation Rigging | `com.unity.animation.rigging` |
| ProBuilder | `com.unity.probuilder` |
| Unity Timeline | `com.unity.timeline` |

---

## License

MIT — see [LICENSE](LICENSE) for details.
