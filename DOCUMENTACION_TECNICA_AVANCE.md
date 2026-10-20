# Ecos de una Era Olvidada — Documentación técnica de avance

> Proyecto: **Proyecto Final VFX** · Unity **6000.3.22f1** · URP **17.3.0**
> Paquetes relevantes: Input System 1.20.0 · AI Navigation 2.0.16 · uGUI 2.0.0 · ProBuilder 6.1.2 · Timeline 1.8.12 · Visual Scripting 1.9.12 · **Visual Effect Graph 17.3.0 · Shader Graph 17.3.0**
> Escenas: `Assets/Scenes/Maqueta Nivel 1.unity` y `Assets/Scenes/Maqueta Nivel 2.unity`

---

## 1. Arquitectura de scripts

```text
Assets/Scripts/
├─ Core        CameraFollow · FSM(legado) · GameProgressManager · PlayerController
│              PlayerInputReader · IInteractuable · HeroInteractor
│              GestorDerrotaHeroe · ZonaDeAbismo
├─ Combat      HitstopManager · PlayerCombat · IGolpeableElemental
├─ Health      HealthSystem(legado) · IDamageable · HealthComponent · DamageFlash
├─ TagTeam     HeroCombatData · TagTeamManager · TemporarySummon
├─ AI          EnemyAI
├─ Puzzles     PuzzleActivator · SequencePuzzleManager · WeightPressurePlate · PuzzleDoor
│              InteractableItem · MarcadorMuroBasalto · RelayApertura · RelayAparicion
│              RelayEnergia · CeldaDeRescate · BulboEolico · GeiserRescate
│              EngranajeEolico · RafagaViento · PersistentPuzzleObstacle(legado)
├─ UI          CombatHUDManager · UIModalWindow · Billboard · WorldSpaceHealthBar
│              HeroAbilityTreeUI · ResourceCodexUI
└─ Editor      Level1BlockoutBuilder · Level1AdvancedDecor · Level1DetailAndAtmosphere
               Level2WindIslandsBuilder · VFXGeneratorTool · HUDGeneratorTool
               AuraOps/*  (operaciones puntuales de editor, no se compilan en build)
```

**Regla de dependencia:** `UI → Core/Health/TagTeam → Combat → Puzzles`. Ningún script de
`Puzzles` lee entrada directamente; toda la interacción entra por `PlayerInputReader`.

---

## 2. Contratos de comunicación

| Sistema | API pública | Consumidores |
|---|---|---|
| `PlayerInputReader` (`DefaultExecutionOrder(-100)`) | `Movimiento`, `DeltaRaton`, `SaltoPulsado`, `DashPulsado`, `AtaquePulsado`, `HabilidadPulsada`, `InteraccionPulsada`, `AlternarRecursos`, `AlternarHabilidades`, `AlternarCursor`, `RelevoSolicitado` (−1/0/1), `EnModoJuego`, **`ReinicioPulsado`** (se lee siempre, incluso bloqueado), **`EntradaBloqueada`** (anula movimiento, salto, dash y combate) | `PlayerController`, `PlayerCombat`, `HeroInteractor`, `CombatHUDManager`, `GestorDerrotaHeroe` |
| `HealthComponent` | `OnHealthChanged(float,float)`, `OnDamaged(float,Vector3)`, `OnDied`, `Initialize(max,actual)`, `ApplyDamage(cantidad, retroceso, punto)`, `Normalizada`, `EstaMuerto` | `WorldSpaceHealthBar`, `CombatHUDManager`, `TagTeamManager`, `EnemyAI`, `DamageFlash` |
| `TagTeamManager` (singleton) | `SaludHeroe`, `OnHeroeCambiado`, `OnVidaHeroeCambiada`, `IntentarRelevo`, `CambiarHeroe`, `DesbloquearIllya`, `illyaDesbloqueada`, **`puntoReaparicion`**, **`Reaparecer()`**, **`HeroeCaido(int)`**, **`OnAmbosCaidos`**, **`OnReaparecido`** | `CombatHUDManager`, `PlayerCombat`, `HeroInteractor`, `GestorDerrotaHeroe` |
| `EnemyAI` | `Arquetipo {FaunaCorrupta, HuesteCelestial, BestiaPiedraVoraz, GuardianCeleste}`, `AplicarRetroceso(vector)`, **`radioPersecucionMaximo`**, **`confinadoAArena`**, **`radioArena`**, **`AplicarInmovilizacion(segundos)`**, **`ReiniciarEstado()`** | `HealthComponent` (deriva el knockback), `PlayerCombat`, `GestorDerrotaHeroe` |
| `GestorDerrotaHeroe` | `OnAmbosCaidos` → panel + TMP `"CAÍDO EN COMBATE - Presiona R para reiniciar"`; en 'R' → `Reubicar` + `Reaparecer` + `ReiniciarEnemigosCercanos(80 m)` | `TagTeamManager`, `PlayerInputReader`, `PlayerController` |
| `GameProgressManager` | `RegistrarCofreOItem(id)`, `EstaRecogido(id)` | `InteractableItem`, `CeldaDeRescate`, `PuzzleDoor` |

