using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using UnityEngine;
using Gungeon;

using Alexandria.ItemAPI;
using Dungeonator;
using System.Diagnostics;
using Alexandria.Misc;

namespace TheGrinReaper
{
    public class C // constants
    {
        public const float PIXELS_PER_TILE = 16f;
    }
    public class IDs
    {
        public static Dictionary<string, int> Pickups  { get; set; } = new Dictionary<string, int>();
        public static Dictionary<string, int> Guns     { get; set; } = new Dictionary<string, int>();
        public static Dictionary<string, int> Actives  { get; set; } = new Dictionary<string, int>();
        public static Dictionary<string, int> Passives { get; set; } = new Dictionary<string, int>();
    }
    public static class Lazy
    {
        // stolen from NN yay
        public static GameObject SpawnObject(GameObject thingToSpawn, Vector3 convertedVector, GameObject SpawnVFX = null)
        {
            Vector2 Vector2Position = convertedVector;

            GameObject newObject = UnityEngine.Object.Instantiate(thingToSpawn, convertedVector, Quaternion.identity);

            SpeculativeRigidbody ObjectSpecRigidBody = newObject.GetComponentInChildren<SpeculativeRigidbody>();
            Component[] componentsInChildren = newObject.GetComponentsInChildren(typeof(IPlayerInteractable));
            for (int i = 0; i < componentsInChildren.Length; i++)
            {
                // ETGModConsole.Log(" == "+componentsInChildren[i].GetType());
                IPlayerInteractable interactable = componentsInChildren[i] as IPlayerInteractable;
                if (interactable != null)
                {
                    newObject.transform.position.GetAbsoluteRoom().RegisterInteractable(interactable);
                }
            }
            Component[] componentsInChildren2 = newObject.GetComponentsInChildren(typeof(IPlaceConfigurable));
            for (int i = 0; i < componentsInChildren2.Length; i++)
            {
                IPlaceConfigurable placeConfigurable = componentsInChildren2[i] as IPlaceConfigurable;
                if (placeConfigurable != null)
                {
                    placeConfigurable.ConfigureOnPlacement(GameManager.Instance.Dungeon.data.GetAbsoluteRoomFromPosition(Vector2Position.ToIntVector2()));
                }
            }

            ObjectSpecRigidBody.Initialize();
            PhysicsEngine.Instance.RegisterOverlappingGhostCollisionExceptions(ObjectSpecRigidBody, null, false);

            if (SpawnVFX != null)
            {
                UnityEngine.Object.Instantiate<GameObject>(SpawnVFX, ObjectSpecRigidBody.sprite.WorldCenter, Quaternion.identity);
            }

            return newObject;
        }

        /// <summary>
        /// Perform basic initialization for a new gun definition.
        /// </summary>
        public static Gun InitGunFromStrings(
          string gunName, string spriteName, string projectileName, string shortDescription, string longDescription)
        {
            string newGunName  = gunName.Replace("'", "").Replace("-", "");  //get sane gun for item rename
            string baseGunName = newGunName.Replace(" ", "_").ToLower();  //get saner gun name for commands

            Gun gun = ETGMod.Databases.Items.NewGun(newGunName, spriteName);  //create a new gun using specified sprite name
            Game.Items.Rename("outdated_gun_mods:"+baseGunName, "cg:"+baseGunName);  //rename the gun for commands
            gun.encounterTrackable.EncounterGuid = baseGunName+"-"+spriteName; //create a unique guid for the gun
            gun.SetShortDescription(shortDescription); //set the gun's short description
            gun.SetLongDescription(longDescription); //set the gun's long description
            gun.SetupSprite(null, spriteName+"_idle_001", 8); //set the gun's ammonomicon sprit
            int projectileId = 0;
            if (int.TryParse(projectileName, out projectileId))
                gun.AddProjectileModuleFrom(PickupObjectDatabase.GetById(projectileId) as Gun, true, false); //set the gun's default projectile to inherit
            else
                gun.AddProjectileModuleFrom(projectileName, true, false); //set the gun's default projectile to inherit
            ETGMod.Databases.Items.Add(gun, false, "ANY");  //register the gun in the EtG database
            IDs.Guns[baseGunName] = gun.PickupObjectId; //register gun in gun ID database
            IDs.Pickups[baseGunName] = gun.PickupObjectId; //register gun in pickup ID database

            ETGModConsole.Log("Lazy Initialized Gun: "+baseGunName);
            return gun;
        }

        /// <summary>
        /// Perform basic initialization for a new projectile definition.
        /// </summary>
        public static Projectile PrefabProjectileFromGun(Gun gun, bool setGunDefaultProjectile = true)
        {
            //actually instantiate the projectile
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(gun.DefaultModule.projectiles[0]);
            projectile.gameObject.SetActive(false); //make sure the projectile isn't an active game object
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);  //mark the projectile as a prefab
            UnityEngine.Object.DontDestroyOnLoad(projectile); //make sure the projectile isn't destroyed when loaded as a prefab
            if (setGunDefaultProjectile)
                gun.DefaultModule.projectiles[0] = projectile; //reset the gun's default projectile
            return projectile;
        }

