//#define DEBUG

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Oxide.Plugins
{
    [Info("forbiddenItems", "Ohm", "1.0.0")]
    [Description("Preventing certain players from equipping/using certain items")]
    public class forbiddenItems : RustPlugin
    {

        void Init()
        {
            permission.RegisterPermission("forbiddenItems.corn", this);
            permission.RegisterPermission("forbiddenItems.pumpkin", this);
        }

        #region Oxide Hooks

        object CanResearchItem(BasePlayer player, Item targetItem)
        {
#if DEBUG
            PrintToChat(player, "trying to research {0}", targetItem.info.shortname);
#endif
            if(forbidden(player, targetItem.info.shortname))
            {
                PrintToChat(player, "you are not allowed to research {0}!", targetItem.info.shortname);
                return false;
            }
            return null;
        }

        object OnItemCraft(ItemCraftTask item)
        {
            BasePlayer player = item.owner;
            ItemDefinition target = item.blueprint.targetItem;
#if DEBUG
            PrintToChat(player, "crafting {0}", target.name);
#endif
            if(forbidden(player, target.name))
            {
                PrintToChat(player, "you are not allowed to craft {0}!", target.name);
                foreach (ItemAmount ingr in item.blueprint.ingredients)
                {
                    Item refund = ItemManager.Create(ingr.itemDef, (int)ingr.amount);
                    if (refund != null) player.GiveItem(refund);
                }
                return false;
            }
            return null;
        }

        object OnReloadWeapon(BasePlayer player, BaseProjectile projectile)
        {
#if DEBUG
            PrintToChat(player, "reloading {0} with {1}", projectile.ShortPrefabName, projectile.primaryMagazine.ammoType.shortname);
#endif
            if (player != null)
            {
                projectile.primaryMagazine.SwitchAmmoTypesIfNeeded(player);
                if (forbidden(player, projectile.primaryMagazine.ammoType.shortname))
                {
                    PrintToChat(player, "you are not allowed to use {0}!", projectile.primaryMagazine.ammoType.shortname);
                    return false;
                }
            }
            return null;
        }

        void OnRocketLaunched(BasePlayer player, BaseEntity entity)
        {
            if (player.GetHeldEntity() is BaseProjectile)
            {
                var weapon = player.GetHeldEntity() as BaseProjectile;
                var ammo = weapon.primaryMagazine.ammoType;

#if DEBUG
                PrintToChat(player, "{0} was launched", ammo.shortname);
#endif
                if (forbidden(player, ammo.shortname))
                {
                    PrintToChat(player, "you are not allowed to launch {0}!", ammo.shortname);
                    entity.Kill();
                    ItemDefinition def = ItemManager.FindItemDefinition(ammo.shortname);
                    Item refund = null;
                    if (def != null) refund = ItemManager.Create(def);
                    if (refund != null) player.GiveItem(refund);
                }
            }
        }

        void OnWeaponFired(BaseProjectile projectile, BasePlayer player, ItemModProjectile mod, ProtoBuf.ProjectileShoot projectiles)
        {
            var ammo = projectile.primaryMagazine.ammoType;

#if DEBUG
            PrintToChat(player, "{0} was fired", ammo.shortname);
#endif
            if (forbidden(player, ammo.shortname))
            {
                PrintToChat(player, "you are not allowed to fire {0}!", ammo.shortname);
                projectile.UnloadAmmo(player.GetActiveItem(), player);
                ItemDefinition def = ItemManager.FindItemDefinition(ammo.shortname);
                Item refund = null;
                if (def != null) refund = ItemManager.Create(def);
                if (refund != null) player.GiveItem(refund);
            }
        }

        object CanMoveItem(Item item, PlayerInventory playerLoot, uint targetContainer, int targetSlot, int amount)
        {
            if (item != null)
            {
                BasePlayer player = item.GetOwnerPlayer();

                if (player != null)
                {
                    foreach (Item it in player.inventory.AllItems())
                    {
                        if (it.contents != null)
                        {
                            if (it.contents.uid == targetContainer)
                            {
#if DEBUG
                                PrintToChat(player, "item has been moved into another one");
#endif
                                if (forbidden(player, item.info.shortname))
                                {
                                    PrintToChat(player, "you are not allowed to equip {0}!", item.info.shortname);
                                    return false;
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }

        void OnPlayerActiveItemChanged(BasePlayer player, Item oldItem, Item newItem)
        {
#if DEBUG
            PrintToChat(player, "changed active item from {0} to {1}", oldItem.info.shortname, newItem.info.shortname);
#endif
            if (newItem.contents != null)
            {
                if (newItem.contents.itemList.Count != 0)
                {
#if DEBUG
                    PrintToChat(player, "{0} content:", newItem.info.shortname);
#endif
                    foreach (Item it in newItem.contents.itemList)
                    {
#if DEBUG
                        PrintToChat(player, "{0}", it.info.shortname);
#endif
                        if (forbidden(player, it.info.shortname))
                        {
                            player.GiveItem(it);
                            break;
                        }
                    }
                }
            }
        }

        [ChatCommand("info")]
        private void info(BasePlayer player, string command, string[] args)
        {
            PrintToChat(player, "info: {0}", player.GetActiveItem().contents.parent.info.shortname);
            foreach(Item it in player.GetActiveItem().contents.itemList)
            {
                PrintToChat(player, "{0}", it.info.shortname);
            }
        }

            ItemContainer.CanAcceptResult CanAcceptItem(ItemContainer container, Item item, int targetPos)
        {
#if HARDDEBUG
            PrintToChat( "item: {0} moved to container: {1}", item.info.shortname, container.uid);
#endif

            if (container != null)
            {
                BasePlayer basePlayer = item.GetOwnerPlayer();
                if(basePlayer == null)
                {
                    if(container.parent != null)
                    basePlayer = container.parent.GetOwnerPlayer();
                }

                if (basePlayer != null)
                {
#if DEBUG
                    PrintToChat(basePlayer, "item: {0} moved to container: {1}", item.info.shortname, container.uid);
                    if (container.Equals(basePlayer.inventory.containerBelt)) PrintToChat(basePlayer, "Belt");
#endif
                        if (forbidden(basePlayer, item.info.shortname))
                        {
                            if (container.Equals(basePlayer.inventory.containerBelt) || container.Equals(basePlayer.inventory.containerWear))
                            {
                                PrintToChat(basePlayer, "you are not allowed to equip {0}!", item.info.shortname);
                                return ItemContainer.CanAcceptResult.CannotAccept;
                            }
                            if (item.parent != null)
                            {
                                if (item.parent.parent != null)
                                {
                                    if (container.parent.parent.Equals(basePlayer.inventory.containerBelt))
                                    {
                                        PrintToChat(basePlayer, "you are not allowed to equip {0}!", item.info.shortname);
                                        return ItemContainer.CanAcceptResult.CannotAccept;
                                    }
                                }
                            }
                        }
                        


                }
                if (item.contents != null)
                {
                    if (item.contents.itemList.Count != 0)
                    {
#if DEBUG
                        PrintToChat(basePlayer, "{0} content:", item.info.shortname);
#endif
                        foreach (Item it in item.contents.itemList)
                        {
#if DEBUG
                            PrintToChat(basePlayer, "{0}", it.info.shortname);
#endif
                            if (forbidden(basePlayer, it.info.shortname))
                            {
                                basePlayer.GiveItem(it);
                                break;
                            }
                        }
                    }
                }
            }
            return ItemContainer.CanAcceptResult.CanAccept;
        }

        object OnItemAction(Item item, string action, BasePlayer player)
        {
            if (player != null)
            {
#if DEBUG
                PrintToChat(player, "{0} {1} {2}", player.displayName, action, item.info.shortname);
#endif
                if (forbidden(player, item.info.shortname))
                {
                    if (action == "unload_ammo" || action == "drop") return null;
                    PrintToChat(player, "you are not allowed to {0} {1}!", action, item.info.shortname);
                    return false;
                }
            }
            return null;
        }

        #endregion

        #region Helpers

        bool forbidden(BasePlayer basePlayer, string name)
        {
            if (config.blacklist.Contains(name))
            {
                if (permission.UserHasPermission(basePlayer.UserIDString, "forbiddenItems.corn"))
                {
                    if (config.corn.Contains(name))
                    {
                        return false;
                    }
                }

                else if (permission.UserHasPermission(basePlayer.UserIDString, "forbiddenItems.pumpkin"))
                {
                    if (config.pumpkin.Contains(name))
                    {
                        return false;
                    }
                }
                return true;
            }
            return false;
        }


        #endregion

        #region Config

        private static ConfigData config;

        private class ConfigData
        {
            [JsonProperty(PropertyName = "blacklist")]
            public List<string> blacklist;
            [JsonProperty(PropertyName = "corn")]
            public List<string> corn;
            [JsonProperty(PropertyName = "pumpkin")]
            public List<string> pumpkin;
        }

        private ConfigData GetDefaultConfig()
        {
            return new ConfigData
            {
                blacklist = new List<string>
                {
                    "example",
                    "example",
                    "example"
                },
                corn = new List<string>
                {
                    "example",
                    "example",
                    "example"
                },

                pumpkin = new List<string>
                {
                    "example",
                    "example",
                    "example"
                }
            };
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();

            try
            {
                config = Config.ReadObject<ConfigData>();
            }
            catch
            {
                LoadDefaultConfig();
            }

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            PrintError("Configuration file is corrupt(or not exists), creating new one!");
            config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config);
        }

        #endregion
    }
}