**Vida 100 % por eventos.** `CombatHUDManager.Update()` solo anima la barra fantasma, el
radial de cooldown y los modales; ningún valor de vida se sondea por frame.

---

## 3. Decisiones de física y matriz de colisión

### 3.1 Capas del proyecto

| Índice | Capa | Uso |
|---|---|---|
| 0 | Default | Enemigos, gameplay suelto, VFX con collider |
| 5 | UI | Todo el HUD (uGUI) |
| 8 | Player | Rig jugable completo |
| 9 | Environment | Blockout de ambos niveles: pisos, muros, islas, decorado |

### 3.2 Máscaras implementadas (equivalen a la matriz de colisión efectiva)

| Script | Máscara | Valor | Consecuencia |
|---|---|---|---|
| `PlayerCombat.capasAfectadas` | solo Default | `1` | Los ataques **no** golpean decorado ni el blockout (Environment 9) → no se generan VFX/hitstop por impactos en escenario |
| `CameraFollow.collisionLayers` | Default + Environment | `513` | La cámara choca con el escenario y **excluye Player** → sin clipping interno ni retroceso de cámara sobre el propio héroe |
| `NavMeshSurface.layerMask` (Nivel 2) | Environment | `1 << 9` | Horneado con `collectObjects = Children` + `PhysicsColliders`, `agentTypeID = 0` → 12 superficies caminables sobre 48 colliders |
| `PlayerCombat` raycast de golpe | Default | `1` | El objetivo del golpe siempre es un enemigo en Default |

### 3.3 Sincronización NavMesh ↔ PhysX (retroceso)

`EnemyAI` alterna el control del cuerpo durante el aturdimiento:

```text
Entrada en Aturdido:  agent.updatePosition = false · rb.isKinematic = false · AddForce(retroceso)
Salida de Aturdido:   agent.Warp(NavMesh.SamplePosition(...)) · rb.isKinematic = true · agent.updatePosition = true
```

Esto evita que el `NavMeshAgent` pelee contra el `Rigidbody` y que el enemigo quede
desincronizado del navmesh tras salir despedido.

### 3.4 Cuerpos cinemáticos intencionales

- `Muro_Basalto_Habilidad.prefab`: `Rigidbody` cinemático + `MarcadorMuroBasalto`. El muro **no**
  se simula; cae por animación/posición y activa las placas por trigger (`WeightPressurePlate`,
  trigger de 2.6 × 2.6 × 0.70 m — ver §3.7).
- `Plataforma_Liana_Temporal.prefab`: plataforma cian de 5 s que se carga por `Resources.Load`
  desde `BulboEolico` (no está en la escena hasta que se invoca).

### 3.5 Regla de no destrucción del blockout

`Level1BlockoutBuilder.GenerarNivelMaestro` **nunca se ejecutó**. Toda la integración de contenido
se hizo de forma aditiva e idempotente (`IntegracionNivel1` / `IntegracionNivel2` reconstruyen sus
propios objetos y respetan la geometría existente).

### 3.6 Red de seguridad del abismo (Nivel 2)

El volumen `00_KillZone_Abismo` (BoxCollider trigger de 180 × 2 × 180 en Y = −12) se creaba sin
comportamiento asociado: el héroe lo atravesaba y caía indefinidamente. `ZonaDeAbismo` lo cubre:

- muestrea cada 0.35 s la última posición **apoyada** (`CharacterController.isGrounded`);
- solo la memoriza si hay suelo en un radio de 1.2 m en ±X y ±Z (5 raycasts) → nunca deja el
  punto de reaparición al filo de una isla;
- al cruzar el volumen llama a `PlayerController.Reubicar(destino)`, que desactiva el
  `CharacterController`, recoloca y **anula la velocidad acumulada** (si no, el héroe reaparece
  con la caída de ~25 m/s y vuelve a cruzar el volumen);
- enfriamiento de 0.3 s entre rescates para no encadenar eventos del mismo impacto.

`GeiserRescate` (Nivel 2) comparte el mismo mecanismo: en lugar de recolocar el `Transform` con
el `CharacterController` desactivado a mano, llama a `PlayerController.Reubicar`, de modo que el
héroe no reaparece conservando la velocidad de caída.

### 3.7 Trigger de las placas de presión: penetración mínima vs `skinWidth`

Las placas de presión nacían como losas de 0.12 m de alto (cara superior en Y = 0.62) sobre un
piso cuya cara superior está en Y = 0.50. El `CharacterController` del héroe (height 2.0,
radius 0.5, `skinWidth` 0.08) apoya a **Y = 0.58**, de modo que solo **4 cm** del casquete
inferior de la cápsula quedaban dentro del trigger.

