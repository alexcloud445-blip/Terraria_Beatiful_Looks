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

## Архитектура (v0.6)
```
BeautifulLooksMod.cs            Mod: сетевые пакеты (SyncLook)
Common/Looks/                   LookSettings (весь образ: сохранение, сеть, пресеты), ColorSetting (HSL + радуга),
                                ColorThemes (12 тем), ReadyLooks (10 образов), EffectDyes (краски для эффектов)
Common/Players/LooksPlayer.cs   SaveData/LoadData, 5 пресетов, история позиций (лента и копии), синхронизация,
                                запуск аур/следов, DrawPlayer (призрачные копии), TransformDrawData (их перекраска)
Common/VFX/                     своя система частиц:
  Particle.cs, Particles.cs       базовые классы и виды частиц (огонёк, вспышка, искра, дым, кольцо, молния,
                                  ShapeParticle — 24 формы атласа Shapes, руна, падающая звезда, орбитальный огонёк)
  Ramp.cs                         Ramp (градиент 2–4 цветов, смешивание по HSV) и Palette (Hi/A/B/Lo из двух цветов образа)
  Shapes.cs                       enum Shape = порядок форм в атласе Shapes.png
  ParticleSystem.cs               ModSystem: список частиц, обновление в PostUpdateEverything, лимит из конфига
  Ribbons.cs                      шейдерные ленты: VertexStrip + GameShaders.Misc (MagicMissile/RainbowRod/FlameLash/EmpressBlade)
  VfxRenderer.cs                  детур On_Main.DrawPlayers_AfterProjectiles: слой «позади» до orig, «спереди» после
  VfxAssets.cs, VfxMath.cs        текстуры Assets/VFX и помощники (Additive = цвет с A = 0)
Common/Effects/                 AuraEffects (17 аур), TrailEffects (частицы следа, ленты игроков, призрачные копии),
                                LooksDrawLayers (BackEffectsLayer: лучи/сияние/сигил/круги/подсветка ауры; HaloLayer: 5 нимбов), Fx,
                                WingEffects (WingRig: скелет плечо→запястье→кончик + 3 ряда перьев; 8 стилей; VfxWingsLayer
                                вместо слоя Wings, который прячется в HideDrawLayers), OutlineEffect (контур = 8 сдвинутых копий),
                                DashEffects (рывок: dashDelay == -1 или скачок скорости X выше бега — ловит и рывки Calamity;
                                вспышка/волна/искры, линии скорости, росчерк-лента, послеобразы; 7 стилей, цвета «Движения»)
  v0.6:                         CelestialAuras (AstroRig — 3 кольца в 3D, точки с Z: Z>=0 рисует FrontEffectsLayer, Z<0 —
                                BackEffectsLayer; сияние, галактика (32 кадра + смешивание), затмение, мандала),
                                CelestialHalos (венец рун, диадема, рога, нимб-затмение; дальняя часть — HaloBackLayer перед Head),
                                StepEffects (шаги/прыжок/приземление), WeaponTrails (GlobalProjectile.PostAI → свой массив путей
                                по whoAmI + identity; ленты из VfxRenderer, без шейдеров — спрайтами), StealthEffects (Calamity
                                через Mod.Call: GetCurrentStealth/GetMaxStealth/GetStealthProjectile; глаз рисуется в VfxRenderer),
                                CompanionEffects (+ CompanionLayer: спутник рисуется относительно центра игрока → виден в превью)
Common/UI/                      LooksPanel (860x520, справа, вкладки Аура/След/Движение/Эффекты/Крылья/Спутник/Цвета/Образы/Гардероб),
                                Elements (кнопки с авто-уменьшением текста, OptionGrid, SwatchButton, LiveSwatch, слайдеры, палитра)
Common/Systems/                 KeybindSystem (K), LooksUISystem (меню создаётся лениво при первом открытии)
Content/Dusts/                  старые ModDust (больше не используются эффектами, оставлены)
Content/Items/Wings|Vanity/     базовые классы CosmeticWings (VfxStyle + цвета → рисуются WingEffects) / CosmeticHat / CosmeticCape /
                                CosmeticRobe (5 роб; RobeGlowLayer после слоя Robe копирует каждый DrawData юбки из текстуры _Glow)
Assets/VFX/                     текстуры эффектов (tools/make_vfx.py + make_vfx_shapes.py): Bloom, Orb, Flare, Glint, Streak,
                                Line, RingWave, GodRay (тонкий луч), Smoke, Runes, Shapes/ShapesGlow (24 формы: заливка + свечение),
                                Feather/Covert(+Glow), ButterflyWing/Pattern/Glow, BatWing/BatBones, Flame (4 кадра), Blade,
                                HaloRing/GroundRing/ThornHalo/SigilRings (уже в перспективе);
                                v0.6 (make_vfx_v6.py): AuroraStrip, Galaxy (32 кадра), Corona, Disc, MandalaOuter/Inner,
                                NebulaWing/Rim/StarsA/B, Fox/FoxGlow
tools/make_robes.py             робы из шаблона ExampleMod (MIT): перекраска по ролям цветов, узор и маска свечения подола
tools/make_icon.py              иконки мода (numpy + Pillow): icon.png 80x80 (обязательно ровно 80x80), icon_small.png 30x30,
                                icon_workshop.png 480x480 — превью в Steam, tML берёт его при Publish сам (в buildIgnore)
tools/sim/                      симуляции на Python: wingsim2 (крылья v2), v6sim (новые крылья/ауры/нимбы), companions_sim
tools/make_sprites.py, pngio.py генерация PNG предметов на чистом Python (без Pillow)
tools/stubcheck/                проверка компиляции без игры (см. ниже)
```

