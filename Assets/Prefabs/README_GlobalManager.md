How to create a GlobalManager prefab (Unity)

1. In Unity Editor, create an empty GameObject in your starting scene: GameObject > Create Empty.
2. Rename it to `GlobalManager`.
3. Add the `GlobalManager` component: Add Component > GlobalManager (script is at Assets/Scripts/Core/GlobalManager.cs).
4. Configure fields in the Inspector:
   - Assign `AppConfig` (drag the AppConfig asset or create one via Assets > Create > TimeAura > App Config).
   - Assign `FirebaseDataService`, `TwilioSmsGateway` services (or leave empty to let `TimeAuraInstaller` create/find them).
   - Assign manager components (`AuthManager`, `GameManager`, `LocalizationManager`, `AuraEffectManager`, `SecurityHub`, `MatchingManager`) if you prefer inspector control. Otherwise the installer will create missing components at runtime.
5. Make the GameObject a prefab: drag the `GlobalManager` GameObject into `Assets/Prefabs` to create `GlobalManager.prefab`.
6. Ensure `TimeAuraInstaller` is present in `ProjectContext`/SceneContext and, optionally, assign the created `GlobalManager` instance to the `globalManager` field of the installer in the Inspector.

Notes
- `GlobalManager` is DontDestroyOnLoad so it persists between scenes.
- If you assign managers manually in the Inspector the installer will use those instances and Zenject will Inject their `[Inject]` fields.
- For CI/QA, add `BootstrapValidator` to any bootstrap GameObject to log missing DI bindings on scene start.