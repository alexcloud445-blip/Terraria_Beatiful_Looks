# Beautiful Looks — контекст для Claude

Косметический мод для Terraria (tModLoader 1.4.4+, сборка tML 2026.9): крылья, ауры, следы, эффекты, костюмы.
Только красота, без влияния на баланс.

## Пользователь
- Общаться **по-русски**, просто и по шагам. Пользователь новичок в программировании, но сам собирает мод и присылает логи и скриншоты.
- Приоритет пользователя: **уровень VFX как в Wrath of the Gods / Daybreak**, широкий выбор эффектов и кастомизации, красивые костюмы.

## Как собирается и тестируется
- Собирает пользователь на Windows-ПК: папка `Документы\My Games\Terraria\tModLoader\ModSources\Terraria_Beatiful_Looks` → в игре **Workshop → Develop Mods → Build + Reload**.
- **Имя папки обязано совпадать с пространством имён `Terraria_Beatiful_Looks`.** ZIP с GitHub распаковывается как `...-main`, и мод падает с ошибкой «Пространство имён и название папки не совпадают».
- В папке мода не должно быть посторонних модов: tModLoader компилирует всё, что лежит внутри.
- Лог сборки и ошибок: `C:\Program Files (x86)\Steam\steamapps\common\tModLoader\tModLoader-Logs\client.log`.
- У пользователя также стоят Calamity, MagicStorage, RecipeBrowser, BossChecklist.

## Архитектура (v0.3, компилируется в игре, работа эффектов ещё не проверена)
```
BeautifulLooksMod.cs            Mod: сетевые пакеты (SyncLook)
Common/Looks/                   LookSettings (весь образ: сохранение, сеть, пресеты), ColorSetting (HSL + радуга),
                                ColorThemes (12 тем), ReadyLooks (10 образов), EffectDyes (краски для эффектов)
Common/Players/LooksPlayer.cs   SaveData/LoadData, 5 пресетов, история позиций (лента и копии), синхронизация,
                                запуск аур/следов, DrawPlayer (призрачные копии), TransformDrawData (их перекраска)
Common/VFX/                     своя система частиц:
  Particle.cs, Particles.cs       базовые классы и виды частиц (огонёк, вспышка, искра, дым, кольцо, молния,
                                  спрайт, руна, падающая звезда, орбитальный огонёк с хвостом)
  ParticleSystem.cs               ModSystem: список частиц, обновление в PostUpdateEverything, лимит из конфига
  Ribbons.cs                      шейдерные ленты: VertexStrip + GameShaders.Misc (MagicMissile/RainbowRod/FlameLash/EmpressBlade)
  VfxRenderer.cs                  детур On_Main.DrawPlayers_AfterProjectiles: слой «позади» до orig, «спереди» после
  VfxAssets.cs, VfxMath.cs        текстуры Assets/VFX и помощники (Additive = цвет с A = 0)
Common/Effects/                 AuraEffects (17 аур), TrailEffects (частицы следа, ленты игроков, призрачные копии),
                                LooksDrawLayers (BackEffectsLayer: лучи/сияние/сигил/круги/подсветка ауры; HaloLayer: 5 нимбов), Fx
Common/UI/                      LooksPanel (760x500, справа, вкладки Аура/След/Эффекты/Цвета/Образы/Гардероб),
                                Elements (кнопки с авто-уменьшением текста, OptionGrid, SwatchButton, LiveSwatch, слайдеры, палитра)
Common/Systems/                 KeybindSystem (K), LooksUISystem (меню создаётся лениво при первом открытии)
Content/Dusts/                  старые ModDust (больше не используются эффектами, оставлены)
Content/Items/Wings|Vanity/     базовые классы CosmeticWings / CosmeticHat / CosmeticCape + наследники
Assets/VFX/                     текстуры эффектов (tools/make_vfx.py): Bloom, Orb, Flare, Streak, Line, RingWave, GodRay,
                                Smoke, Sprites (8 спрайтов), Runes, HaloRing/GroundRing/ThornHalo/SigilRings (уже в перспективе)
tools/make_sprites.py, pngio.py генерация PNG предметов на чистом Python (без Pillow)
tools/stubcheck/                проверка компиляции без игры (см. ниже)
```

Номера в перечислениях (AuraStyle, TrailStyle, RibbonStyle, EchoStyle, HaloStyle, EffectDyes.Items) сохраняются
в файл персонажа: новые значения добавлять только В КОНЕЦ.

## Проверка сборки без игры (tools/stubcheck)
Скачать сам tModLoader в облачной сессии не удалось (github.com закрыт прокси для чужих репозиториев,
releases недоступны). Поэтому код проверяется против заглушек API: `tools/stubcheck/*.cs.txt`
(лежат как .txt, иначе tModLoader скомпилирует их вместе с модом!) + настоящие XNA DLL из репозитория tModLoader.
```
apt-get install -y dotnet-sdk-8.0
GIT_LFS_SKIP_SMUDGE=1 git clone --depth 1 -b stable https://github.com/tModLoader/tModLoader /home/user/tmodloader/tmodloader
tools/stubcheck/check.sh        # должно быть "0 Error(s)"
```
Заглушки ловят опечатки и нестыковки внутри мода, но API игры в них написан по памяти и исходникам tML:
если что-то используется впервые — добавить в заглушку, сверившись с `patches/tModLoader/...` в клоне tML.

