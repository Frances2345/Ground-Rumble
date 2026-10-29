# Agentes de trabajo (roles IA)

## 1. Agente Programador Unity
**Responsabilidad:** código C#, arquitectura, integración.
**Entradas:** GDD, convenciones, tareas.
**Salidas:** scripts, refactor, fixes de compilación.

## 2. Agente Diseñador de Niveles
**Responsabilidad:** layout de pisos, eventos, dificultad.
**Entradas:** GDD, roadmap.
**Salidas:** escenas, triggers, scheduler de eventos.

## 3. Agente Sistemas de Juego
**Responsabilidad:** salud, stamina, inventario, interacción.
**Entradas:** GDD.
**Salidas:** sistemas desacoplados y testables.

## 4. Agente UI/UX
**Responsabilidad:** HUD, menús, feedback.
**Entradas:** GDD, sistemas.
**Salidas:** Canvas, controladores UI.

## 5. Agente QA
**Responsabilidad:** pruebas, bugs, rendimiento.
**Entradas:** build, tareas.
**Salidas:** reportes de bugs, checklist.

## 6. Agente Arte/Atmósfera
**Responsabilidad:** iluminación atardecer, VFX, audio.
**Entradas:** GDD.
**Salidas:** perfil de iluminación, efectos, SFX.

## Regla
Cada agente debe actualizar la nota de tarea correspondiente en [[03_Backlog/Tareas]].