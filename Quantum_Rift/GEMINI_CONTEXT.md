# Project Knowledge Base: Quantum Rift

## 1. Project Overview
*   **Project Name:** Quantum Rift
*   **Genre:** 2D Top-down Action / RPG
*   **Art Style:** Pixel Art
*   **Core Systems:** 
    *   Modular Character Selection System (Carousel style).
    *   Top-down movement with 8-directional input, Dashing, and Sprinting[cite: 6].
    *   Combat and Survival mechanics including Knockback, Invincibility Frames (I-Frames), Skill Cooldowns (Q/E), and Weapon Swapping[cite: 3, 6].
    *   Scene transition system passing selected character data from the Main Menu to the Gameplay map[cite: 2, 4, 5].

## 2. Tech Stack & Tools
*   **Game Engine:** Unity (2D Core)
*   **Asset Pipeline (Art):** 
    *   Assets drawn primarily in Procreate.
    *   Exported as transparent `.png` files to maintain pixel integrity.
    *   Unity Sprite Settings: `Sprite Mode: Multiple`, `Filter Mode: Point (no filter)`, `Compression: None`.
*   **Version Control:** Unity Version Control (Plastic SCM) for team collaboration and cloud repository synchronization.
*   **Input System:** Configured to `Both` (Legacy Input Manager + New Input System) in Player Settings to resolve conflicts and support `Input.GetAxisRaw` usage[cite: 6].
*   **UI System:** Unity Canvas, `TextMeshProUGUI` for typography.

## 3. Architecture & Code Conventions
*   **Language & Approach:** C#, highly modular architecture decoupled into specific Managers.
*   **Data Management:** Heavy reliance on `ScriptableObject` for game data configurations (`CharacterData`, `WeaponData`, `SkillData`) to separate data from logic[cite: 3, 5].
*   **State & Scene Management:**
    *   `GameManager` holds static data (e.g., `public static CharacterData selectedCharacter`) to persist information across Scene loads[cite: 5].
    *   `SceneManager` handles transitions from the Main Menu to the Gameplay Scene (e.g., `GameScene` / `map_1`)[cite: 4].
*   **Code Conventions:** 
    *   Use of `[Header(...)]` for Inspector organization[cite: 3, 4].
    *   Coroutines (`IEnumerator`) for time-based actions like Dash duration, Knockback recovery, and I-Frame blinking[cite: 3, 6].

## 4. Completed Systems
*   **Main Menu & UI Flow:**
    *   Working Navigation (Start, Settings, Exit, Character Detail Pop-up) managed by `MainMenuController`[cite: 4].
    *   Character Selection Carousel (`CharacterSelectionManager`) capable of reading `CharacterData`, instantiating Prefabs (`CharacterBox_Template`), and toggling `SetActive` for Left/Right navigation.
*   **Player Mechanics:**
    *   `PlayerMovement`: Handles WASD/Arrow keys, Shift to run, sprite flipping, Dash, and Knockback physics via `Rigidbody2D`[cite: 6].
    *   `PlayerStats`: Handles Health/Energy pools, UI updates (`HUDManager`), Cooldown tracking, I-frames (blinking sprite), and Currency[cite: 3].
*   **Game Loop Setup:**
    *   `PlayerSpawner`: Reads the selected character from `GameManager` and instantiates the correct Prefab at the Start point[cite: 2, 5].
    *   `CameraFollow`: Smoothly interpolates (Lerp) to track the spawned player[cite: 1, 2].

## 5. Current State & Known Issues
*   **Current State:** 
    *   Transitioning from Main Menu UI logic to Game Scene integration.
    *   Ensuring `PlayerSpawner` accurately instantiates the `CharacterPrefab` and passes the Transform to `CameraFollow` upon entering the map (`map_1`)[cite: 1, 2].
*   **Recently Resolved Issues:**
    *   *Input Handling Conflict:* Fixed `InvalidOperationException` by enabling "Both" input systems to support `Input.GetAxisRaw`.
    *   *UI Button Sticking:* Resolved sprite-swap visual bugs by setting UI Button `Navigation` to `None`.
    *   *UI Script Mismatches:* Corrected Type mismatches by using `TextMeshProUGUI` consistently.