## Что в v0.3 не проверено в игре (если упадёт — смотреть сюда)
- `On_Main.DrawPlayers_AfterProjectiles` (instance-метод, SpriteBatch в этот момент закрыт). Если открыт — VfxRenderer
  просто ничего не рисует (проверка через приватное поле `beginCalled` у FNA SpriteBatch).
- Ленты: `VertexStrip.PrepareStripWithProceduralPadding(..., includeBacksides: true)`, `uWorldViewProjection`
  ставится вручную = `GameViewMatrix.TransformationMatrix * Ortho(viewport)`. Если ленты не видны/съехали — выключить
  в конфиге «Шейдерные ленты» и разбираться.
- Призрачные копии: `Main.PlayerRenderer.DrawPlayer` из `ModPlayer.DrawPlayer`, перекраска в `TransformDrawData`.
- Имена ItemID красок в `EffectDyes`: `ItemID.PrismaticDye` НЕ существует (сборка упала). Остальные прошли компиляцию.
- Все ошибки отрисовки ловятся и пишутся в client.log один раз: строка `Beautiful Looks VFX error (...)`.

## Грабли, на которые уже наступили
- **Нельзя трогать `Main.LocalPlayer` / `GetModPlayer` в конструкторах UI**: при загрузке мода ModPlayer ещё не зарегистрированы (IndexOutOfRangeException). Меню создаётся при первом открытии, тексты кнопок берутся в Update.
- Шрифт Terraria не поддерживает символы ✦ ✔ ★: использовать ASCII.
- `[AutoloadEquip]` ставить на **каждый конкретный** класс, а не на абстрактный базовый.
- Нужен `using Terraria.DataStructures;` для `WingStats`.

## Проверенные факты об API (по исходникам tModLoader, ветка stable)
- `ModPlayer.DrawPlayer(Camera camera)`: рисовать копии игрока через `Main.PlayerRenderer.DrawPlayer(camera, Player, pos, Player.fullRotation, Player.fullRotationOrigin, shadow)`.
- `ModPlayer.TransformDrawData(ref PlayerDrawSet drawInfo)`: перекрасить DrawDataCache (тонировка копий).
- `Player.GetAdvancedShadow(i)` / `Player.availableAdvancedShadowsCount`: до 60 прошлых позиций для послеобразов.
- Флаги `player.armorEffectDrawShadow`, `armorEffectDrawOutlines`, `armorEffectDrawOutlinesForbidden`.
- Роба: `[AutoloadEquip(EquipType.Body)]` + в `Load()` вызвать `EquipLoader.AddEquipTexture(Mod, $"{Texture}_{EquipType.Legs}", EquipType.Legs, this)` (не на сервере) + `SetMatch(bool male, ref int equipSlot, ref bool robes)` с `robes = true; equipSlot = EquipLoader.GetEquipSlot(Mod, Name, EquipType.Legs)`; `ArmorIDs.Body.Sets.HidesHands[Item.bodySlot] = false`.
- Лист тела 1.4: **360x224 = сетка 9x4 кадров 40x56** (торс, плечи, руки в разных позах). Ноги, голова и спина: 40x1120 (20 кадров вертикально). Шаблоны есть в ExampleMod (`Content/Items/Armor/ExampleBreastplate_Body.png`, `Vanity/ExampleRobe_Body.png`, `Vanity/ExampleRobe_Legs.png`, лицензия MIT). Цвета шаблона робы: контур (50,50,50)/(52,52,35), ткань 4 оттенка (110,96,79)…(197,183,166), золотая отделка (184,151,52)/(201,191,80)/(215,208,125). Шаблоны нагрудника, штанов и капюшона: красные рампы по яркости.

## План v0.3 (VFX-версия): статус
Сделано в коде (пункты 1–6 и 8 исходного плана): своя система частиц с bloom, шейдерные ленты, 17 аур, следы
(лента, частицы, призрачные копии), 5 нимбов + сияние/лучи/сигил/круги/пульсация + краски, по два цвета на часть +
12 тем, 10 готовых образов, 5 пресетов, новое меню 760x500 с 6 вкладками и общим выключателем.
Прокрутка в меню пока не понадобилась: всё помещается.

Осталось:
- **Проверить в игре** (Build + Reload, client.log) и поправить то, что всплывёт.
- **Пункт 7: костюмы-робы** (тело + юбка через SetMatch), перекраска шаблонов ExampleMod градиентной картой с узорами:
  Серафим, Инферно, Мороз, Сакура-кимоно, Пустота; королевский комплект; капюшон пустоты; частицы комплекта.
  Шаблоны ExampleMod есть в клоне tML: `ExampleMod/Content/Items/Armor/...`, `ExampleMod/Content/Items/Vanity/...`.
- Крылья можно перевести на новые эффекты (сейчас они сыплют частицы через Fx → уже новая система).