        /// <summary>
        /// Perform basic initialization for a copied projectile definition.
        /// </summary>
        public static Projectile PrefabProjectileFromExistingProjectile(Projectile baseProjectile)
        {
            //actually instantiate the projectile
            Projectile projectile = UnityEngine.Object.Instantiate<Projectile>(baseProjectile);
            projectile.gameObject.SetActive(false); //make sure the projectile isn't an active game object
            FakePrefab.MarkAsFakePrefab(projectile.gameObject);  //mark the projectile as a prefab
            UnityEngine.Object.DontDestroyOnLoad(projectile); //make sure the projectile isn't destroyed when loaded as a prefab
            return projectile;
        }

        /// <summary>
        /// Post a custom item pickup notification to the bottom of the screen
        /// </summary>
        public static void CustomNotification(string header, string text)
        {
            var sprite = GameUIRoot.Instance.notificationController.notificationObjectSprite;
            GameUIRoot.Instance.notificationController.DoCustomNotification(
                header,
                text,
                sprite.Collection,
                sprite.spriteId,
                UINotificationController.NotificationColor.PURPLE,
                false,
                false);
        }

        /// <summary>
        /// Calculate a vector from a given angle in degrees
        /// </summary>
        public static Vector2 AngleToVector(float angleInDegrees, float magnitude = 1)
        {
            Vector2 offset = new Vector2(
                Mathf.Cos(angleInDegrees*Mathf.PI/180),Mathf.Sin(angleInDegrees*Mathf.PI/180));
            return magnitude*offset;
        }

        /// <summary>
        /// Perform basic initialization for a new passive item definition. Stolen and modified from Noonum.
        /// </summary>
        public static PickupObject SetupItem<T>(string itemName, string spritePath, string shortDescription, string longDescription, string idPool = "ItemAPI")
            where T : PickupObject
        {
            GameObject obj = new GameObject(itemName);
            PickupObject item = obj.AddComponent<T>();
            ItemBuilder.AddSpriteToObject(itemName, spritePath, obj);

            item.encounterTrackable = null;

            ETGMod.Databases.Items.SetupItem(item, item.name);
            SpriteBuilder.AddToAmmonomicon(item.sprite.GetCurrentSpriteDef());
            item.encounterTrackable.journalData.AmmonomiconSprite = item.sprite.GetCurrentSpriteDef().name;

            item.SetName(item.name);
            item.SetShortDescription(shortDescription);
            item.SetLongDescription(longDescription);

            if (item is PlayerItem)
                (item as PlayerItem).consumable = false;

            string newItemName  = itemName.Replace("'", "").Replace("-", "");  //get sane item for item rename
            string baseItemName = newItemName.Replace(" ", "_").ToLower();  //get saner item name for commands
            Gungeon.Game.Items.Add(idPool + ":" + baseItemName, item);
            ETGMod.Databases.Items.Add(item);
            IDs.Passives[baseItemName] = item.PickupObjectId; //register item in passive ID database
            IDs.Pickups[baseItemName] = item.PickupObjectId; //register item in pickup ID database

            ETGModConsole.Log("Lazy Initialized Passive: "+baseItemName);
            return item;
        }

        /// <summary>
        /// Perform basic initialization for a new active item definition.
        /// </summary>
        public static PlayerItem SetupActive<T>(string itemName, string spritePath, string shortDescription, string longDescription, string idPool = "ItemAPI")
            where T : PlayerItem
        {
            GameObject obj = new GameObject(itemName);
            PlayerItem item = obj.AddComponent<T>();
            ItemBuilder.AddSpriteToObject(itemName, spritePath, obj);

            item.encounterTrackable = null;

            ETGMod.Databases.Items.SetupItem(item, item.name);
            SpriteBuilder.AddToAmmonomicon(item.sprite.GetCurrentSpriteDef());
            item.encounterTrackable.journalData.AmmonomiconSprite = item.sprite.GetCurrentSpriteDef().name;

            item.SetName(item.name);
            item.SetShortDescription(shortDescription);
            item.SetLongDescription(longDescription);

            if (item is PlayerItem)
                (item as PlayerItem).consumable = false;

            string newItemName  = itemName.Replace("'", "").Replace("-", "");  //get sane item for item rename
            string baseItemName = newItemName.Replace(" ", "_").ToLower();  //get saner item name for commands
            Gungeon.Game.Items.Add(idPool + ":" + baseItemName, item);
            ETGMod.Databases.Items.Add(item);
            IDs.Actives[baseItemName] = item.PickupObjectId; //register item in active ID database
            IDs.Pickups[baseItemName] = item.PickupObjectId; //register item in pickup ID database

            ETGModConsole.Log("Lazy Initialized Active: "+baseItemName);
            return item;
        }

        /// <summary>
        /// Create a basic list of named directional animations given a list of animation names previously setup with SpriteBuilder.AddSpriteToCollection
        /// </summary>
        public static List<AIAnimator.NamedDirectionalAnimation> EasyNamedDirectionalAnimations(string[] animNameList)
        {
            var theList = new List<AIAnimator.NamedDirectionalAnimation>();
            for(int i = 0; i < animNameList.Count(); ++i)
            {
                string anim = animNameList[i];
                theList.Add(new AIAnimator.NamedDirectionalAnimation() {
                    name = anim,
                    anim = new DirectionalAnimation() {
                        Type = DirectionalAnimation.DirectionType.Single,
                        Prefix = anim,
                        AnimNames = new string[] {anim},
                        Flipped = new DirectionalAnimation.FlipType[]{DirectionalAnimation.FlipType.None}
                    }
                });
            }
            return theList;
        }
    }
}

