using System;
using System.Windows;
using Wholesome_Inventory_Manager.Managers.CharacterSheet;
using static WAEEnums;

namespace Wholesome_Inventory_Manager.Settings
{
    public partial class PluginSettingsControl
    {
        internal PluginSettingsControl()
        {
            InitializeComponent();

            if (AutoEquipSettings.CurrentSettings.SpecSelectedByUser == ClassSpec.None)
            {
                ClassSpecManager.DetectSpec();
            }

            DiscordLink.RequestNavigate += (sender, e) =>
            {
                System.Diagnostics.Process.Start(e.Uri.ToString());
            };

            // AUTO EQUIP
            AutoDetectStatWeights.IsChecked = AutoEquipSettings.CurrentSettings.AutoDetectStatWeights;

            AutoEquipGear.IsChecked = AutoEquipSettings.CurrentSettings.AutoEquipGear;
            AutoSelectQuestRewards.IsChecked = AutoEquipSettings.CurrentSettings.AutoSelectQuestRewards;
            AutoEquipBags.IsChecked = AutoEquipSettings.CurrentSettings.AutoEquipBags;

            EquipQuiver.IsChecked = AutoEquipSettings.CurrentSettings.EquipQuiver;
            EquipThrown.IsChecked = AutoEquipSettings.CurrentSettings.EquipThrown;
            EquipBows.IsChecked = AutoEquipSettings.CurrentSettings.EquipBows;
            EquipGuns.IsChecked = AutoEquipSettings.CurrentSettings.EquipGuns;
            EquipCrossbows.IsChecked = AutoEquipSettings.CurrentSettings.EquipCrossbows;
            SwitchRanged.IsChecked = AutoEquipSettings.CurrentSettings.SwitchRanged;
            EquipAmmo.IsChecked = AutoEquipSettings.CurrentSettings.EquipAmmo;

            EquipOneHanders.IsChecked = AutoEquipSettings.CurrentSettings.EquipOneHanders;
            EquipTwoHanders.IsChecked = AutoEquipSettings.CurrentSettings.EquipTwoHanders;
            EquipShields.IsChecked = AutoEquipSettings.CurrentSettings.EquipShields;

            // Group loot
            AlwaysGreed.IsChecked = AutoEquipSettings.CurrentSettings.AlwaysGreed;
            AlwaysPass.IsChecked = AutoEquipSettings.CurrentSettings.AlwaysPass;

            // Misc
            RestackItems.IsChecked = AutoEquipSettings.CurrentSettings.RestackItems;
            UseScrolls.IsChecked = AutoEquipSettings.CurrentSettings.UseScrolls;

            // STATS
            StatsPreset.ItemsSource = Enum.GetNames(typeof(ClassSpec));
            UpdateStats();
            StatsPreset.SelectionChanged += new System.Windows.Controls.SelectionChangedEventHandler(AutoDetectStatsPresetChanged);
            GroupStats.IsEnabled = !(bool)AutoDetectStatWeights.IsChecked;

            // LOOT FILTER
            LootFilterActivated.IsChecked = AutoEquipSettings.CurrentSettings.LootFilterActivated;

            // Rarity
            DeleteGray.IsChecked = AutoEquipSettings.CurrentSettings.DeleteGray;
            AnyGray.IsChecked = AutoEquipSettings.CurrentSettings.AnyGray;
            KeepGray.IsChecked = AutoEquipSettings.CurrentSettings.KeepGray;

            DeleteWhite.IsChecked = AutoEquipSettings.CurrentSettings.DeleteWhite;
            AnyWhite.IsChecked = AutoEquipSettings.CurrentSettings.AnyWhite;
            KeepWhite.IsChecked = AutoEquipSettings.CurrentSettings.KeepWhite;

            DeleteGreen.IsChecked = AutoEquipSettings.CurrentSettings.DeleteGreen;
            AnyGreen.IsChecked = AutoEquipSettings.CurrentSettings.AnyGreen;
            KeepGreen.IsChecked = AutoEquipSettings.CurrentSettings.KeepGreen;

            DeleteBlue.IsChecked = AutoEquipSettings.CurrentSettings.DeleteBlue;
            AnyBlue.IsChecked = AutoEquipSettings.CurrentSettings.AnyBlue;
            KeepBlue.IsChecked = AutoEquipSettings.CurrentSettings.KeepBlue;

            // Value
            if (Main.WoWVersion < ToolBox.WoWVersion.WOTLK)
            {
                DeleteItemWithNoValue.IsEnabled = false;
                DeleteItemWithNoValue.IsChecked = false;
                DeleteGoldValue.IsEnabled = false;
                DeleteSilverValue.IsEnabled = false;
                DeleteCopperValue.IsEnabled = false;
            }
            else
            {
                DeleteItemWithNoValue.IsChecked = AutoEquipSettings.CurrentSettings.DeleteItemWithNoValue;
                DeleteGoldValue.Value = AutoEquipSettings.CurrentSettings.DeleteGoldValue;
                DeleteSilverValue.Value = AutoEquipSettings.CurrentSettings.DeleteSilverValue;
                DeleteCopperValue.Value = AutoEquipSettings.CurrentSettings.DeleteCopperValue;
            }

            // Misc
            DeleteDeprecatedQuestItems.IsChecked = AutoEquipSettings.CurrentSettings.DeleteDeprecatedQuestItems;
            LogItemInfo.IsChecked = AutoEquipSettings.CurrentSettings.LogItemInfo;
        }

