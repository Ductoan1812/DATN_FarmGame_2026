# Unity Architecture Rules

Use the existing 3-layer architecture.

Data and Runtime:
- EntityData is static config.
- EntityRuntime is dynamic state.
- ModuleData configures a module.
- ModuleRuntime owns runtime logic.
- Runtime logic should be scene-independent where practical.
- Save only stable IDs plus dynamic values.

Unity Bridge:
- MonoBehaviours are adapters, lifecycle hooks, animation/input bridges, or scene glue.
- Do not put core gameplay rules in UI or MonoBehaviour if an existing runtime/service should own them.

Services:
- EntityService, InventoryService, ShopService, QuestService, WorldEntityService, TimeManager, registries, and save/load own business rules.
- One clear responsibility per service.
- Avoid duplicate gameplay rules.

State mutation:
- Inventory through InventoryService.
- Shop/money through ShopService.
- Quest through QuestService.
- Time through TimeManager.
- EXP/level through the existing progression API.
- If no API exists, add the smallest focused API.