Esa penetración es **menor que el `skinWidth`**, y PhysX filtra los contactos dentro de esa banda:
el `OnTriggerEnter` **nunca** se disparaba. La comprobación que lo aisló:

| Medición | Resultado |
|---|---|
| `Collider.bounds` héroe / placa | y 0.58 – 2.58 vs 0.50 – 0.62 → `Intersects` = `true` |
| `Physics.OverlapBox` con el volumen real de la placa (4 cm) | **no** devuelve al `Player` |
| `Physics.OverlapBox` con un volumen profundo (y 0.4 – 1.6) | **sí** devuelve `Player(L8)` |
| `Physics.GetIgnoreLayerCollision(8, 0)` | `false` — la matriz no era la causa |

Corrección aplicada (escena y `IntegracionNivel1.EngrosarTriggerPlaca`, para que la regeneración
la reproduzca): el trigger de ambas placas pasa a **0.70 m de alto real**, con el centro 0.24 m
sobre la placa (mundo: y 0.45 – 1.15); el volumen visible no cambia. Penetración resultante con el
héroe apoyado: 0.57 m ≫ `skinWidth`.

**Regla general del proyecto**: cualquier trigger que deba detectar al héroe por contacto con el
suelo necesita al menos ~10 × `skinWidth` de solape vertical; un trigger de solo el espesor del
objeto decorativo no es fiable.

### 3.8 Zona de guardia (leash) por arquetipo

`EnemyAI.AplicarLimitesPorArquetipo()` se ejecuta en `Start()` y fija el radio de persecución
respecto de `origenPatrulla`. Al superarlo, `IniciarRegreso()` cancela la corrutina de ataque, pasa
el `Rigidbody` a cinemático y llama a `SetDestination(origenPatrulla)`; al llegar, el enemigo
**recupera la vida completa** y vuelve a `Patrulla`.

| Arquetipo | `radioPersecucionMaximo` | `confinadoAArena` | Uso |
|---|---|---|---|
| `FaunaCorrupta` | **12 m** | `false` | Enemigo de Arena A del Nivel 1 |
| `HuesteCelestial` | **10 m** | `false` | Enemigo de Arena A del Nivel 2 |
| `BestiaPiedraVoraz` | `radioArena` = **14 m** | `true` | Minijefe del Nivel 1 |
| `GuardianCeleste` | `radioArena` = **14 m** | `true` | Minijefe del Nivel 2 |

El valor queda serializado en la escena por `IntegracionBloquesFinales.AjustarLeashEnemigos()`, de
modo que no depende de los valores por defecto del script.

### 3.9 Micro-offset anti Z-fighting (+0.02 m)

Las placas, canales y losas del blockout se generaron coplanares con su piso de apoyo, lo que
producía parpadeo de profundidad a distancia. Se elevó **+0.02 m** la Y de cada pieza implicada
(por debajo del umbral perceptible, por encima del error de la depth buffer de 24 bits a esas
distancias). El desplazamiento se aplica una sola vez por sesión de editor mediante
`SessionState` (`AuraOps.MicroOffset.N1` / `.N2`), de modo que el integrador es idempotente.

| Nivel | Objeto | Y final |
|---|---|---|
| N1 | `Placa_Presion_A`, `Placa_Presion_B` | **0.58** |
| N1 | `Canal_Runico_Presion` | **0.57** |
| N1 | `Losa_Puente_A`, `Losa_Puente_B` | **3.17** |
| N2 | `Placa_Tobera` | **8.52** |
| N2 | `Ranura_Muro_1`, `Ranura_Muro_2`, `Ranura_Muro_3` | **6.12** |

> **Desviación consciente del GDD:** el GDD pedía las placas en Y = 0.1, pero la cara superior de
> `Piso_Puzzle` está en 0.50; dejarlas en 0.1 las hundiría dentro del piso. Se conserva 0.56 de
> base + 0.02 de micro-offset.

### 3.10 Bloqueo físico de los lagos de maná (Nivel 1)

Los lagos de maná eran geometría decorativa sin colisión: el héroe podía caminar sobre el líquido y
el NavMesh los consideraba transitables. Se creó un `BoxCollider` sólido de **3.5 m de alto**
envuelto en un `NavMeshModifier` con `overrideArea = true` y `area = 1` (*Not Walkable*).

| Barrera | Centro | Tamaño |
|---|---|---|
| `Barrera_Fisica_Lago_Mana_Norte` | (40, 1.85, 75) | XZ del `Bounds` del lago × 3.5 m |
| `Barrera_Fisica_Lago_Mana_Sur` | (30, 1.85, 2) | XZ del `Bounds` del lago × 3.5 m |

El collider bloquea al `CharacterController` y el modificador impide que el horneado del NavMesh
marque el lago como caminable, sin tocar la geometría visible.

---

## 4. Código cromático del blockout

`Assets/Materials/BlockoutColors/` — **19 materiales** URP/Lit que codifican significado por color.
Todos usan la misma convención: `_BaseColor` saturado + `_Smoothness` bajo (lectura de blockout).

