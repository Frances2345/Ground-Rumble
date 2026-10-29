# Convenciones

## Código
- C# con `PascalCase` para clases/métodos, `camelCase` para campos privados.
- Serializados con `[SerializeField]`.
- Evitar `FindObjectOfType` en Update; cachear referencias.

## Git
- Ramas: `feature/<nombre>`, `fix/<nombre>`.
- Commits: `feat:`, `fix:`, `docs:`, `refactor:`.

## Assets
- Prefabs en `Assets/GroundRumble/Prefabs`.
- ScriptableObjects en `Assets/GroundRumble/Settings`.

## Documentación
- Toda decisión técnica se anota en [[03_Backlog/Tareas]].