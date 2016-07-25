using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Net;
using EloBuddy;
using EloBuddy.SDK;
using EloBuddy.SDK.Constants;
using EloBuddy.SDK.Enumerations;
using EloBuddy.SDK.Events;
using EloBuddy.SDK.Menu.Values;
using EloBuddy.SDK.Rendering;
using SharpDX;
using Mario_s_Lib;
using static RoninVelkoz.Menus;

namespace RoninVelkoz
{
    public static class SpellsManager
    {
        /*
        Targeted spells are like Katarina`s Q
        Active spells are like Katarina`s W
        Skillshots are like Ezreal`s Q
        Circular Skillshots are like Lux`s E and Tristana`s W
        Cone Skillshots are like Annie`s W and ChoGath`s W
        */
        public static AIHeroClient Champion { get { return Player.Instance; } }
        //Remenber of putting the correct type of the spell here
        public static Spell.Skillshot Q;
        public static Spell.Skillshot W;
        public static Spell.Skillshot E;
        public static Spell.Skillshot R;
        public static MissileClient Qmiss = null;
        public const int MissileSpeed = 2100;
        public const int CastDelay = 250;
        public const int SpellWidth = 45;
        public const int SpellRange = 1100;
        public static MissileClient Handle { get; set; }
        public static Vector2 Direction { get; set; }
        public static List<Vector2> Perpendiculars { get; set; }
        public static float StackerStamp = 0;
        /// <summary>
        /// It sets the values to the spells
        /// </summary>
        public static void InitializeSpells()
        {
            Q = new Spell.Skillshot(SpellSlot.Q, 1050, SkillShotType.Linear, 250, 1300, 50);
            W = new Spell.Skillshot(SpellSlot.W, 1050, SkillShotType.Linear, 250, 1700, 80);
            E = new Spell.Skillshot(SpellSlot.E, 850, SkillShotType.Circular, 500, 1500, 120);
            R = new Spell.Skillshot(SpellSlot.R, 1575, SkillShotType.Linear);
            Obj_AI_Base.OnLevelUp += Obj_AI_Base_OnLevelUp;
        }

        #region Damages

        /// <summary>
        /// It will return the damage but you need to set them before getting the damage
        /// </summary>
        /// <param name="target"></param>
        /// <param name="slot"></param>
        /// <returns></returns>
        public static float GetDamage(this Obj_AI_Base target, SpellSlot slot)
        {
            var damageType = DamageType.Magical;
            var AD = Player.Instance.FlatPhysicalDamageMod;
            var AP = Player.Instance.FlatMagicDamageMod;
            var sLevel = Player.GetSpell(slot).Level - 1;

            //You can get the damage information easily on wikia

            var dmg = 0f;

            switch (slot)
            {
                case SpellSlot.Q:
                    if (Q.IsReady())
                    {
                        //Information of Q damage
                        dmg += new float[] {20, 45, 70, 95, 120}[sLevel] + 1f*AD;
                    }
                    break;
                case SpellSlot.W:
                    if (W.IsReady())
                    {
                        //Information of W damage
                        dmg += new float[] {0, 0, 0, 0, 0}[sLevel] + 1f*AD;
                    }
                    break;
                case SpellSlot.E:
                    if (E.IsReady())
                    {
                        //Information of E damage
                        dmg += new float[] {80, 110, 140, 170, 200}[sLevel];
                    }
                    break;
                case SpellSlot.R:
                    if (R.IsReady())
                    {
                        //Information of R damage
                        dmg += new float[] {600, 840, 1080}[sLevel]*0.6f + 1.2f*AP;
                    }
                    break;
            }
            return Player.Instance.CalculateDamageOnUnit(target, damageType, dmg - 10);
        }

        #endregion Damages

        /// <summary>
        /// This event is triggered when a unit levels up
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private static void Obj_AI_Base_OnLevelUp(Obj_AI_Base sender, Obj_AI_BaseLevelUpEventArgs args)
        {
            if (MiscMenu.GetCheckBoxValue("activateAutoLVL") && sender.IsMe)
            {
                var delay = MiscMenu.GetSliderValue("delaySlider");
                Core.DelayAction(LevelUPSpells, delay);

            }
        }

        /// <summary>
        /// It will level up the spell using the values of the comboboxes on the menu as a priority
        /// </summary>
        private static void LevelUPSpells()
        {
            if (Player.Instance.Spellbook.CanSpellBeUpgraded(SpellSlot.R))
            {
                Player.Instance.Spellbook.LevelSpell(SpellSlot.R);
            }

            if (Player.Instance.Spellbook.CanSpellBeUpgraded(GetSlotFromComboBox(MiscMenu.GetComboBoxValue("firstFocus"))))
            {
                Player.Instance.Spellbook.LevelSpell(GetSlotFromComboBox(MiscMenu.GetComboBoxValue("firstFocus")));
            }

            if (Player.Instance.Spellbook.CanSpellBeUpgraded(GetSlotFromComboBox(MiscMenu.GetComboBoxValue("secondFocus"))))
            {
                Player.Instance.Spellbook.LevelSpell(GetSlotFromComboBox(MiscMenu.GetComboBoxValue("secondFocus")));
            }

            if (Player.Instance.Spellbook.CanSpellBeUpgraded(GetSlotFromComboBox(MiscMenu.GetComboBoxValue("thirdFocus"))))
            {
                Player.Instance.Spellbook.LevelSpell(GetSlotFromComboBox(MiscMenu.GetComboBoxValue("thirdFocus")));
            }
        }


        public static float RDamage()
        {
            return new float[] { 0, 500, 700, 900 }[R.Level] + 0.5f * Champion.FlatMagicDamageMod;
        }
        //public static void QSplit(EventArgs args)
        //{
        //    // Check if the missile is active
        //    if (Handle != null && Q.IsReady() && Q.Name == "velkozqsplitactivate")
        //    {
        //        foreach (var perpendicular in Perpendiculars)
        //        {
        //            if (Handle != null)
        //            {
        //                var startPos = Handle.Position.To2D();
        //                var endPos = Handle.Position.To2D() + SpellRange * perpendicular;

        //                var collisionObjects = ObjectManager.Get<Obj_AI_Base>()
        //                    .Where(o => o.IsEnemy && !o.IsDead && !o.IsStructure() && !o.IsWard() && !o.IsInvulnerable
        //                            && o.Distance(Champion, true) < (SpellRange + 200).Pow()
        //                            && o.ServerPosition.To2D().Distance(startPos, endPos, true, true) <= (SpellWidth * 2 + o.BoundingRadius).Pow());

        //                var colliding = collisionObjects
        //                    .Where(o => o.Type == GameObjectType.AIHeroClient && o.IsValidTarget()
        //                            && Prediction.Position.Collision.LinearMissileCollision(o, startPos, endPos, MissileSpeed, SpellWidth, CastDelay, (int)o.BoundingRadius))
        //                        .OrderBy(o => o.Distance(Champion, true)).FirstOrDefault();

        //                if (colliding != null)
        //                {
        //                    Player.CastSpell(SpellSlot.Q);
        //                    Handle = null;
        //                }
        //            }
        //        }
        //    }
        //    else
        //        Handle = null;
        //}

        private static SpellSlot GetSlotFromComboBox(this int value)
        {
            switch (value)
            {
                case 0:
                    return SpellSlot.Q;
                case 1:
                    return SpellSlot.W;
                case 2:
                    return SpellSlot.E;
            }
            Chat.Print("Failed getting slot");
            return SpellSlot.Unknown;
        }
    }
}
