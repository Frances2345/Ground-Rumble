# Estructura del proyecto

## Carpetas clave
```
Assets/
├─ GroundRumble/
│  ├─ Scripts/
│  │  ├─ Core/
│  │  ├─ Player/
│  │  ├─ Level/
│  │  ├─ Events/
│  │  ├─ UI/
│  │  └─ Items/
│  ├─ Prefabs/
│  ├─ Settings/
│  └─ Images/
├─ Obsidian/
└─ InputSystem_Actions.cs
```

## Convenciones
- Namespaces: `GroundRumble.<Area>` (ej. `GroundRumble.Player`).
- ScriptableObjects para datos (`ItemData`, `DisasterEvent`, `FloorData`).
- Eventos con `UnityEvent` o C# `Action` para desacoplar HUD.
- Un sistema por archivo.