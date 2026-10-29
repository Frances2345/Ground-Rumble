# Sistemas del juego

## Core
- `PlayerController` – movimiento, crouch, sprint, stamina.
- `CameraController` – seguimiento y colisión.
- `HealthSystem` – vida y daño.
- `BodyPartSystem` – partes heridas.
- `StaminaSystem` – consumo/regeneración.

## Interacción
- `IInteractable` – interfaz común.
- `InteractionRaycaster` – detección.
- `Highlighter` – feedback visual.

## Niveles
- `FloorManager` – piso actual y transición.
- `StairTrigger` – bajar de piso.
- `EventScheduler` – eventos programados.

## Eventos de desastre
- `DisasterEvent` (SO) – datos del evento.
- `DebrisSpawner`, `FireZone`, `ElectricHazard`, `GlassShatter`.

## UI
- `HUDController` – barras y piso.
- `MenuController` – menús y pausa.