# ZombieShoot

ZombieShoot is a first-person survival shooter built with Unity and C#. Fight through five waves of zombies, scavenge supplies, and upgrade your weapons to take down the final boss.

## Getting started

You’ll need Git, Unity Hub, and Unity Editor **6000.3.11f1**. [Download Unity](https://unity.com/releases/editor/whats-new/6000.3.11f1) · [Project version](ProjectSettings/ProjectVersion.txt)

```bash
git clone https://github.com/Kenfiz123/PRU213_Zombie.git
cd PRU213_Zombie
```

1. In Unity Hub, go to **Projects → Add → Add project from disk**. Select the `PRU213_Zombie` folder containing `Assets`, `Packages`, and `ProjectSettings`.
2. Open the project with Unity `6000.3.11f1`. On first launch, wait for Unity to download the packages and import the assets.
3. In the **Project** window, open [Assets/Scenes/SampleScene.unity](Assets/Scenes/SampleScene.unity) and press **Play**.
4. Select **CHƠI GAME** (Play) in the main menu, then choose a difficulty.

## How to play

- Choose **Easy** (*Dễ*), **Normal** (*Bình thường*), **Hard** (*Khó*), or **Asian**. Difficulty affects enemy count and health, the damage you deal, and how often chests spawn.
- Defeat every zombie to advance to the next wave. The five waves are split into three stages with one, two, and two waves respectively. You’ll face melee, ranged, and explosive zombies before the boss appears.
- Open chests to find ammunition, healing items, armor, and skill cards. Cards can fully restore health or armor, or summon an ally.
- Spend points from zombie kills on weapon upgrades, character stats, or grenades. Defeat the boss to win.

## Controls

| Input | Action |
| --- | --- |
| `W` `A` `S` `D` | Move |
| Mouse movement | Look and aim |
| Left `Shift` | Sprint |
| `Space` | Jump |
| `C` while moving | Dash in the direction of movement |
| Scroll wheel | Cycle through the pistol, AK, and knife |
| Left mouse button | Fire or perform a light knife attack |
| Right mouse button | Perform a heavy knife attack |
| `R` | Reload |
| `G` | Throw a grenade if you have one |
| `F` | Toggle the flashlight |
| `E` | Open a chest, pick up an item, or use the selected skill card |
| `1` `2` `3` | Select a skill card slot, then press `E` to use it |
| Hold `Tab` | Open the inventory |
| `B` | Open the upgrade menu |
| `H` | Open the in-game guide |

`E` prioritizes nearby chests and items over skill cards.

## Build the game

1. Open **File → Build Profiles** and choose a target platform, such as **Windows**. If Unity requests an additional module, install it through Unity Hub for the Editor version you’re using.
2. Check that `Assets/Scenes/SampleScene.unity` is enabled in the build’s scene list. The scene is enabled in the [default build settings](ProjectSettings/EditorBuildSettings.asset).
3. Select **Build** or **Build And Run**. You can save the output to `Builds/`, which Git ignores.

[Unity 6.3 Build Profiles documentation](https://docs.unity.com/en-us/engine/6000.3/manual/building-and-publishing/build-settings/build-profiles)

## Project structure

| Path | Contents |
| --- | --- |
| [`Assets/Scenes/`](Assets/Scenes/) | Main scene and NavMesh data |
| [`Assets/Code_Off/Player/`](Assets/Code_Off/Player/) | Movement, weapons, character stats, inventory, and skills |
| [`Assets/Code_Off/Zombie/`](Assets/Code_Off/Zombie/) | Wave management, zombie AI, and boss behavior |
| [`Assets/Code_Off/Chest/`](Assets/Code_Off/Chest/) | Chests, loot, and pickups |
| [`Assets/Code_Off/UI/`](Assets/Code_Off/UI/) | Menus, difficulty settings, HUD, and upgrade menu |
| [`Assets/Code_Off/Environment/`](Assets/Code_Off/Environment/) | Explosive barrels and barrel spawning |
| [`Assets/Code_Off/Optimization/`](Assets/Code_Off/Optimization/) | Object pooling, level of detail, and performance utilities |
| [`Assets/Prefab/`](Assets/Prefab/) | Prefabs for zombies, the boss, chests, items, and effects |
| [`Packages/`](Packages/) and [`ProjectSettings/`](ProjectSettings/) | Unity packages and Editor, input, and build settings |

To change the wave count, enemy pools, or chest spawn points, open `SampleScene` and select the `WaveManager` component on `GameManager`. **Active Input Handling** is set to **Both** because the game uses Unity’s Input System and legacy Input Manager.