        private void LogItemInfoChanged(object sender, RoutedEventArgs e)
        {
            AutoEquipSettings.CurrentSettings.LogItemInfo = (bool)LogItemInfo.IsChecked;
            AutoEquipSettings.CurrentSettings.Save();
        }

        private void AlwaysPassChanged(object sender, RoutedEventArgs e)
        {
            AutoEquipSettings.CurrentSettings.AlwaysPass = (bool)AlwaysPass.IsChecked;
            AutoEquipSettings.CurrentSettings.Save();
        }

        private void AlwaysGreedChanged(object sender, RoutedEventArgs e)
        {
            AutoEquipSettings.CurrentSettings.AlwaysGreed = (bool)AlwaysGreed.IsChecked;
            AutoEquipSettings.CurrentSettings.Save();
        }

        private void AutoSelectQuestRewardsChanged(object sender, RoutedEventArgs e)
        {
            AutoEquipSettings.CurrentSettings.AutoSelectQuestRewards = (bool)AutoSelectQuestRewards.IsChecked;
            AutoEquipSettings.CurrentSettings.Save();
        }

        private void SwitchRangedChanged(object sender, RoutedEventArgs e)
        {
            AutoEquipSettings.CurrentSettings.SwitchRanged = (bool)SwitchRanged.IsChecked;
            AutoEquipSettings.CurrentSettings.Save();
        }

        private void RestackItemsChanged(object sender, RoutedEventArgs e)
        {
            AutoEquipSettings.CurrentSettings.RestackItems = (bool)RestackItems.IsChecked;
            AutoEquipSettings.CurrentSettings.Save();
        }

        public void UseScrollsChanged(object sender, RoutedEventArgs e)
        {
            AutoEquipSettings.CurrentSettings.UseScrolls = (bool)UseScrolls.IsChecked;
            AutoEquipSettings.CurrentSettings.Save();
        }

        private void EquipAmmoChanged(object sender, RoutedEventArgs e)
        {
            AutoEquipSettings.CurrentSettings.EquipAmmo = (bool)EquipAmmo.IsChecked;
            AutoEquipSettings.CurrentSettings.Save();
        }

        private void DeleteDeprecatedQuestItemsChanged(object sender, RoutedEventArgs e)
        {
            AutoEquipSettings.CurrentSettings.DeleteDeprecatedQuestItems = (bool)DeleteDeprecatedQuestItems.IsChecked;
            AutoEquipSettings.CurrentSettings.Save();
        }

        private void DeleteCopperValueChanged(object sender, RoutedEventArgs e)
        {
            AutoEquipSettings.CurrentSettings.DeleteCopperValue = (int)DeleteCopperValue.Value;
            AutoEquipSettings.CurrentSettings.Save();
        }

