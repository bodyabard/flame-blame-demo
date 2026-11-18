# Фаза 1 — Каркас, рукав, струмінь, вогонь (greybox)

Прочитай `CLAUDE.md` і дотримуйся його. Поточна дата 18.11.2026, дедлайн 27.11.

У `Assets/_Project/Scripts/_Prototype/` лежать 4 файли першого прототипу (`Interfaces.cs`, `PlayerController.cs`, `HoseSystem.cs`, `NozzleJet.cs`). Вони працюють як ідея, але написані монолітно. Мета фази: **перенести їх у правильну архітектуру, зберігши поведінку й відчуття**, і додати систему вогню. Візуал поки на білбоксах.

## Завдання
1. Створи структуру папок, namespaces і `GameBootstrap` (композиційний корінь) + `IGameEvents` / `GameEvents` (Observer).
2. **Рефактор Hose** на окремі відповідальності: `HoseSimulation` (чистий Verlet, без Unity-рендера, легко тестується), `HoseRenderer` (LineRenderer/меш), `HosePressure` (`IPressureSource`), `HoseHoldState` (State: Held/Dropped), `IHoseInput` (+ реалізація на старому Input). `NozzleJet` розділити на `JetEmitter` (частинки, з пулом) і `JetHitScanner` (SphereCast → `IWaterReceiver`).
3. `PlayerController` розділи на рух, погляд і `CameraFeedback` (тряска/кік). Віддача й тряска йдуть подією або інтерфейсом, а не прямими викликами.
4. **Вогонь:** `FireMaterialProfile` (ScriptableObject: Wood, Fabric, Paper, Metal), `Flammable` (стан нагріву, горіння, обвуглення — змінює колір матеріалу через MaterialPropertyBlock), `FireNode`-реєстрація в `FireSystem` (центральний тик, поширення на сусідів у радіусі, обмеження кількості активних вогнів). Горить → викликає події Observer. Реалізує `IWaterReceiver`: вода знижує тепло, при нулі гасить, створює подію для пари/калюжі.
5. Greybox-сцена `Scenes/Main.unity` будується Editor-скриптом `Tools/FlameBlame/Build Greybox`: підлога, стіни, диван, 2 полиці, штора, гідрант, дві зони з різними матеріалами. Шари: Player, Hose, Interactable.
6. Один EditMode-тест-файл для логіки поширення вогню й формули тиску (3–5 тестів, без зайвого).
7. Створи/онови `ASSETS.md`, `AI_LOG.md`.

## Вихід
Коміт, резюме, блок «ЗРОБИ ВРУЧНУ» (мінімум: що перетягнути/призначити в інспекторі, якщо Editor-скрипт не зміг).

▶ **ВІХА 1.** Зупинись і попроси мене зіграти: рукав, віддача, кидок брандспойта, загоряння й гасіння на білбоксах. Далі лише після мого фідбеку.
