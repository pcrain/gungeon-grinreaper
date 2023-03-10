using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

using UnityEngine;
using Gungeon;
using ItemAPI;

namespace TheGrinReaper
{
    public static class VFX
    {
        private const int PIXELS_ABOVE_HEAD = 2;

        private static GameObject VFXScapegoat;
        private static tk2dSpriteCollectionData OverheadVFXCollection;
        private static Dictionary<GameActor,List<GameObject>> extantSprites;

        public static Dictionary<string,int> sprites;
        public static Dictionary<string,GameObject> animations;
        public static Dictionary<string,VFXPool> vfxpool;
        public static Dictionary<string,VFXComplex> vfxcomplex;

        private static Dictionary<GameObject,VFXPool> vfxObjectToPoolMap;

        public static void Init()
        {
            sprites               = new Dictionary<string,int>();
            extantSprites         = new Dictionary<GameActor,List<GameObject>>();
            animations            = new Dictionary<string,GameObject>();
            vfxpool               = new Dictionary<string,VFXPool>();
            vfxcomplex            = new Dictionary<string,VFXComplex>();
            vfxObjectToPoolMap    = new Dictionary<GameObject,VFXPool>();

            VFXScapegoat          = new GameObject();
            OverheadVFXCollection = SpriteBuilder.ConstructCollection(VFXScapegoat, "OverheadVFX_Collection");
            UnityEngine.Object.DontDestroyOnLoad(VFXScapegoat);
            UnityEngine.Object.DontDestroyOnLoad(OverheadVFXCollection);
        }

        /// <summary>
        /// Register a single-frame static sprite
        /// </summary>
        private static void RegisterSprite(string path)
        {
            sprites[path.Substring(path.LastIndexOf("/")+1)] = SpriteBuilder.AddSpriteToCollection(path, OverheadVFXCollection);
        }

