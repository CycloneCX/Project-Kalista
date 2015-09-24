using LeagueSharp;
using LeagueSharp.Common;

namespace Kalista
{
    public static class Damages
    {
        private static readonly Obj_AI_Hero Player = ObjectManager.Player;

        public static bool IsRendKillable(this Obj_AI_Base target)
        {
            var hero = target as Obj_AI_Hero;
            return GetRendDamage(target) > target.Health && (hero == null || !hero.HasNoDmgBuff());
        }

        public static float GetRendDamage(Obj_AI_Base target)
        {
            return SpellManager.E.IsReady()
                       ? 0f
                       : GetActualDamage(target) - Config.SliderLinks["spellReductionE"].Value.Value;
        }

        public static float GetActualDamage(Obj_AI_Base target)
        {
            if (target.HasBuff("FerociousHowl"))
            {
                return (float)(SpellManager.E.GetDamage(target) * 0.7);
            }

            if (Player.HasBuff("summonerexhaust"))
            {
                return (float)(SpellManager.E.GetDamage(target) * 0.4);
            }

            return SpellManager.E.GetDamage(target);
        }

        public static float GetTotalDamage(Obj_AI_Base target)
        {
            // Auto attack damage
            double damage = Player.GetAutoAttackDamage(target);

            // Q damage
            if (SpellManager.Q.IsReady())
            {
                damage += Player.GetSpellDamage(target, SpellSlot.Q);
            }

            // E stack damage with ally ult on enemy
            if (SpellManager.E.IsReady() && target.HasBuff("FerociousHowl"))
            {
                damage += SpellManager.E.GetDamage(target) * 0.7;
            }

            // E stack damage with exhaust on player
            if (SpellManager.E.IsReady() && Player.HasBuff("summonerexhaust"))
            {
                damage += SpellManager.E.GetDamage(target) * 0.4;
            }

            // E stack damage
            if (SpellManager.E.IsReady())
            {
                damage += SpellManager.E.GetDamage(target);
            }

            return (float)damage;
        }

        public static float GetBaronReduction(Obj_AI_Base target)
        {
            return Player.HasBuff("barontarget")
                       ? SpellManager.E.GetDamage(target) * 0.5f
                       : SpellManager.E.GetDamage(target);
        }

        public static float GetDragonReduction(Obj_AI_Base target)
        {
            return Player.HasBuff("s5test_dragonslayerbuff")
                       ? SpellManager.E.GetDamage(target)
                         * (1 - (.07f * Player.GetBuffCount("s5test_dragonslayerbuff")))
                       : SpellManager.E.GetDamage(target);
        }

        public static float GetActualHealth(Obj_AI_Base target)
        {
            return target.Health + 5;
        }
    }
}
