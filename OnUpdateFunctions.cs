using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LeagueSharp;
using LeagueSharp.Common;

namespace Kalista
{
    class OnUpdateFunctions
    {
        internal class Time
        {
            private static readonly DateTime AssemblyLoadTime = DateTime.Now;
            public static float LastRendTick { get; set; }
            public static float LastNonKillable { get; set; }

            public static float TickCount
            {
                get
                {
                    return (int)DateTime.Now.Subtract(AssemblyLoadTime).TotalMilliseconds;
                }
            }

            public static bool CheckRendDelay()
            {
                return !(TickCount - LastRendTick < 750);
            }

            public static bool CheckNonKillable()
            {
                return !(TickCount - LastNonKillable < 2000);
            }
        }

        public static void HandleSentinels()
        {
            if (!SpellManager.W.IsReady())
            {
                return;
            }

            if (Config.KeyLinks["sentinelBaron"].Value.Active && ObjectManager.Player.Distance(SummonersRift.River.Baron) <= SpellManager.W.Range)
            {
                SpellManager.W.Cast(SummonersRift.River.Baron);
            }
            else if (Config.KeyLinks["sentinelDrake"].Value.Active && ObjectManager.Player.Distance(SummonersRift.River.Dragon) <= SpellManager.W.Range)
            {
                SpellManager.W.Cast(SummonersRift.River.Dragon);
            }
        }

        public static void JungleSteal()
        {
            if (Config.BoolLinks["jungleSteal"].Value && SpellManager.E.IsReady())
            {
                var normalMob =
                    MinionManager.GetMinions(
                        ObjectManager.Player.ServerPosition,
                        SpellManager.E.Range,
                        MinionTypes.All,
                        MinionTeam.Neutral,
                        MinionOrderTypes.MaxHealth)
                        .FirstOrDefault(
                            x =>
                            x.IsValid && x.Health < Damages.GetActualDamage(x) && !x.Name.Contains("Mini")
                            && !x.Name.Contains("Dragon") && !x.Name.Contains("Baron"));

                var superMinion =
                    MinionManager.GetMinions(
                        ObjectManager.Player.ServerPosition,
                        SpellManager.E.Range,
                        MinionTypes.All,
                        MinionTeam.Enemy,
                        MinionOrderTypes.MaxHealth)
                        .FirstOrDefault(
                            x =>
                            x.IsValid && x.Health <= Damages.GetActualDamage(x)
                            && x.SkinName.ToLower().Contains("super"));

                var baron =
                    MinionManager.GetMinions(
                        ObjectManager.Player.ServerPosition,
                        SpellManager.E.Range,
                        MinionTypes.All,
                        MinionTeam.Neutral,
                        MinionOrderTypes.MaxHealth)
                        .FirstOrDefault(
                            x => x.IsValid && x.Health < Damages.GetBaronReduction(x) && x.Name.Contains("Baron"));

                var dragon =
                    MinionManager.GetMinions(
                        ObjectManager.Player.ServerPosition,
                        SpellManager.E.Range,
                        MinionTypes.All,
                        MinionTeam.Neutral,
                        MinionOrderTypes.MaxHealth)
                        .FirstOrDefault(
                            x => x.IsValid && x.Health < Damages.GetDragonReduction(x) && x.Name.Contains("Dragon"));

                if ((normalMob != null && SpellManager.E.CanCast(normalMob))
                    || (superMinion != null && SpellManager.E.CanCast(superMinion))
                    || (baron != null && SpellManager.E.CanCast(baron))
                    || (dragon != null && SpellManager.E.CanCast(dragon)))
                {
                    SpellManager.E.Cast();
                    SpellManager.E.LastCastAttemptT = Environment.TickCount;
                }
            }
        }

        public static bool EDeath()
        {
            if (!Time.CheckRendDelay()) return false;

            var champs = 0;
            foreach (var target in HeroManager.Enemies)
            {
                if (!target.IsValid) continue;
                if (!target.IsValidTarget(1000)) continue;
                if (!target.HasBuff("KalistaExpungeMarker")) continue;
                if (!Time.CheckRendDelay()) continue;
                if (ObjectManager.Player.HealthPercent > Config.SliderLinks["miscEBeforeDeathMaxHP"].Value.Value) continue;
                if (target.GetBuffCount("kalistaexpungemarker") < Config.SliderLinks["miscEBeforeDeathStacks"].Value.Value) continue;
                champs++;
                if (champs < Config.SliderLinks["miscEBeforeDeathChamps"].Value.Value) continue;

                UseRend();
                return true;
            }
            return false;
        }

        public static void UseRend()
        {
            SpellManager.E.Cast();
            SpellManager.E.LastCastAttemptT = Environment.TickCount;

            Time.LastRendTick = Time.TickCount;
        }
    }
}