        private void DeleteSilverValueChanged(object sender, RoutedEventArgs e)
        {
            AutoEquipSettings.CurrentSettings.DeleteSilverValue = (int)DeleteSilverValue.Value;
            AutoEquipSettings.CurrentSettings.Save();
        }

        private void DeleteGoldValueChanged(object sender, RoutedEventArgs e)
        {
            AutoEquipSettings.CurrentSettings.DeleteGoldValue = (int)DeleteGoldValue.Value;
            AutoEquipSettings.CurrentSettings.Save();
        }

        private void DeleteItemWithNoValueChanged(object sender, RoutedEventArgs e)
        {
            AutoEquipSettings.CurrentSettings.DeleteItemWithNoValue = (bool)DeleteItemWithNoValue.IsChecked;
            AutoEquipSettings.CurrentSettings.Save();
        }

        private void FilterGreenChanged(object sender, RoutedEventArgs e)
        {
            AutoEquipSettings.CurrentSettings.DeleteGreen = (bool)DeleteGreen.IsChecked;
            AutoEquipSettings.CurrentSettings.AnyGreen = (bool)AnyGreen.IsChecked;
            AutoEquipSettings.CurrentSettings.KeepGreen = (bool)KeepGreen.IsChecked;
            AutoEquipSettings.CurrentSettings.Save();
        }

        private void FilterBlueChanged(object sender, RoutedEventArgs e)
        {
            AutoEquipSettings.CurrentSettings.DeleteBlue = (bool)DeleteBlue.IsChecked;
            AutoEquipSettings.CurrentSettings.AnyBlue = (bool)AnyBlue.IsChecked;
            AutoEquipSettings.CurrentSettings.KeepBlue = (bool)KeepBlue.IsChecked;
            AutoEquipSettings.CurrentSettings.Save();
        }

        private void FilterWhiteChanged(object sender, RoutedEventArgs e)
        {
            AutoEquipSettings.CurrentSettings.DeleteWhite = (bool)DeleteWhite.IsChecked;
            AutoEquipSettings.CurrentSettings.AnyWhite = (bool)AnyWhite.IsChecked;
            AutoEquipSettings.CurrentSettings.KeepWhite = (bool)KeepWhite.IsChecked;
            AutoEquipSettings.CurrentSettings.Save();
        }

        private void FilterGrayChanged(object sender, RoutedEventArgs e)
        {
            AutoEquipSettings.CurrentSettings.DeleteGray = (bool)DeleteGray.IsChecked;
            AutoEquipSettings.CurrentSettings.AnyGray = (bool)AnyGray.IsChecked;
            AutoEquipSettings.CurrentSettings.KeepGray = (bool)KeepGray.IsChecked;
            AutoEquipSettings.CurrentSettings.Save();
        }

        private void LootFilterActivatedChanged(object sender, RoutedEventArgs e)
        {
            AutoEquipSettings.CurrentSettings.LootFilterActivated = (bool)LootFilterActivated.IsChecked;
            AutoEquipSettings.CurrentSettings.Save();
        }

        private void EquipShieldsChanged(object sender, RoutedEventArgs e)
        {
            AutoEquipSettings.CurrentSettings.EquipShields = (bool)EquipShields.IsChecked;
            AutoEquipSettings.CurrentSettings.Save();
        }

        private void EquipTwoHandersChanged(object sender, RoutedEventArgs e)
        {
            AutoEquipSettings.CurrentSettings.EquipTwoHanders = (bool)EquipTwoHanders.IsChecked;
            AutoEquipSettings.CurrentSettings.Save();
        }

        private void EquipOneHandersChanged(object sender, RoutedEventArgs e)
        {
            AutoEquipSettings.CurrentSettings.EquipOneHanders = (bool)EquipOneHanders.IsChecked;
            AutoEquipSettings.CurrentSettings.Save();
        }