| Familia | Material | Color / significado |
|---|---|---|
| Suelo y estructura | `M_Blockout_SueloTierra` | Marrón tierra — piso caminable |
| | `M_Blockout_RocaMuro` | Gris roca — muro / límite |
| | `M_Blockout_PiedraRuina` | Piedra desaturada — ruina antigua |
| Elementos vivos | `M_Blockout_MaderaTronco` | Marrón madera — troncos y raíces sanas |
| | `M_Blockout_FollajeCopa` | Verde follaje — copa de árbol |
| | `M_Blockout_RaizCorrupta` | Violeta oscuro — raíz corrompida |
| Mecánicas elementales | `M_Blockout_MecanicaPiedra_AzulOceano` | Azul — placas y muros de Tierra |
| | `M_Blockout_MecanicaMadera_VerdeEsmeralda` | Verde esmeralda — mecanismos de Madera |
| | `M_Blockout_MecanicaViento_CianTranslucido` | Cian translúcido (`_Surface=1`, `_ZWrite=0`, renderQueue Transparent) — viento / plataformas temporales |
| Enemigos | `M_Blockout_Enemigo_FaunaSalmon` | Salmón — Fauna Corrupta |
| | `M_Blockout_Enemigo_HuesteCarmesi` | Carmesí — Hueste Celestial |
| | `M_Blockout_Minijefe_PurpuraProfundo` | Púrpura — minijefes (Bestia, Guardián) |
| Señalización | `M_Blockout_Pista_BlancoResplandeciente` | Blanco — pistas y monolitos |
| | `M_Blockout_Totem_AmbarBrillante` | Ámbar — tótems elementales |
| | `M_Blockout_Recompensa_DoradoMetalico` | Dorado metálico — cofres y llaves |
| | `M_Blockout_ReliquiaOro` | Oro — reliquias y pedestales |
| Ambiente | `M_Blockout_FuegoEmisivo` | Emisivo naranja — brasas / antorchas |
| | `M_Blockout_ManaMuertoAgua` | Azul-agua oscuro — maná muerto |
| | `M_Blockout_VaporMana` | Vapor translúcido — geiser y niebla de maná |

---

## 5. Contenido integrado por nivel

### Nivel 1 — `Maqueta Nivel 1`

| Zona | Piezas clave |
|---|---|
| Rig y spawn | Player (capa 8) en **(0, 0.8, 0)** con rotación identidad; hijos `Indicador_Frontal` (prisma 0.22, rot Z 45°, local 0/1.35/0.60) y `Arma_Blockout_Caelen` (escala 0.12/0.12/0.70, local 0.42/1.00/0.34), ambos sin collider. `_Managers` con `HitstopManager` + `GameProgressManager` |
| Arena A | Enemigo con `HealthComponent` (100 HP) + `DamageFlash` + `NavMeshAgent` (r 0.55 / h 1.9 / v 3.4) + `EnemyAI` (Fauna Corrupta, rango 2.6, daño 10) |
| Puzle de placas | `Pista_Monolito`, `Canal_Runico`, `Placa_Presion_A` (Muro de Basalto, 5 s), `Placa_Presion_B`, `Reja_Runica_Escalinata` |
| Arena B | `Gestor_RunasCiegas` (18 s) con 4 tótems: Topo, Ciervo, Halcón, Oso + `Losa_Puente_A/B` |
| Jefe | `Minijefe_BestiaPiedraVoraz` (240 HP), `Pedestal_Llave_Zarzas` (30 HP) → `Llave_de_las_Zarzas` (id `LlaveDeZarzas`), `Celda_Illya` + su trigger de interacción |
| Secreto | `Gestor_EcoTresRaices` (35 s) con tres corazones (Repisa, Cueva, Lecho) + `Sello_Organico_TresRaices` |
| Cofres | `Cofre_Atajo_N1`, `Cofre_Mirador_N1` |
| Riesgo | Lagos de maná sellados con barreras físicas de 3.5 m + área *Not Walkable* (§3.10) |

### Nivel 2 — `Maqueta Nivel 2`