Номера в перечислениях (AuraStyle, TrailStyle, RibbonStyle, EchoStyle, HaloStyle, WingStyle, DashStyle, StepStyle,
WeaponTrailStyle, CompanionStyle, EffectDyes.Items) сохраняются в файл персонажа: новые значения добавлять только В КОНЕЦ.
Пары цветов — LookSettings.ColorPairs (аура, след, движение, эффекты, крылья, спутник): темы, «скопировать на всё» и вкладка
«Цвета» берут их оттуда. Цвета «Движения» (рывок, шаги, след оружия, скрытность) в старых сохранениях = цвета эффектов.

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

## Что ещё не проверено в игре (если упадёт — смотреть сюда)
- Проверено (скриншот v0.3): мод собирается и работает, детур отрисовки, частицы, слои (лучи, сигил, нимб) видны.
- v0.4: крылья (`PlayerDrawSet.cWings`, `Player.wings`, `PlayerDrawLayers.Wings.Hide()` в `HideDrawLayers`,
  слой `Between(Wings, HairBack)`), контур (8 копий через `Main.PlayerRenderer.DrawPlayer`).
- `On_Main.DrawPlayers_AfterProjectiles` (instance-метод, SpriteBatch в этот момент закрыт). Если открыт — VfxRenderer
  просто ничего не рисует (проверка через приватное поле `beginCalled` у FNA SpriteBatch).
- Ленты: `VertexStrip.PrepareStripWithProceduralPadding(..., includeBacksides: true)`, `uWorldViewProjection`
  ставится вручную = `GameViewMatrix.TransformationMatrix * Ortho(viewport)`. Если ленты не видны/съехали — выключить
  в конфиге «Шейдерные ленты» и разбираться.
- Призрачные копии: `Main.PlayerRenderer.DrawPlayer` из `ModPlayer.DrawPlayer`, перекраска в `TransformDrawData`.
- Имена ItemID красок в `EffectDyes`: `ItemID.PrismaticDye` НЕ существует (сборка упала). Остальные прошли компиляцию.
- Все ошибки отрисовки ловятся и пишутся в client.log один раз: строка `Beautiful Looks VFX error (...)`.

## v0.6 — что не проверено в игре (если упадёт — смотреть сюда)
- Слои: `FrontEffectsLayer` = Between(BeetleBuff, EyebrellaCloud), `HaloBackLayer` = BeforeParent(Head),
  `CompanionLayer` = Between(MountBack, Carpet), `RobeGlowLayer` = AfterParent(Robe).
- Робы: `EquipLoader.AddEquipTexture` в Load + SetMatch (как ExampleRobe). Свечение сравнивает `DrawData.texture` с
  `TextureAssets.ArmorLeg[player.legs].Value`. Если подол не светится — смотреть сюда.
- Глаз в скрытности: позиция вычисляется (кадр головы 40x56, глаз ≈ (24, 21), + Main.OffsetsPlayerHeadgear). Может быть сдвинут на 1–2 px.
- Calamity может приглушать DrawDataCache персонажа в скрытности — поэтому глаз рисуется в VfxRenderer, а не слоем.
- «Метательное» оружие: CountsAsClass(DamageClass.Throwing) (разбойник Calamity наследует Throwing) или aiStyle == 2
  (ванильные сюрикены/ножи числятся дальним боем).
- Живые частицы: `Living.Solid` (Main.tile, HasUnactuatedTile, tileSolid/tileSolidTop); выключаются в настройках.