        private void AutoDetectStatsPresetChanged(object sender, RoutedEventArgs e)
        {
            Logger.Log("SETTING CHANGED");
            AutoEquipSettings.CurrentSettings.SpecSelectedByUser = (ClassSpec)Enum.Parse(typeof(ClassSpec), StatsPreset.SelectedIndex.ToString());
            SettingsPresets.ChangeStatsWeightSettings(AutoEquipSettings.CurrentSettings.SpecSelectedByUser);
            UpdateStats();
            AutoEquipSettings.CurrentSettings.Save();
        }

        private void AutoDetectStatWeightsChanged(object sender, RoutedEventArgs e)
        {
            AutoEquipSettings.CurrentSettings.AutoDetectStatWeights = (bool)AutoDetectStatWeights.IsChecked;
            GroupStats.IsEnabled = !(bool)AutoDetectStatWeights.IsChecked;
            ClassSpecManager.DetectSpec();
            SettingsPresets.ChangeStatsWeightSettings(AutoEquipSettings.CurrentSettings.SpecSelectedByUser);
            UpdateStats();
            AutoEquipSettings.CurrentSettings.Save();
        }

        private void Mana5WeightChanged(object sender, RoutedEventArgs e)
        {
            AutoEquipSettings.CurrentSettings.SetStat(CharStat.ManaPer5, (int)Mana5Weight.Value);
            AutoEquipSettings.CurrentSettings.Save();
        }

        private void ArmorWeightChanged(object sender, RoutedEventArgs e)
        {
            AutoEquipSettings.CurrentSettings.SetStat(CharStat.Armor, (int)ArmorWeight.Value);
            AutoEquipSettings.CurrentSettings.Save();
        }

        private void ResilienceWeightChanged(object sender, RoutedEventArgs e)
        {
            AutoEquipSettings.CurrentSettings.SetStat(CharStat.ResilienceRating, (int)ResilienceWeight.Value);
            AutoEquipSettings.CurrentSettings.Save();
        }

        private void DodgeRatingWeightChanged(object sender, RoutedEventArgs e)
        {
            AutoEquipSettings.CurrentSettings.SetStat(CharStat.DodgeRating, (int)DodgeRatingWeight.Value);
            AutoEquipSettings.CurrentSettings.Save();
        }

        private void ParryRatingWeightChanged(object sender, RoutedEventArgs e)
        {
            AutoEquipSettings.CurrentSettings.SetStat(CharStat.ParryRating, (int)ParryRatingWeight.Value);
            AutoEquipSettings.CurrentSettings.Save();
        }

        private void DefenseRatingWeightChanged(object sender, RoutedEventArgs e)
        {
            AutoEquipSettings.CurrentSettings.SetStat(CharStat.DefenseRating, (int)DefenseRatingWeight.Value);
            AutoEquipSettings.CurrentSettings.Save();
        }

        private void ShieldBlockWeightChanged(object sender, RoutedEventArgs e)
        {
            AutoEquipSettings.CurrentSettings.SetStat(CharStat.BlockValue, (int)ShieldBlockRatingWeight.Value);
            AutoEquipSettings.CurrentSettings.Save();
        }

        private void ShieldBlockRatingWeightChanged(object sender, RoutedEventArgs e)
        {
            AutoEquipSettings.CurrentSettings.SetStat(CharStat.BlockRating, (int)ShieldBlockRatingWeight.Value);
            AutoEquipSettings.CurrentSettings.Save();
        }

        private void SpellPenetrationWeightChanged(object sender, RoutedEventArgs e)
        {
            AutoEquipSettings.CurrentSettings.SetStat(CharStat.SpellPenetration, (int)SpellPenetrationWeight.Value);
            AutoEquipSettings.CurrentSettings.Save();
        }

        private void ArmorPenetrationWeightChanged(object sender, RoutedEventArgs e)
        {
            AutoEquipSettings.CurrentSettings.SetStat(CharStat.ArmorPenetrationRating, (int)ArmorPenetrationWeight.Value);
            AutoEquipSettings.CurrentSettings.Save();
        }

        private void ExpertiseRatingWeightChanged(object sender, RoutedEventArgs e)
        {
            AutoEquipSettings.CurrentSettings.SetStat(CharStat.ExpertiseRating, (int)ExpertiseRatingWeight.Value);
            AutoEquipSettings.CurrentSettings.Save();
        }