        /// <summary>
        /// Generically register a VFX as a GameObject (animated sprite), VFXComplex, or VFXPool
        /// </summary>
        public static void RegisterVFX<T>(string name, List<string> spritePaths, int fps, bool loops = true, tk2dBaseSprite.Anchor anchor = tk2dBaseSprite.Anchor.MiddleCenter, IntVector2? dimensions = null, bool usesZHeight = false, float zHeightOffset = 0, bool persist = false, VFXAlignment alignment = VFXAlignment.NormalAligned, float emissivePower = -1, Color? emissiveColour = null)
        {
            // GameObject Obj     = new GameObject(name);
            GameObject Obj     = SpriteBuilder.SpriteFromResource(spritePaths[0], new GameObject(name));
            VFXComplex complex = new VFXComplex();
            VFXObject vfObj    = new VFXObject();
            VFXPool pool       = new VFXPool();
            pool.type          = VFXPoolType.All;
            Obj.SetActive(false);
            FakePrefab.MarkAsFakePrefab(Obj);
            UnityEngine.Object.DontDestroyOnLoad(Obj);

            tk2dBaseSprite baseSprite = Obj.GetComponent<tk2dBaseSprite>();
            tk2dSpriteDefinition baseDef = baseSprite.GetCurrentSpriteDef();
            baseDef.ConstructOffsetsFromAnchor(
                tk2dBaseSprite.Anchor.LowerCenter,
                baseDef.position3);

            tk2dSpriteCollectionData VFXSpriteCollection = SpriteBuilder.ConstructCollection(Obj, (name + "_Pool"));
            int spriteID = SpriteBuilder.AddSpriteToCollection(spritePaths[0], VFXSpriteCollection);

            tk2dSprite sprite = Obj.GetOrAddComponent<tk2dSprite>();
            sprite.SetSprite(VFXSpriteCollection, spriteID);
            tk2dSpriteDefinition defaultDef = sprite.GetCurrentSpriteDef();

            if (dimensions is IntVector2 dims)
            {
                defaultDef.colliderVertices = new Vector3[]{
                          new Vector3(0f, 0f, 0f),
                          new Vector3((dims.x / C.PIXELS_PER_TILE), (dims.y / C.PIXELS_PER_TILE), 0f)
                      };
            }
            else
            {
                defaultDef.colliderVertices = new Vector3[]{
                          new Vector3(0f, 0f, 0f),
                          new Vector3(
                            baseSprite.GetCurrentSpriteDef().position3.x / C.PIXELS_PER_TILE,
                            baseSprite.GetCurrentSpriteDef().position3.y / C.PIXELS_PER_TILE,
                            0f)
                      };
            }

            tk2dSpriteAnimator animator           = Obj.GetOrAddComponent<tk2dSpriteAnimator>();
            tk2dSpriteAnimation animation         = Obj.GetOrAddComponent<tk2dSpriteAnimation>();
            animation.clips                       = new tk2dSpriteAnimationClip[0];
            animator.Library                      = animation;
            tk2dSpriteAnimationClip clip          = new tk2dSpriteAnimationClip() { name = "start", frames = new tk2dSpriteAnimationFrame[0], fps = fps };
            List<tk2dSpriteAnimationFrame> frames = new List<tk2dSpriteAnimationFrame>();
            for (int i = 0; i < spritePaths.Count; i++)
            {
                tk2dSpriteCollectionData collection = VFXSpriteCollection;
                int frameSpriteId                   = SpriteBuilder.AddSpriteToCollection(spritePaths[i], collection);
                tk2dSpriteDefinition frameDef       = collection.spriteDefinitions[frameSpriteId];
                frameDef.ConstructOffsetsFromAnchor(anchor);
                frameDef.colliderVertices = defaultDef.colliderVertices;
                if (emissivePower > 0) {
                    frameDef.material.shader = ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTintableTiltedCutoutEmissive");
                    frameDef.material.SetFloat("_EmissivePower", emissivePower);
                    frameDef.material.SetFloat("_EmissiveColorPower", 1.55f);
                    frameDef.materialInst.shader = ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTintableTiltedCutoutEmissive");
                    frameDef.materialInst.SetFloat("_EmissivePower", emissivePower);
                    frameDef.materialInst.SetFloat("_EmissiveColorPower", 1.55f);
                }
                if (emissiveColour != null)
                {
                    frameDef.material.SetColor("_EmissiveColor", (Color)emissiveColour);
                    frameDef.materialInst.SetColor("_EmissiveColor", (Color)emissiveColour);
                }
                frames.Add(new tk2dSpriteAnimationFrame { spriteId = frameSpriteId, spriteCollection = collection });
            }
            if (emissivePower > 0) {
                sprite.renderer.material.shader = ShaderCache.Acquire("Brave/LitTk2dCustomFalloffTintableTiltedCutoutEmissive");
                sprite.renderer.material.SetFloat("_EmissivePower", emissivePower);
                sprite.renderer.material.SetFloat("_EmissiveColorPower", 1.55f);
            }
            if (emissiveColour != null)
                sprite.renderer.material.SetColor("_EmissiveColor", (Color)emissiveColour);
            clip.frames     = frames.ToArray();
            clip.wrapMode   = loops ? tk2dSpriteAnimationClip.WrapMode.Loop : tk2dSpriteAnimationClip.WrapMode.Once;
            animation.clips = animation.clips.Concat(new tk2dSpriteAnimationClip[] { clip }).ToArray();
            if (!persist)
            {
                SpriteAnimatorKiller kill = animator.gameObject.AddComponent<SpriteAnimatorKiller>();
                kill.fadeTime = -1f;
                kill.animator = animator;
                kill.delayDestructionTime = -1f;
            }
            animator.playAutomatically = true;
            animator.DefaultClipId     = animator.GetClipIdByName("start");
            vfObj.attached             = true;
            vfObj.persistsOnDeath      = persist;
            vfObj.usesZHeight          = usesZHeight;
            vfObj.zHeight              = zHeightOffset;
            vfObj.alignment            = alignment;
            vfObj.destructible         = false;

            vfObj.effect               = Obj;
            complex.effects            = new VFXObject[] { vfObj };
            pool.effects               = new VFXComplex[] { complex };

            Type genericType = typeof(T);

            if(genericType == typeof(VFXPool))         vfxpool[name]    = pool;
            else if(genericType == typeof(VFXComplex)) vfxcomplex[name] = complex;
            else if(genericType == typeof(GameObject)) animations[name] = Obj;
        }

        public static void ShowOverheadVFX(this GameActor gunOwner, string name, float timeout)
        {
            gunOwner.StartCoroutine(ShowVFXCoroutine(gunOwner, name, timeout));
        }

        public static void ShowOverheadAnimatedVFX(this GameActor gunOwner, string name, float timeout)
        {
            gunOwner.StartCoroutine(ShowAnimatedVFXCoroutine(gunOwner, name, timeout));
        }

        /// <summary>
        /// Spawn prefabricated vfx, optionally locked relative to a gameobject's position
        /// </summary>
        public static void SpawnVFXPool(string name, Vector2 position, bool above = false, float degAngle = 0, GameObject relativeTo = null)
        {
            SpawnVFXPool(VFX.vfxpool[name], position, above, degAngle, relativeTo);
        }

        public static void SpawnVFXPool(VFXPool vfx, Vector2 position, bool above = false, float degAngle = 0, GameObject relativeTo = null)
        {
            Transform t = (relativeTo != null) ? relativeTo.transform : null;
            vfx.SpawnAtPosition(
                position.ToVector3ZisY(above ? -1f : 1f), /* -1 = above player sprite */
                degAngle, t, null, null, -0.05f);
        }

        public static void SpawnVFXPool(GameObject vfx, Vector2 position, bool above = false, float degAngle = 0, GameObject relativeTo = null)
        {

            SpawnVFXPool(CreatePoolFromVFXGameObject(vfx), position, above, degAngle, relativeTo);
        }