| Zona | Piezas clave |
|---|---|
| Rig y spawn | Player (capa 8) en **(0, 1.15, 0)** sobre `01_Isla_Spawn_RiscosDelSur/Isla_Spawn/Superficie_Caminable` (techo y = 1.0); mismos hijos `Indicador_Frontal` y `Arma_Blockout_Caelen`; `_Managers` con `HitstopManager` + `GameProgressManager` |
| Navegación | `NavMeshSurface` en `Level2_Isla_Del_Viento_Blockout`: `collectObjects = Children`, `layerMask = 1 << 9`, geometría por `PhysicsColliders`, apuntando a **`Assets/Scenes/Maqueta Nivel 2/NavMesh_Level2.asset`** |
| Arena A | `Enemigo_HuesteCelestial_ArenaA` (110 HP, daño 12) con barra World Space a 2.7; reubicado de (0, 5, 36) —dentro de `Pilar_Resonancia`— a **(5, 5, 36)** y validado con `NavMesh.SamplePosition` en (5.00, 4.02, 36.00); `baseOffset = 1.0` |
| Ruta eólica | 4 `BulboEolico` escalonados → `Plataforma_Liana_Temporal` de 5 s; `Geiser_Rescate_Corriente` → `Ancla_Rescate_ArenaA` |
| Santuario | `Fresco`, `Tobera`, `Placa_Tobera` (8 s), `Plataforma_Descendente` + `RelayEnergia`, `Engranaje_Eolico_Anclaje`, `Puerta_Templo_Jefe` |
| Jefe | `Minijefe_GuardianCeleste` (260 HP, daño 20, `baseOffset = 1.3`, `confinadoAArena` con radio 14), `Nucleo_Engranaje_Eolico` (id `NucleoEolico`, oculto tras `RelayAparicion`), `Altar_Insercion_Nucleo` |
| Secreto | Pasillo de muros + 3 ranuras, `Rafaga_Pasillo_Secreto` (fuerza 6 hacia +X), `Monolito_Sellado_Aguja` (80 HP), `Cofre_Aguja_N2` |
| Riesgo | `KillZone` a Y = −12 (recuperación por geiser y `ZonaDeAbismo`) |

---

## 6. HUD y ventanas modales

`Canvas_CombatHUD` contiene barras duales de héroe, radial de cooldown, avisos de relevo y dos
modales. Tras la ampliación (operación `AmpliarModalesHUD`), ambos modales miden **1040 × 620**:

```text
Modal_Ventana_Habilidades           Modal_Ventana_Recursos
├─ Header  (980×58 @ y −46)        ├─ Header  (980×58 @ y −46)
├─ Rejilla_Nodos  480×440 (−250)   ├─ Rejilla_Recursos     380×440 (−300)
│   └─ Nodo_1..6 · 146×100         │   └─ Tarjeta_1..3 · 340×118
├─ Detalle_Habilidades 480×440 (+260)  └─ Detalle_Recursos 560×440 (+220)
│   ├─ Detalle_Icono 84×84
│   ├─ Detalle_Nombre / Detalle_Costo
│   └─ Detalle_Descripcion 440×200
└─ Body (oculto: el detalle vive en el panel derecho)
```

Contenido del árbol: Muro de Basalto (3 Cristales), Piel de Roca (5 Oro), Raíces Voraces
(3 Cristales), Pulso de Savia (5 Esencias), Sinergia Telúrica (8 Cristales), Vínculo de Savia (8 Oro).
Códex: Oro Puro ×3, Cristales de Voto ×5, Esencias Elementales ×2, cada uno con uso y lore.

> **Nota de implementación:** el componente `Outline` de uGUI se **desactiva** en el fondo de los
> modales. `Shadow`/`Outline` añaden sus copias al *final* del flujo de vértices, por encima del
> relleno, de modo que sobre un panel opaco se veían como un bloque dorado sólido. El Header está
> anclado arriba (`anchorMin.y = 1`), así que su `anchoredPosition.y` se mide desde el borde superior.

---

## 7. Capacidades y límites de las herramientas sobre VFX Graph y Shader Graph

Sondeo ejecutado en el editor con `Assets/Scripts/Editor/AuraOps/SondearCapacidadesVFXGraph.cs`
(`Execute`, `ListarTiposAutoria`, `ListarMenusVFX`): consulta de paquetes registrados, reflexión sobre
los ensamblados cargados y enumeración de los `MenuItem` de creación registrados.

### 7.1 VFX Graph

| Comprobación | Resultado |
|---|---|
| Paquete `com.unity.visualeffectgraph` | **PRESENTE · 17.3.0** |
| `UnityEditor.VFX.VFXGraph` | PRESENTE (`Unity.VisualEffectGraph.Editor`), constructor público sin parámetros |
| `UnityEditor.VFX.VFXSlot` | PRESENTE (`Unity.VisualEffectGraph.Editor`) |
| `UnityEditor.VFX.VFXTemplateDescriptor` / `VFXTemplateHelper` | PRESENTE |
| `VFXGraph.CreateNewGraphAsset` | **AUSENTE** — no hay API de creación en una llamada |
| `UnityEngine.VFX.VisualEffect` | PRESENTE (`UnityEngine.VFXModule`) |
| `UnityEngine.VFX.VisualEffectAsset` | PRESENTE (`UnityEngine.VFXModule`) |
| Menú `Assets/Create/Visual Effects/Visual Effect Graph` | **REGISTRADO** (`UnityEditor.VisualEffectAssetEditorUtility.CreateVisualEffectAsset`) |
| Menú `GameObject/Visual Effects/Visual Effect` | **REGISTRADO** (`CreateVisualEffectGameObject`) |
| Menú `Window/Visual Effects/Visual Effect Graph` | **REGISTRADO** (`UnityEditor.VFX.UI.VFXViewWindow.ShowWindow`) |
| Assets `t:VisualEffectAsset` en `Assets/` | **0** |