        private void HitRatingWeightChanged(object sender, RoutedEventArgs e)
        {
            AutoEquipSettings.CurrentSettings.SetStat(CharStat.HitRating, (int)HitRatingWeight.Value);
            AutoEquipSettings.CurrentSettings.Save();
        }

        private void HasteRatingWeightChanged(object sender, RoutedEventArgs e)
        {
            AutoEquipSettings.CurrentSettings.SetStat(CharStat.HasteRating, (int)HasteRatingWeight.Value);
            AutoEquipSettings.CurrentSettings.Save();
        }

        private void CritRatingWeightChanged(object sender, RoutedEventArgs e)
        {
            AutoEquipSettings.CurrentSettings.SetStat(CharStat.CriticalStrikeRating, (int)CritRatingWeight.Value);
            AutoEquipSettings.CurrentSettings.Save();
        }

        private void SpellPowerWeightChanged(object sender, RoutedEventArgs e)
        {
            AutoEquipSettings.CurrentSettings.SetStat(CharStat.SpellPower, (int)SpellPowerWeight.Value);
            AutoEquipSettings.CurrentSettings.Save();
        }

        private void AttackPowerWeightChanged(object sender, RoutedEventArgs e)
        {
            AutoEquipSettings.CurrentSettings.SetStat(CharStat.AttackPower, (int)AttackPowerWeight.Value);
            AutoEquipSettings.CurrentSettings.SetStat(CharStat.AttackPowerinForms, (int)AttackPowerWeight.Value);
            AutoEquipSettings.CurrentSettings.Save();
        }

        private void DPSWeightChanged(object sender, RoutedEventArgs e)
        {
            AutoEquipSettings.CurrentSettings.SetStat(CharStat.DamagePerSecond, (int)DPSWeight.Value);
            AutoEquipSettings.CurrentSettings.Save();
        }

        private void AgilityWeightChanged(object sender, RoutedEventArgs e)
        {
            AutoEquipSettings.CurrentSettings.SetStat(CharStat.Agility, (int)AgilityWeight.Value);
            AutoEquipSettings.CurrentSettings.Save();
        }

        private void SpiritWeightChanged(object sender, RoutedEventArgs e)
        {
            AutoEquipSettings.CurrentSettings.SetStat(CharStat.Spirit, (int)SpiritWeight.Value);
            AutoEquipSettings.CurrentSettings.Save();
        }

        private void IntellectWeightChanged(object sender, RoutedEventArgs e)
        {
            AutoEquipSettings.CurrentSettings.SetStat(CharStat.Intellect, (int)IntellectWeight.Value);
            AutoEquipSettings.CurrentSettings.Save();
        }

        private void StrengthWeightChanged(object sender, RoutedEventArgs e)
        {
            AutoEquipSettings.CurrentSettings.SetStat(CharStat.Strength, (int)StrengthWeight.Value);
            AutoEquipSettings.CurrentSettings.Save();
        }

        private void StaminaWeightChanged(object sender, RoutedEventArgs e)
        {
            AutoEquipSettings.CurrentSettings.SetStat(CharStat.Stamina, (int)StaminaWeight.Value);
            AutoEquipSettings.CurrentSettings.Save();
        }

        private void EquipCrossbowsChanged(object sender, RoutedEventArgs e)
        {
            AutoEquipSettings.CurrentSettings.EquipCrossbows = (bool)EquipCrossbows.IsChecked;
            AutoEquipSettings.CurrentSettings.Save();
        }

        private void EquipGunsChanged(object sender, RoutedEventArgs e)
        {
            AutoEquipSettings.CurrentSettings.EquipGuns = (bool)EquipGuns.IsChecked;
            AutoEquipSettings.CurrentSettings.Save();
        }

        private void EquipBowsChanged(object sender, RoutedEventArgs e)
        {
            AutoEquipSettings.CurrentSettings.EquipBows = (bool)EquipBows.IsChecked;
            AutoEquipSettings.CurrentSettings.Save();
        }

