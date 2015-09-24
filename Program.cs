using System;

using LeagueSharp;
using LeagueSharp.Common;

using Color = System.Drawing.Color;

namespace Kalista
{
    using System.Linq;

    public class Program
    {
        public const string CHAMP_NAME = "Kalista";
        private static Obj_AI_Hero player = ObjectManager.Player;

        public static void Main(string[] args)
        {
            // Clear console from previous errors
            Utils.ClearConsole();

            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            // Validate Champion
            if (player.ChampionName != CHAMP_NAME)
                return;

            // Initialize classes
            SpellQueue.Initialize();
            SoulBoundSaver.Initialize();

            // Enable damage indicators
            Utility.HpBarDamageIndicator.DamageToUnit = Damages.GetTotalDamage;
            Utility.HpBarDamageIndicator.Enabled = true;

            // Enable E damage indicators
            CustomDamageIndicator.Initialize(Damages.GetActualDamage);

            // Listen to additional events
            Game.OnUpdate += Game_OnGameUpdate;
            Spellbook.OnCastSpell += Spellbook_OnCastSpell;
            Drawing.OnDraw += Drawing_OnDraw;
            Obj_AI_Base.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
            CustomEvents.Unit.OnDash += Unit_OnDash;
            Orbwalking.AfterAttack += ActiveModes.Orbwalking_AfterAttack;
            Orbwalking.OnNonKillableMinion += Orbwalking_OnNonKillableMinion;
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            // Permanent checks for something like killsteal
            ActiveModes.OnPermaActive();

            if (SpellQueue.IsReady)
            {
                if (Config.KeyLinks["comboActive"].Value.Active)
                    ActiveModes.OnCombo();
                if (Config.KeyLinks["harassActive"].Value.Active)
                    ActiveModes.OnHarass();
                if (Config.KeyLinks["waveActive"].Value.Active)
                    ActiveModes.OnWaveClear();
            }
            if (Config.KeyLinks["fleeActive"].Value.Active)
                ActiveModes.OnFlee();
            else
                ActiveModes.fleeTargetPosition = null;

            if (Config.BoolLinks["miscEBeforeDeath"].Value)
            {
                if (OnUpdateFunctions.EDeath()) return;
            }

            OnUpdateFunctions.HandleSentinels();
            OnUpdateFunctions.JungleSteal();
        }

        private static void Unit_OnDash(Obj_AI_Base sender, Dash.DashItem args)
        {
            if (sender.IsMe)
            {
                ActiveModes.wallJumpInitTime = null;
                ActiveModes.wallJumpTarget = null;
            }
        }

        private static void Orbwalking_OnNonKillableMinion(AttackableUnit minion)
        {
            if (Config.BoolLinks["miscAutoE"].Value && SpellManager.E.IsReady())
            {
                var target = minion as Obj_AI_Base;
                if (target != null && target.Health <= SpellManager.E.GetDamage(target))
                {
                    // Cast since it's killable with E
                    SpellManager.E.Cast();
                    SpellManager.E.LastCastAttemptT = Environment.TickCount;
                }
            }
        }

        private static void Obj_AI_Base_OnProcessSpellCast(Obj_AI_Base sender, GameObjectProcessSpellCastEventArgs args)
        {
            if (sender.IsMe)
            {
                // Rend
                if (args.SData.Name == "KalistaExpungeWrapper")
                {
                    // Make the orbwalker attack again, might get stuck after casting E
                    Utility.DelayAction.Add(250, Orbwalking.ResetAutoAttackTimer);
                }
            }
        }

        private static void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            // Avoid stupid Q casts while jumping in mid air!
            if (sender.Owner.IsMe && args.Slot == SpellSlot.Q && player.IsDashing())
            {
                // Don't process the packet since we are jumping!
                args.Process = false;
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            // All circles
            foreach (var entry in Config.CircleLinks)
            {
                if (entry.Value.Value.Active && entry.Key != "drawDamageE")
                    Render.Circle.DrawCircle(player.Position, entry.Value.Value.Radius, entry.Value.Value.Color);
            }

            // E damage on healthbar
            CustomDamageIndicator.DrawingColor = Config.CircleLinks["drawDamageE"].Value.Color;
            CustomDamageIndicator.Enabled = Config.CircleLinks["drawDamageE"].Value.Active;

            // Flee position the player moves to
            if (ActiveModes.fleeTargetPosition.HasValue)
                Render.Circle.DrawCircle(ActiveModes.fleeTargetPosition.Value, 50, ActiveModes.wallJumpPossible ? Color.Green : SpellManager.Q.IsReady() ? Color.Red : Color.Teal, 10);

            foreach (var source in HeroManager.Enemies.Where(x => player.Distance(x) <= 2000f && !x.IsDead))
            {
                var stacks = source.GetBuffCount("kalistaexpungemarker");

                if (stacks > 0)
                {
                    if (Config.BoolLinks["drawStacksE"].Value)
                    {
                        Drawing.DrawText(
                            Drawing.WorldToScreen(source.Position)[0] - 80,
                            Drawing.WorldToScreen(source.Position)[1],
                            Color.White,
                            "Stacks: " + stacks);
                    }
                }

                if (Config.BoolLinks["drawProcentE"].Value)
                {
                    var currentPercentage =
                        Math.Ceiling(Damages.GetActualDamage(source) * 100 / source.Health);

                    Drawing.DrawText(
                        Drawing.WorldToScreen(source.Position)[0],
                        Drawing.WorldToScreen(source.Position)[1],
                        currentPercentage >= (100) ? Color.DarkRed : Color.White,
                        currentPercentage >= (100) ? "Killable With E" : + currentPercentage + "%");
                }
            }

            // Remaining time for E stacks and
        }
    }
}
