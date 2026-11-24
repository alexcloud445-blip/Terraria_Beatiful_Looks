# Симуляция крыльев без игры

Скрипты повторяют расчёт крыльев из `Common/Effects/WingEffects.cs` на чистом Python
и рисуют кадры в PNG — так видно форму и анимацию, не запуская Terraria.

    python3 tools/sim/wingsim2.py /tmp/seraph.png                 # серафим: полёт, планирование, покой
    python3 -c "import sys; sys.path.insert(0,'tools/sim'); import wingsim2; wingsim2.all_styles('/tmp/styles.png')"

При изменении WingRig / WingEffects нужно поправить и симуляцию, иначе она начнёт врать.
Текстуры читаются из Assets/VFX и считаются premultiplied (как после make_vfx.py).