**Conclusión:** VFX Graph **sí está instalado y operativo** en este proyecto (17.3.0, con el editor
de grafos, las plantillas y el menú de creación registrados). Por tanto **autorar un `.vfx` a mano
es posible**: `Assets → Create → Visual Effects → Visual Effect Graph` y se edita en la ventana de
VFX Graph.

Lo que **no** existe es una vía programática de creación: `VFXGraph.CreateNewGraphAsset` no está
expuesta y el grafo se construye manipulando `GraphData` + `VFXViewWindow`, que es API interna de
presentación. Por eso el proyecto **no** ha generado ningún `.vfx` por script: los efectos actuales
se resuelven con `ParticleSystem` + shaders de texto, que sí son autorizables por código.

`manage_vfx` opera sobre `ParticleSystem`, `VisualEffect`, `LineRenderer` y `TrailRenderer`
(Play/Stop/SetFloat), pero no crea el asset `.vfx`.

### 7.2 Shader Graph

| Comprobación | Resultado |
|---|---|
| Paquete `com.unity.shadergraph` | **17.3.0** |
| Ensamblado `Unity.ShaderGraph.Editor` | Cargado |
| `UnityEditor.ShaderGraph.ShaderGraphImporter` | PRESENTE |
| `UnityEditor.ShaderGraph.GraphData` (+ `GraphDataUtils`, `GraphDataReadOnly`) | PRESENTE |
| `UnityEditor.ShaderGraph.Drawing.MaterialGraphView` | PRESENTE |
| `UnityEditor.ShaderGraph.SubGraphAsset` / `ShaderSubGraphImporter` | PRESENTE |

**Conclusión:** Shader Graph está instalado (17.3.0) y el editor registra el juego completo de
menús de creación URP (`Lit`, `Unlit`, `Sprite Lit/Unlit`, `Decal`, `Fullscreen`, `Canvas`, `UI`,
`Six Way`, `Terrain`, `Sub Graph`, `Blank`). Autorar un `.shadergraph` a mano **sí es posible**.

Lo que no es viable es **generarlo o reestructurarlo por código**: `GraphData` exige manipular
nodos y slots internos, `MaterialGraphView` es capa de presentación y el archivo es JSON con
referencias por GUID interno. En el proyecto hay **0** `.shadergraph` propios (solo los que
trae TextMesh Pro).

**Vía usada en este proyecto:** shaders de texto (`.shader`), editables por completo con
`manage_shader` y `create_script`. El único shader propio es
`Assets/Shaders/DissolveSurface.shader`.

### 7.3 Resumen de límites

| Quiero… | ¿Se puede? | Herramienta |
|---|---|---|
| Crear un `.vfx` nuevo **a mano** | **Sí** — VFX Graph 17.3.0 instalado | `Assets → Create → Visual Effects → Visual Effect Graph` |
| Crear un `.vfx` **por script** | **No** — sin `CreateNewGraphAsset`; solo `GraphData` + `VFXViewWindow` internos | — |
| Ajustar un `ParticleSystem` (emisión, formas, color over lifetime) | Sí | `manage_vfx` / `execute_script` |
| Crear un `.shader` de texto completo | Sí | `manage_shader` |
| Crear un `.shadergraph` nuevo **a mano** | **Sí** — menús URP registrados | `Assets → Create → Shader Graph → URP → …` |
| Ajustar parámetros de un `.shadergraph` por nombre de propiedad | Sí, vía `Material.SetFloat/SetColor` | `manage_material` |
| Crear o reestructurar un `.shadergraph` con nodos **por script** | **No de forma fiable** | — (solo GraphView interno) |
| Editar un `.shadergraph` como YAML | Desaconsejado | Formato con GUIDs internos |

---

## 8. Registro de scripts de legado (no usar en contenido nuevo)

| Script | Ubicación | Estado | Sustituto |
|---|---|---|---|
| `HealthSystem` | `Assets/Scripts/Health/` | Conservado por compatibilidad: `HUDGeneratorTool`, `Level1BlockoutBuilder` e `IntegracionNivel1` todavía lo consultan. Ya **no** está montado en ningún enemigo. | `HealthComponent` + `DamageFlash` |
| `FSM` | `Assets/Scripts/Core/` | No montado en ningún GameObject. Lee `Keyboard.current` por su cuenta y duplica la locomoción. | `PlayerController` + `PlayerInputReader` |
| `PersistentPuzzleObstacle` | `Assets/Scripts/Puzzles/` | No montado en ningún GameObject. Lee la tecla F directamente. | `InteractableItem` + `HeroInteractor` |

Los tres llevan un banner `[LEGADO · NO USAR EN CONTENIDO NUEVO]` en su bloque de documentación.
**No** se marcaron con `[System.Obsolete]` para no generar advertencias de compilación en los
generadores de editor que aún los referencian.

---

## 9. Estado de verificación