        public static VFXPool CreatePoolFromVFXGameObject(GameObject vfx)
        {
            if (!(vfxObjectToPoolMap.ContainsKey(vfx)))
            {
                VFXObject vfObj         = new VFXObject();
                vfObj.attached          = false;
                vfObj.persistsOnDeath   = false;
                vfObj.usesZHeight       = false;
                vfObj.zHeight           = 0;
                vfObj.alignment         = VFXAlignment.NormalAligned;
                vfObj.destructible      = false;
                vfObj.effect            = vfx;

                VFXComplex complex      = new VFXComplex();
                complex.effects         = new VFXObject[] { vfObj };

                VFXPool pool            = new VFXPool();
                pool.type               = VFXPoolType.All;
                pool.effects            = new VFXComplex[] { complex };

                vfxObjectToPoolMap[vfx] = pool;
            }
            return vfxObjectToPoolMap[vfx];
        }

        private static IEnumerator ShowVFXCoroutine(this GameActor gunOwner, string name, float timeout)
        {
            if (!(extantSprites.ContainsKey(gunOwner)))
                extantSprites[gunOwner] = new List<GameObject>();
            gunOwner.StopAllOverheadVFX();
            GameObject newSprite = new GameObject(name, new Type[] { typeof(tk2dSprite) }) { layer = 0 };
            // newSprite.transform.position = (gunOwner.transform.position + new Vector3(0.5f, 2));
            newSprite.transform.position = new Vector3(
                gunOwner.sprite.WorldCenter.x,
                gunOwner.sprite.WorldTopCenter.y + PIXELS_ABOVE_HEAD/C.PIXELS_PER_TILE);
            tk2dSprite overheadSprite = newSprite.AddComponent<tk2dSprite>();
            extantSprites[gunOwner].Add(newSprite);
            overheadSprite.SetSprite(OverheadVFXCollection, sprites[name]);
            overheadSprite.PlaceAtPositionByAnchor(newSprite.transform.position, tk2dBaseSprite.Anchor.LowerCenter);
            overheadSprite.transform.localPosition = overheadSprite.transform.localPosition.Quantize(0.0625f);
            newSprite.transform.parent = gunOwner.transform;
            if (overheadSprite)
            {
                gunOwner.sprite.AttachRenderer(overheadSprite);
                overheadSprite.depthUsesTrimmedBounds = true;
                overheadSprite.UpdateZDepth();
            }
            gunOwner.sprite.UpdateZDepth();
            if (timeout > 0)
            {
                yield return new WaitForSeconds(timeout);
                if (newSprite)
                {
                    extantSprites[gunOwner].Remove(newSprite);
                    UnityEngine.Object.Destroy(newSprite.gameObject);
                }
            }
            else {
                yield break;
            }
        }

        private static IEnumerator ShowAnimatedVFXCoroutine(this GameActor gunOwner, string name, float timeout)
        {
            if (!(extantSprites.ContainsKey(gunOwner)))
                extantSprites[gunOwner] = new List<GameObject>();
            gunOwner.StopAllOverheadVFX();

            GameObject newSprite = UnityEngine.Object.Instantiate<GameObject>(animations[name]);

            tk2dBaseSprite baseSprite = newSprite.GetComponent<tk2dBaseSprite>();
            newSprite.transform.parent = gunOwner.transform;
            newSprite.transform.position = new Vector3(
                gunOwner.sprite.WorldCenter.x,
                gunOwner.sprite.WorldTopCenter.y + PIXELS_ABOVE_HEAD/C.PIXELS_PER_TILE);

            extantSprites[gunOwner].Add(baseSprite.gameObject);

            Bounds bounds = gunOwner.sprite.GetBounds();
            Vector3 vector = gunOwner.transform.position + new Vector3((bounds.max.x + bounds.min.x) / 2f, bounds.max.y, 0f).Quantize(0.0625f);
            newSprite.transform.position = gunOwner.sprite.WorldCenter.ToVector3ZUp(0f).WithY(vector.y);
            baseSprite.HeightOffGround = 0.5f;

            gunOwner.sprite.AttachRenderer(baseSprite);

            if (timeout > 0)
            {
                yield return new WaitForSeconds(timeout);
                if (baseSprite)
                {
                    extantSprites[gunOwner].Remove(baseSprite.gameObject);
                    UnityEngine.Object.Destroy(baseSprite.gameObject);
                }
            }
            else {
                yield break;
            }
        }

        public static void StopAllOverheadVFX(this GameActor gunOwner)
        {
            if (extantSprites[gunOwner].Count > 0)
            {
                for (int i = extantSprites[gunOwner].Count - 1; i >= 0; i--)
                {
                    UnityEngine.Object.Destroy(extantSprites[gunOwner][i].gameObject);
                }
                extantSprites[gunOwner].Clear();
            }
        }
    }
}

