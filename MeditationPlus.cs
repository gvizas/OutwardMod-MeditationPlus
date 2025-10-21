using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using BepInEx.Logging;

namespace Meditation
{

    [BepInPlugin("com.github.system233.meditation", "MeditationPlus", "1.0.0")]
    public class MeditationPlus : BaseUnityPlugin
    {
        public static ConfigEntry<bool> EnableBurntSitRegen;
        public static ConfigEntry<bool> EnableCurrentSitRegen;
        public static ConfigEntry<float> BurntStaminaRegen;
        public static ConfigEntry<float> BurntHealthRegen;
        public static ConfigEntry<float> BurntManaRegen;
        public static ConfigEntry<float> CurrentStaminaRegen;
        public static ConfigEntry<float> CurrentHealthRegen;
        public static ConfigEntry<float> CurrentManaRegen;
        public static ConfigEntry<bool> EnableSitting;

        private static ManualLogSource logger;
        private readonly static Harmony harmony = new Harmony("com.github.system233.meditation");
        void Awake()
        {
            EnableBurntSitRegen = Config.Bind("Burnt Stat Regen",
                                     "EnableBurntSitRegen",
                                     true,
                                     "Enable or disable the regeneration of burnt stats while sitting");
            EnableCurrentSitRegen = Config.Bind("Current Stat Regen",
                                     "EnableCurrentSitRegen",
                                     true,
                                     "Enable or disable the regeneration of current(non-burnt) stats while sitting");
            BurntStaminaRegen = Config.Bind("Burnt Stat Regen",
                                     "BurntStaminaRegen",
                                     0.5f,
                                     "How quickly burnt stamina will regen while siting. Default: 0.5f");
            BurntHealthRegen = Config.Bind("Burnt Stat Regen",
                                     "BurntHealthRegen",
                                     0.5f,
                                     "How quickly burnt health will regen while siting. Default: 0.5f");
            BurntManaRegen = Config.Bind("Burnt Stat Regen",
                                     "BurntManaRegen",
                                     0.5f,
                                     "How quickly burnt Mana will regen while siting. Default: 0.5f");
            CurrentStaminaRegen = Config.Bind("Current Stat Regen",
                                     "CurrentStaminaRegen",
                                     1.0f,
                                     "How quickly stamina will regen while siting. Default: 1.0f");
            CurrentHealthRegen = Config.Bind("Current Stat Regen",
                                     "CurrentHealthRegen",
                                     1.0f,
                                     "How quickly health will regen while siting. Default: 1.0f");
            CurrentManaRegen = Config.Bind("Current Stat Regen",
                                     "CurrentManaRegen",
                                     1.0f,
                                     "How quickly mana will regen while siting. Default: 1.0f");
            EnableSitting = Config.Bind("Sitting",
                                     "EnableSitting",
                                     true,
                                     "Ability to toggle sitting from this mod if you prefer another mods implimentation. Default: true");
            logger = BepInEx.Logging.Logger.CreateLogSource("MeditationPlus");
            harmony.PatchAll();
            logger.LogInfo("loaded");
        }
        public static void LogInfo(params object[] args)
        {
            var _logger = logger;
            if (_logger == null)
            {
                return;
            }
            _logger.LogDebug(string.Join(",", args));
        }
    }

    [HarmonyPatch(typeof(PlayerCharacterStats), "OnUpdateStats")]
    class Patch_PlayerCharacterStats_OnUpdateStats
    {
        static bool Prefix(PlayerCharacterStats __instance)
        {
            var stats = __instance;
            Character character = stats.m_character;
            if (character.CurrentSpellCast == Character.SpellCastType.Sit)
            {
                if (MeditationPlus.EnableBurntSitRegen.Value)
                {
                    stats.m_burntStamina = Mathf.Clamp(stats.m_burntStamina - (MeditationPlus.BurntStaminaRegen.Value * Time.deltaTime), 0, stats.MaxStamina);
                    stats.m_burntHealth = Mathf.Clamp(stats.m_burntHealth - (MeditationPlus.BurntHealthRegen.Value * Time.deltaTime), 0, stats.MaxHealth);
                    stats.m_burntMana = Mathf.Clamp(stats.m_burntMana - (MeditationPlus.BurntManaRegen.Value * Time.deltaTime), 0, stats.MaxMana);
                }
                if (MeditationPlus.EnableCurrentSitRegen.Value)
                {

                    stats.m_stamina = Mathf.Clamp(stats.m_stamina + (MeditationPlus.CurrentStaminaRegen.Value * Time.deltaTime), 0, stats.ActiveMaxStamina);
                    stats.m_health = Mathf.Clamp(stats.m_health + (MeditationPlus.CurrentHealthRegen.Value * Time.deltaTime), 0, stats.ActiveMaxHealth);
                    stats.m_mana = Mathf.Clamp(stats.m_mana + (MeditationPlus.CurrentManaRegen.Value * Time.deltaTime), 0, stats.ActiveMaxMana);
                }
            }
            return true;
        }

    }

    [HarmonyPatch(typeof(ControlsInput), "Sheathe")]
    class Patch_ControlsInput_Sheathe
    {
        static void Postfix(ref bool __result, object[] __args)
        {
            int playerId = (int)__args[0];
            var im = ControlsInput.m_playerInputManager[playerId];
            var sheatheActionName = ControlsInput.GetGameplayActionName(ControlsInput.GameplayActions.Sheathe);
            __result = im.GetButtonUpQuick(sheatheActionName, 0.5f);
        }
    }
    [HarmonyPatch(typeof(LocalCharacterControl), "UpdateInteraction")]
    class Patch_LocalCharacterControl_UpdateInteraction
    {

        static void Postfix(LocalCharacterControl __instance)
        {
            var self = __instance;
            if (!MeditationPlus.EnableSitting.Value)
            {
                return;
            }
            if (self.InputLocked || self.Character.CharacterUI.ChatPanel.IsChatFocused)
            {
                return;
            }
            var character = self.Character;
            var playerId = character.OwnerPlayerSys.PlayerID;

            if (ControlsInput.QuickSlotToggle1(playerId) || ControlsInput.QuickSlotToggle1(playerId))
            {
                return;
            }
            var im = ControlsInput.m_playerInputManager[playerId];
            var sheatheActionName = ControlsInput.GetGameplayActionName(ControlsInput.GameplayActions.Sheathe);
            if (im.m_rewiredPlayer.GetButtonLongPress(sheatheActionName))
            {
                self.Character.CastSpell(Character.SpellCastType.Sit, self.Character.gameObject, Character.SpellCastModifier.Immobilized, 1, -1f);
            }

        }
    }
}
