/*
 * OfflineCoopFix.cs — Fix Helper for OfflineCoop v1.9
 *
 * Дополнительный скрипт, решающий баги оригинального v1.9:
 *   • Проваливание P2 сквозь текстуры (принудительная загрузка коллизий)
 *   • Удаление P2 движком игры (флаг IsPersistent + BlockPermanentEvents)
 *   • Автовоскрешение P2 при смерти
 *   • Автотелепорт P2 к P1 при десинхронизации (>200 м или провал под карту)
 *   • Приоритет процесса ВЫШЕ СРЕДНЕГО при старте (фикс подгрузки текстур)
 *   • Безопасное логирование в scripts/OfflineCoop/fixhelper.log
 *   • Отдача всех трёх стартовых пушек P2 сразу при спавне
 *
 * Кладётся как ЕСТЬ в папку scripts/. ScriptHookVDotNet3 компилирует
 * .cs файлы на лету при запуске игры — отдельная компиляция не нужна.
 */

using System;
using System.IO;
using System.Windows.Forms;
using GTA;
using GTA.Native;
using GTA.UI;

public class OfflineCoopFix : Script
{
    private Ped _p2;
    private int _tickCount;
    private static readonly string LogDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "scripts", "OfflineCoop");

    public OfflineCoopFix()
    {
        try { Directory.CreateDirectory(LogDir); } catch { }
        Tick += OnTick;
        KeyDown += OnKeyDown;
        Interval = 350;

        // Ставим приоритет процесса выше обычного — помогает с подгрузкой
        // текстур и коллизий при сплит-скрине на 144Hz
        try
        {
            using var proc = System.Diagnostics.Process.GetCurrentProcess();
            proc.PriorityClass = System.Diagnostics.ProcessPriorityClass.AboveNormal;
        }
        catch { }

        Notification.Show("~g~OfflineCoop Fix Helper загружен~w~");
        Log("Fix helper started");
    }

    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        // F6 — ручной телепорт P2 к P1
        if (e.KeyCode == Keys.F6) { ForceTeleportP2(); Notification.Show("~b~P2 телепортирован к тебе"); }
    }

    private Ped FindP2Ped()
    {
        var p1 = Game.Player.Character;
        Ped best = null;
        float bestDist = float.MaxValue;
        foreach (var ped in World.GetAllPeds())
        {
            if (ped == null || !ped.Exists() || ped == p1 || ped.IsPlayer) continue;
            if (!ped.IsPersistent) continue;
            float d = ped.Position.DistanceTo(p1.Position);
            if (d < 200f && d < bestDist)
            {
                bestDist = d;
                best = ped;
            }
        }
        return best;
    }

    private void OnTick(object sender, EventArgs e)
    {
        _tickCount++;
        try
        {
            _p2 = FindP2Ped();
            if (_p2 == null || !_p2.Exists()) return;

            // --- Физика / коллизии (не проваливается) ---
            Function.Call(Hash.REQUEST_COLLISION_AT_COORD,
                _p2.Position.X, _p2.Position.Y, _p2.Position.Z);
            Function.Call(Hash.SET_ENTITY_LOAD_COLLISION_FLAG, _p2, true, 1);
            Function.Call(Hash.SET_ENTITY_HAS_GRAVITY, _p2, true);
            _p2.IsPersistent = true;
            _p2.BlockPermanentEvents = true;
            _p2.CanBeDraggedOutOfVehicle = false;
            Function.Call(Hash.SET_PED_CAN_BE_DRAGGED_OUT, _p2, false);
            Function.Call(Hash.SET_PED_DIES_INSTANTLY_IN_WATER, _p2, false);
            Function.Call(Hash.SET_PED_SUFFERS_CRITICAL_HITS, _p2, false);
            _p2.CanSufferCriticalHits = false;

            // --- Выдаём оружие если не хватает ---
            if (_tickCount % 20 == 0)
            {
                if (!_p2.Weapons.HasWeapon(WeaponHash.Pistol))
                    _p2.Weapons.Give(WeaponHash.Pistol, 500, true, true);
                if (!_p2.Weapons.HasWeapon(WeaponHash.CarbineRifle))
                    _p2.Weapons.Give(WeaponHash.CarbineRifle, 500, true, false);
                if (!_p2.Weapons.HasWeapon(WeaponHash.Knife))
                    _p2.Weapons.Give(WeaponHash.Knife, 1, true, false);
                _p2.Weapons.Current.InfiniteAmmo = true;
                _p2.Armor = 100;
            }

            // --- Воскрешение при смерти ---
            if (_p2.IsDead)
            {
                Wait(1200);
                if (_p2.Exists())
                {
                    _p2.Resurrect();
                    _p2.Health = _p2.MaxHealth;
                    _p2.Armor = 100;
                    var p1 = Game.Player.Character;
                    _p2.Position = p1.Position + p1.ForwardVector * 2.0f;
                    Function.Call(Hash.REQUEST_COLLISION_AT_COORD,
                        _p2.Position.X, _p2.Position.Y, _p2.Position.Z);
                    Notification.Show("~y~P2 воскрешён");
                    Log("P2 resurrected");
                }
                return;
            }

            // --- Автотелепорт при десинке / провале под карту ---
            var p1pos = Game.Player.Character.Position;
            float dist = _p2.Position.DistanceTo(p1pos);
            if (_p2.Position.Z < -50f || (!_p2.IsInVehicle() && dist > 280f)
                || (_p2.IsRagdoll && _p2.Velocity.Length() > 80f))
            {
                ForceTeleportP2();
                Log($"Auto-teleport (z={_p2.Position.Z},dist={dist:F0})");
            }

            // --- Если P2 в машине — сделать так чтобы двери были открыты ---
            if (_p2.IsInVehicle())
            {
                var v = _p2.CurrentVehicle;
                if (v != null && v.Exists())
                    Function.Call(Hash.SET_VEHICLE_DOORS_LOCKED, v, 2); // unlocked
            }
        }
        catch (Exception ex) { Log("Tick error: " + ex.Message); }
    }

    private void ForceTeleportP2()
    {
        if (_p2 == null || !_p2.Exists()) return;
        var p1 = Game.Player.Character;
        if (_p2.IsInVehicle()) { try { _p2.Task.LeaveVehicle(); Wait(200); } catch { } }
        _p2.Position = p1.GetOffsetPosition(new Math.Vector3(0f, 2.6f, 0.4f));
        Function.Call(Hash.SET_ENTITY_COORDS, _p2,
            _p2.Position.X, _p2.Position.Y, _p2.Position.Z,
            false, false, false, false);
        Function.Call(Hash.REQUEST_COLLISION_AT_COORD,
            _p2.Position.X, _p2.Position.Y, _p2.Position.Z);
        Function.Call(Hash.SET_PED_TO_RAGDOLL, _p2, 0, 0, 0, false, false, false);
        Notification.Show("~g~P2 телепортирован к P1");
    }

    private static void Log(string msg)
    {
        try { File.AppendAllText(Path.Combine(LogDir, "fixhelper.log"),
            $"[{DateTime.Now:HH:mm:ss}] {msg}\n"); }
        catch { }
    }
}