| Elemento | Estado |
|---|---|
| Compilación | **Sin errores** (`No compile errors`) — comprobado tras cada edición, incluida la corrección de `GuardarAmbasEscenas` |
| Guardado en disco de ambas maquetas | **Verificado** — `GuardarAmbasEscenas()` guardó `Maqueta Nivel 1` → abrió y guardó `Maqueta Nivel 2` → restauró la original |
| Blockout Nivel 1 / Nivel 2 | Intactos; la integración fue aditiva |
| Modales del HUD | Ampliados y verificados visualmente en ambas escenas (1040 × 620, cabecera, rejilla, panel de detalle) |
| Movimiento del héroe (WASD) en Nivel 2 | **Verificado en Play Mode** — sin hundirse ni atravesar el suelo |
| Ataque con clic izquierdo | **Verificado en Play Mode** — 110 → 80 HP, `DamageFlash` activo 0.18 s, retroceso de ~0.48 m y VFX de impacto |
| Relevo con tecla 2 | **Verificado en Play Mode** — héroe activo Caelen → Illya, vida máxima 130 → 95, HUD y retrato actualizados |
| Derrota total y reinicio con 'R' | **Verificado en Play Mode** (Nivel 1 y Nivel 2) — 2 muertes → `AmbosCaidos=True`, `DerrotaActiva=True`, `Panel_Derrota` visible, `EntradaBloqueada=True`; `Reaparecer()` → derrota desactivada, panel oculto, entrada liberada, `caido[0..1]=False`, vida 130/130 y reaparición a 0.07–0.22 m del punto |
| Red de seguridad del abismo (Nivel 2) | **Verificado en Play Mode** — dos caídas → `Reapariciones` 0 → 1 → 2, reaparición a 2.4–3.6 m del borde, estable y sin recaída |
| `Muro de Basalto` → `Placa_Presion_A` → `Placa_Presion_B` → reja (Nivel 1) | **Verificado** — muro instanciado a 0.00 m del centro de la placa, canal rúnico con 4.3 s restantes, reja `Abierta=True` con desplazamiento 4.00 m y autodestrucción del muro a los 6.5 s |
| Consola en Play Mode | **0 errores y 0 excepciones** en las corridas de la batería de puzles; las advertencias `[VERIF] Detén Play Mode antes de lanzar la verificación` son ruido de la doble invocación de `execute_script` |
| NavMesh Nivel 2 | Horneado en `Assets/Scenes/Maqueta Nivel 2/NavMesh_Level2.asset`, referenciado por el `NavMeshSurface` de `Level2_Isla_Del_Viento_Blockout` |
| `agent.isOnNavMesh` en enemigos elevados | **Verificado en Play Mode** — `Minijefe_GuardianCeleste` en Y=15.02 y `Enemigo_HuesteCelestial_ArenaA` en Y=5.02: ambos con `isOnNavMesh=True`, `hasPath=True`, `baseOffset=1.00`. Nivel 1: `Enemigo` (Y=0.54) y `Minijefe_BestiaPiedraVoraz` (Y=1.56) también en malla (2/2 en cada escena) |
| `Muro de Basalto` activando `Placa_Presion_A` por caída física (Nivel 1) | **Verificado** (ver §3.7: requería elevar el trigger de las placas) |
| Trigger de placas por contacto del héroe | **Verificado** — `Placa_Presion_B` resuelve el circuito al pisarla con el canal activo |
| Ruta bulbo → `Plataforma_Liana_Temporal` → geiser de rescate | **Verificado** por la batería automática (N2-P5) |
| VFX `Destroy(instancia, 2f)` (impacto piedra/madera) | **Verificado en Play Mode** — 2 ataques → 0 → 2 instancias → 0 tras 2.6 s, en ambas escenas |
| Consola tras el cierre | **Limpia** — `errores=0 · advertencias=0 · total=0` |
| Barras World Space sobre enemigos | Verificada indirectamente (la barra de la Hueste se redujo al recibir daño) |

### 9.1 Batería automática de puzles

`Assets/Scripts/Editor/AuraOps/VerificarTodosLosPuzzles.cs` ejecuta el juego en **Play Mode real**,
resuelve las referencias dentro del play, desactiva los `EnemyAI` y valida cada paso disparando los
triggers e interacciones del propio juego. Sobrevive a la recarga de dominio mediante
`[InitializeOnLoadMethod]` + `SessionState["AuraOps.VerificacionPuzzles"]`.

| Puzle | Comprobación | Resultado |
|---|---|---|
| N1-P1 | Muro de Basalto energiza Placa A | True |
| N1-P1 | Placa B pisada en ventana → reja abierta | True |
| N1-P2 | Topo→Ciervo→Halcón→Oso eleva losas | True |
| N1-P3 | Pedestal revela la Llave de las Zarzas | True |
| N1-P3 | 'F' en la celda libera a Illya | True |
| N1-P4 | 3 Corazones de Savia retraen el sello | True |
| N2-P5 | Bulbo despliega plataforma de lianas | True |
| N2-P6 | Tobera sellada 8 s + plataforma desciende | True |
| N2-P6 | Anclaje de Illya abre el paso al jefe | True |
| N2-P7 | Núcleo insertado activa el altar | True |
| N2-P8 | Monolito sellado destruido | True |

