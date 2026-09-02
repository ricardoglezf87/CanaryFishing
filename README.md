# CanaryFishing

Prototipo de simulación de pesca para Roblox, desarrollado con Luau.

## Primer prototipo

Este repositorio contiene el primer vertical slice:

- carga de potencia manteniendo el botón izquierdo;
- lanzamiento físico del señuelo;
- límite de lanzamiento controlado en servidor;
- tecla `R` para recoger el señuelo;
- HUD básico de potencia.
- picada simulada y barra de tensión;
- progreso de lucha con captura o rotura de línea.

## Instalación en Roblox Studio

1. Crea un proyecto nuevo de Roblox Studio.
2. Crea las carpetas `FishingSystem/Modules` y `FishingSystem/Remotes` dentro de `ReplicatedStorage`.
3. Copia `RodConfig.lua` y `FishingMath.lua` en `ReplicatedStorage/FishingSystem/Modules`.
4. Copia `FishingServer.server.lua` en `ServerScriptService`.
5. Copia `FishingClient.client.lua` en `StarterPlayer/StarterPlayerScripts`.
6. Ejecuta Play Test y mantén pulsado el botón izquierdo para lanzar.
7. Cuando aparezca la picada, mantén `R` para luchar. Suelta `R` para reducir la tensión.

El servidor crea automáticamente los `RemoteEvent` necesarios dentro de `FishingSystem/Remotes`.