        private void EquipThrownChanged(object sender, RoutedEventArgs e)
        {
            AutoEquipSettings.CurrentSettings.EquipThrown = (bool)EquipThrown.IsChecked;
            AutoEquipSettings.CurrentSettings.Save();
        }

        private void EquipQuiverChanged(object sender, RoutedEventArgs e)
        {
            AutoEquipSettings.CurrentSettings.EquipQuiver = (bool)EquipQuiver.IsChecked;
            AutoEquipSettings.CurrentSettings.Save();
        }

        private void AutoEquipBagsChanged(object sender, RoutedEventArgs e)
        {
            AutoEquipSettings.CurrentSettings.AutoEquipBags = (bool)AutoEquipBags.IsChecked;
            AutoEquipSettings.CurrentSettings.Save();
        }

        private void AutoEquipGearChanged(object sender, RoutedEventArgs e)
        {
            AutoEquipSettings.CurrentSettings.AutoEquipGear = (bool)AutoEquipGear.IsChecked;
            AutoEquipSettings.CurrentSettings.Save();
        }

        private void UpdateStats()
        {
            StatsPreset.SelectedValue = AutoEquipSettings.CurrentSettings.SpecSelectedByUser.ToString();

            // Base stats
            StaminaWeight.Value = AutoEquipSettings.CurrentSettings.GetStat(CharStat.Stamina);
            StrengthWeight.Value = AutoEquipSettings.CurrentSettings.GetStat(CharStat.Strength);
            IntellectWeight.Value = AutoEquipSettings.CurrentSettings.GetStat(CharStat.Intellect);
            SpiritWeight.Value = AutoEquipSettings.CurrentSettings.GetStat(CharStat.Spirit);
            AgilityWeight.Value = AutoEquipSettings.CurrentSettings.GetStat(CharStat.Agility);
            DPSWeight.Value = AutoEquipSettings.CurrentSettings.GetStat(CharStat.DamagePerSecond);
            // Advanced stats
            AttackPowerWeight.Value = AutoEquipSettings.CurrentSettings.GetStat(CharStat.AttackPower);
            SpellPowerWeight.Value = AutoEquipSettings.CurrentSettings.GetStat(CharStat.SpellPower);
            CritRatingWeight.Value = AutoEquipSettings.CurrentSettings.GetStat(CharStat.CriticalStrikeRating);
            HasteRatingWeight.Value = AutoEquipSettings.CurrentSettings.GetStat(CharStat.HasteRating);
            HitRatingWeight.Value = AutoEquipSettings.CurrentSettings.GetStat(CharStat.HitRating);
            ExpertiseRatingWeight.Value = AutoEquipSettings.CurrentSettings.GetStat(CharStat.ExpertiseRating);
            // Expert stats
            ArmorPenetrationWeight.Value = AutoEquipSettings.CurrentSettings.GetStat(CharStat.ArmorPenetrationRating);
            SpellPenetrationWeight.Value = AutoEquipSettings.CurrentSettings.GetStat(CharStat.SpellPenetration);
            ShieldBlockRatingWeight.Value = AutoEquipSettings.CurrentSettings.GetStat(CharStat.BlockRating);
            ShieldBlockWeight.Value = AutoEquipSettings.CurrentSettings.GetStat(CharStat.BlockValue);
            DefenseRatingWeight.Value = AutoEquipSettings.CurrentSettings.GetStat(CharStat.DefenseRating);
            ParryRatingWeight.Value = AutoEquipSettings.CurrentSettings.GetStat(CharStat.ParryRating);
            DodgeRatingWeight.Value = AutoEquipSettings.CurrentSettings.GetStat(CharStat.DodgeRating);
            ResilienceWeight.Value = AutoEquipSettings.CurrentSettings.GetStat(CharStat.ResilienceRating);
            ArmorWeight.Value = AutoEquipSettings.CurrentSettings.GetStat(CharStat.Armor);
            Mana5Weight.Value = AutoEquipSettings.CurrentSettings.GetStat(CharStat.ManaPer5);
        }
    }
}