Totales emitidos en consola: `"Total 'Maqueta Nivel 1': 6/6"` y `"Total 'Maqueta Nivel 2': 5/5"`
(11 comprobaciones para 8 puzles).

> **Defecto real detectado por la batería:** el diagnóstico del Nivel 1 devolvía `Progreso=False`
> porque **no existía `GameProgressManager`**: `InteractableItem` y `CeldaDeRescate` no podían
> registrar `"LlaveDeZarzas"` y el mini-puzle de la prisión de Illya era **irresoluble en juego**.
> Se añadió el gestor (junto con `HitstopManager`) a `_Managers` mediante
> `IntegracionBloquesFinales.AsegurarGestorProgreso()`.

### 9.2 Batería de pendientes (Play Mode)

`Assets/Scripts/Editor/AuraOps/VerificarPendientesPlayMode.cs` cierra los tres puntos que quedaban
abiertos. Corre en Play Mode real con el mismo patrón estático + `SessionState` y emite
`[PEND] OK/FALLO` por comprobación. Resultado: **20/20 en `Maqueta Nivel 2` y 20/20 en
`Maqueta Nivel 1`**.

| Bloque | Qué comprueba | Nivel 2 | Nivel 1 |
|---|---|---|---|
| A · NavMesh | Todos los `EnemyAI` con `NavMeshAgent` en `isOnNavMesh`, incluidos los elevados | 2/2 | 2/2 |
| B · VFX | Los dos prefabs de impacto existen, se instancian y se autodestruyen a los 2 s | Sí | Sí |
| C · Derrota | `OnAmbosCaidos` → panel + bloqueo + reinicio completo por `GestorDerrotaHeroe.Reaparecer()` | Sí | Sí |

> **Defecto real detectado por esta batería:** `EnemyAI.ReiniciarEstado()` y
> `EnemyAI.ReanudarNavegacion()` asignaban `Rigidbody.linearVelocity` / `angularVelocity` **antes**
> de poner el cuerpo a cinemático. Como el reinicio llega con el `Rigidbody` ya cinemático, Unity
> emitía dos advertencias por enemigo y por reaparición
> (*"Setting linear/angular velocity of a kinematic body is not supported"*). Ahora ambos métodos
> solo anulan la velocidad cuando el cuerpo **no** es cinemático; tras el arreglo, la corrida de
> Nivel 1 no produjo ninguna advertencia.

> **Nota sobre el reinicio:** el arnés invoca `GestorDerrotaHeroe.Reaparecer()`, que es exactamente
> el método que `Update()` dispara al leer `PlayerInputReader.ReinicioPulsado` (tecla 'R'). La
> lectura de la tecla es una línea ya cubierta por compilación; lo verificado aquí es toda la
> cadena de consecuencias del reinicio.

---

## 10. Convenciones de trabajo

- **No regenerar el blockout** de forma destructiva: la geometría existente es la fuente de verdad.
- **Un único lector de entrada**: `PlayerInputReader`. Ningún otro script toca `Keyboard.current`
  ni `Mouse.current` (los scripts de legado son la única excepción, y están sin montar).
- **Operaciones de editor** en `Assets/Scripts/Editor/AuraOps/`, con `Execute()` idempotente y
  `MarkSceneDirty` al final; nunca se compilan en el build.
- **Materiales**: reutilizar `M_Blockout_*` antes de crear nuevos, para no romper el código cromático.

### 10.1 Invariantes que no deben revertirse

| Invariante | Valor | Motivo |
|---|---|---|
| Trigger de las placas de presión | 0.70 m de alto real | Con menos, PhysX filtra el contacto y el héroe no activa la placa (§3.7) |
| Teleports del héroe | siempre `PlayerController.Reubicar(...)` | Anula la velocidad acumulada del `CharacterController` (§3.6) |
| Micro-offset de las piezas del blockout | +0.02 m | Elimina el Z-fighting sin desplazamiento visible (§3.9) |
| Leash de la IA | 12 m fauna · 10 m huestes · radio de arena minijefes | Evita persecuciones fuera de zona y abuso de kiting (§3.8) |
| Barreras de los lagos de maná | 3.5 m + área *Not Walkable* | Sin ellas el lago es caminable en PhysX y en NavMesh (§3.10) |
| Capa del Player | 8, excluida de `CameraFollow.collisionLayers` | Si se incluye, la cámara retrocede sobre el propio héroe |
| Capa de los enemigos | Default (0) | Es la máscara que usa `PlayerCombat` para aplicar daño |
| Capa del escenario | Environment (9) | Es la máscara de horneado del `NavMeshSurface` y de la cámara |
