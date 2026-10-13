@echo off
chcp 65001 >nul
title Offline Coop Fix — Помощник настроек перед первым запуском
color 0A

echo.
echo  ╔══════════════════════════════════════════════════════════╗
echo  ║    Offline Coop v1.9 FIX — ПОМОЩНИК ПО НАСТРОЙКЕ        ║
echo  ╚══════════════════════════════════════════════════════════╝
echo.
echo  Скрипт НИЧЕГО не устанавливает и не ломает, он только
echo  отключает 2 вещи, которые вызывают МЕРЦАНИЕ в GTA V:
echo    1. Полноэкранную оптимизацию Windows (главная причина)
echo    2. DPI-виртуализацию (из-за неё драйвер видит не то разрешение)
echo.

set /p GTAPATH="→ Перетащи сюда файл GTA5.exe мышкой и нажми Enter: "

set GTAPATH=%GTAPATH:"=%
if not exist "%GTAPATH%" (
    echo.
    echo   ❌ ФАЙЛ НЕ НАЙДЕН! Попробуй ещё раз — просто перетащи GTA5.exe
    echo      прямо в это чёрное окно.
    pause
    exit /b 1
)

echo.
echo   Найден: %GTAPATH%
echo.
echo   Применяю фиксы совместимости...

:: Отключаем полноэкранную оптимизацию + отключаем DPI-скейлинг для GTA5.exe
reg add "HKCU\Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers" ^
    /v "%GTAPATH%" /t REG_SZ ^
    /d "~ DISABLEDXMAXIMIZEDWINDOWEDMODE HIGHDPIAWARE" /f >nul 2>&1

if %errorlevel%==0 (
    echo   ✅ Полноэкранная оптимизация отключена.
    echo   ✅ DPI-масштабирование отключено.
) else (
    echo   ⚠ Не удалось записать в реестр — запусти bat-файл от Администратора.
)

echo.
echo  ╔══════════════════════════════════════════════════════════╗
echo  ║   ЧТО СДЕЛАТЬ ПЕРЕД ПЕРВЫМ ЗАПУСКОМ ИГРЫ:               ║
echo  ╠══════════════════════════════════════════════════════════╣
echo  ║ 1. Убедись что установлено:                              ║
echo  ║     • Script Hook V       (лежит в корне GTA V)          ║
echo  ║     • ScriptHookVDotNet3  (ScriptHookVDotNet3.dll и      ║
echo  ║                            ScriptHookVDotNet2.dll — тоже ║
echo  ║                            в корне GTA V)                ║
echo  ║     • NativeUI.dll в папке scripts/                      ║
echo  ║                                                          ║
echo  ║ 2. Зайди в GTA V → Настройки → Графика:                  ║
echo  ║     • Тип экрана: ОКОННЫЙ БЕЗ РАМКИ                      ║
echo  ║     • Вертикальная синхронизация: ВКЛ (НЕ Half!)         ║
echo  ║     • DirectX: 11                                        ║
echo  ║     • MSAA: ОТКЛ или x2 (НЕ x4/x8)                       ║
echo  ║     • TXAA / Nvidia Reflex: ОТКЛ                         ║
echo  ║     • Расстояние прорисовки: не выше 70                  ║
echo  ║     • Частота обновления: как у монитора                 ║
echo  ║                                                          ║
echo  ║ 3. В панели NVIDIA (если карта nvidia) для GTA5.exe:      ║
echo  ║     • Вертикальный синхроимпульс: ВКЛ                    ║
echo  ║     • Тройная буферизация: ВКЛ                           ║
echo  ║     • Режим низкой задержки: ВЫКЛ                        ║
echo  ║     • Предпочт. режим макс. производительности           ║
echo  ║                                                          ║
echo  ║ 4. ОБЯЗАТЕЛЬНО выключи ReShade/ENB/NVE/VTIC — они         ║
echo  ║    перехватывают картинку и вызывают мерцание.            ║
echo  ║                                                          ║
echo  ║ 5. Перед запуском нажми NumLock на клавиатуре             ║
echo  ║    (чтобы работали клавиши нумпада для телепорта).        ║
echo  ╚══════════════════════════════════════════════════════════╝
echo.
echo   🎮 В ИГРЕ:
echo      N        — заспавнить 2 игрока
echo      K        — включить сплит-скрин
echo      PageUp   — меню выбора модели
echo      Numpad-  — телепорт P2 к тебе
echo      F6       — (фикспак) принудительный телепорт P2
echo      Insert   — перезагрузить скрипты если что-то зависло
echo.
pause
