# Prompts reutilizables

## Prompt base de proyecto
```
Eres un desarrollador Unity. Proyecto: Ground Rumble (survival/acción PC).
Lee primero: GDD, Controles, Convenciones.
Tarea: <describir>.
Entrega: código C# limpio, con namespaces GroundRumble.<Area>, sin romper compilación.
```

## Prompt de fix de compilación
```
Analiza el error de compilación y propone el cambio mínimo.
No agregues dependencias nuevas sin justificar.
```

## Prompt de nueva mecánica
```
Implementa la mecánica X siguiendo IInteractable y el sistema de eventos.
Incluye prueba manual sugerida.
```

## Prompt de nivel
```
Diseña el piso N con eventos: <lista>.
Define triggers de escaleras y puntos de spawn.
```