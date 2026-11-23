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

## Архитектура (v0.2, работает в игре)
```
BeautifulLooksMod.cs            Mod: сетевые пакеты (SyncLook)
Common/Looks/                   LookSettings (весь образ: сохранение, сеть, пресеты), ColorSetting (HSL + радуга)
Common/Players/LooksPlayer.cs   SaveData/LoadData, пресеты, SyncPlayer/CopyClientState/SendClientChanges, запуск эффектов
Common/Effects/                 AuraEffects, TrailEffects (частицы), LooksDrawLayers (нимб/сияние/круг через PlayerDrawLayer), Fx
Common/UI/                      LooksPanel (окно с вкладками, перетаскивание, превью UICharacter), Elements (кнопки, слайдеры, палитра, сетка предметов)
Common/Systems/                 KeybindSystem (K), LooksUISystem (меню создаётся лениво при первом открытии)
Content/Dusts/                  свои ModDust: сердечки, лепестки, звёзды, снежинки
Content/Items/Wings|Vanity/     базовые классы CosmeticWings / CosmeticHat / CosmeticCape + наследники
Assets/Effects/                 текстуры свечения
tools/make_sprites.py, pngio.py генерация всех PNG на чистом Python (без Pillow)
```

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

## План v0.3 (VFX-версия, ещё не начата в коде)
1. **Своя система частиц** (не Dust): список частиц в ModSystem (обновление в PostUpdateEverything), отрисовка с bloom через `color.A = 0` (аддитивно в AlphaBlend). Типы: светящаяся сфера, 4-лучевая вспышка, растянутая искра, уголёк, дым, кольцо-волна, молния, руны, спрайты (лепестки, сердечки, снежинки, бабочки, светлячки). Текстуры сгенерировать скриптом (гауссово свечение, вспышка, полоса, шум).
2. **Шейдерные ленты** через ванильные `Terraria.Graphics.VertexStrip` + `GameShaders.Misc["RainbowRod" | "MagicMissile" | "FlameLash" | "FinalFractal" | "EmpressBlade"]` (шейдеры Императрицы Света; свои HLSL скомпилировать негде). Обязательно проверить через `TryGetValue`, после отрисовки вызвать `Main.pixelShader.CurrentTechnique.Passes[0].Apply()`. Отрисовка: детур `On_Main.DrawPlayers_AfterProjectiles` (до orig: эффекты позади, после: спереди); вне активного spriteBatch, обернуть в try/catch.
3. **Ауры** (~17): сияние, искры, огоньки-кометы с шейдерными хвостами, небесное кольцо рун, пламя, мороз, сакура, сердечки, вихрь, звездопад, гроза (молнии), пустота, божественный свет, призма (радужные осколки с хвостами RainbowRod), бабочки, руны, светлячки. Ползунки плотности, размера и скорости.
4. **Следы**: шейдерная лента (обычная, радужная, пламенная, Императрицы), призрачные эхо персонажа (DrawPlayer + TransformDrawData с цветом палитры), частицы.
5. **Эффекты на персонаже** (PlayerDrawLayer): стили нимба (кольцо, двойное, корона света, терновый, звезда), сияние, божественные лучи, магический круг-сигил под ногами, пульсирующее кольцо. Опция «краска эффектов»: `DrawData.shader = GameShaders.Armor.GetShaderIdFromItemId(ItemID.XxxDye)`.
6. **Палитра**: основной и второй цвет (+ радуга), готовые темы. **Готовые образы** (Серафим, Инферно, Мороз, Сакура, Пустота, Императрица, Буря…) + 5 слотов пресетов.
7. **Костюмы-робы** (тело + юбка через SetMatch), перекраска шаблонов ExampleMod градиентной картой с узорами (звёзды, лепестки, пламя у подола): Серафим, Инферно, Мороз, Сакура-кимоно, Пустота; королевский комплект (камзол + брюки); капюшон пустоты. Эффект комплекта: частицы, когда роба надета.
8. **UI**: окно шире (~760x500), по умолчанию справа, чтобы персонаж был виден; вкладки Аура / След / Эффекты / Цвета / Образы / Гардероб; выбор стилей сеткой кнопок; своя прокрутка (OverflowHidden + ScrollWheel); общий выключатель эффектов.