## Грабли, на которые уже наступили
- **tModLoader НЕ предумножает прозрачность PNG** (ImageIO.ToRaw только обнуляет пиксели с alpha = 0), а SpriteBatch
  смешивает как premultiplied. Поэтому PNG эффектов надо сохранять уже premultiplied (RGB = цвет * alpha) —
  это делает `Canvas.save` в tools/make_vfx.py. Иначе мягкое свечение рисуется ровным кругом с резким краем,
  а лучи — «досками» (так было в v0.3 и v0.4 до исправления). Подстраховка: `VfxAssets.Prepare()` при первой
  отрисовке проверяет все текстуры эффектов и при необходимости домножает их в памяти; в client.log строка
  `Beautiful Looks: VFX textures are premultiplied, OK` или `fixed N non-premultiplied VFX textures`.
- **UICharacter нельзя давать настоящего персонажа**: каждый кадр он вызывает у «своего» игрока ResetEffects и
  PlayerFrame и ставит кадры стойки → при открытом меню «замерзала» ходьба и сбрасывались аксессуары. Превью рисует
  копию (`Player.SerializedClone()`), а LooksPanel.SyncPreview каждый кадр переносит в неё образ и взмах крыльев.
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

## Как сделать VFX красивым (выводы после отзыва на v0.3)
Отзыв: «настаканные VFX без градиента выглядят сыро». Поэтому в v0.4:
- **Градиенты везде**: частица = ореол (поздний цвет Ramp) + тело (текущий) + сердцевина (ранний, ближе к белому).
  Цвета смешивать `VfxMath.LerpVivid` (по HSV), не `Color.Lerp` — иначе середина серая.
- **Не пересвечивать**: свечения складываются, белое ядро — только у мелких деталей. У тем второй цвет должен
  заметно отличаться оттенком от первого (не белый).
- **Разнообразие форм**: атлас Shapes (24 формы, у каждой заливка + размытое свечение для второго цвета).
- Проверять вид без игры: `tools/sim/` (симуляция крыльев, повторяет WingRig на Python) помогла поймать
  «крылья торчат вверх» и «взмах в обратную сторону». Меняешь WingEffects — поправь и симуляцию.
- Отзыв на крылья v0.4: «нет глубины, анимации сухие». Крылья v2: длинные узкие перья (Feather) + округлые кроющие
  (Covert); дальнее крыло темнее/холоднее (`Depth`: смесь с pal.Lo), меньше, выше и с запаздыванием; взмах
  несимметричный (вниз 40% цикла), волна от плеча к кончику и изгиб перьев; состояния в LooksPlayer.UpdateWingFlap:
  на земле сложены (Spread), бег — Lean, взлёт/зависание — сильный взмах, планирование — раскрыты, порыв частиц
  в начале каждого удара вниз.
- Отзыв на v0.4.1: «режет глаза». Ореолы частиц слабее и меньше, радуга пастельная (S 0.8, L 0.75), в готовых
  образах 4–5 эффектов, в настройках «Яркость эффектов» (EffectBrightness, по умолчанию 0.8, учитывается в
  VfxMath.Additive).

## План: статус
- v0.3 (проверено в игре): частицы, ленты, ауры, следы, нимбы, образы, меню — работает, но выглядело «сыро».
- v0.4.2 (проверено в игре): градиенты, формы, мягкое свечение — «в разы лучше»; крылья выглядели плоско.
- v0.4.3 (проверено в игре): крылья v2 — «всё выглядит довольно хорошо».
- v0.5 (ждёт проверки): эффекты рывка (7 стилей). Пользователь играет разбойником Calamity: из-за скрытности персонаж
  полупрозрачный — это механика Calamity, не баг мода; послеобразы рывка перекрашиваются целиком и видны и в скрытности.
- v0.5.1: исправлена «замерзающая» ходьба при открытом меню (превью = SerializedClone).
- v0.6 (ждёт проверки): все 19 идей «для красоты»: 5 аур (Астролябия, Северное сияние, Галактика, Затмение, Мандала),
  4 крыла (Феникс, Аврора, Туманность, Цветы), 4 нимба (венец рун, диадема, рога, затмение), вкладка «Движение»
  (рывок, шаги/прыжок/приземление, след брошенного оружия, скрытность Calamity), живые частицы, 5 роб со светящимся
  подолом, вкладка «Спутник» (огонёк, лисица, бабочка, змей), 6 новых готовых образов. Вид проверен симуляциями tools/sim.

Идеи на будущее: обмен образами кодом, публикация в Steam Workshop, полные костюмы с ручными спрайтами,
свечение узоров на теле робы (тело в 1.4 рисуется составными частями — сложнее, чем юбка).

Осталось:
- **Проверить v0.5–v0.6 в игре** и поправить то, что всплывёт.
- Робы сделаны (v0.6). Шаблоны ExampleMod: `ExampleMod/Content/Items/Armor/Vanity/ExampleRobe*.png` в клоне tML.
