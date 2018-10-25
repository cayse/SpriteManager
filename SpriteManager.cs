using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace Shared
{
    /// <summary>
    /// Extensions for SpriteManager class
    /// </summary>
    public static class SpriteManagerExtensions
    {
        /// <summary>
        /// Load sprite by name into image.sprite
        /// </summary>
        /// <param name="image">Image</param>
        /// <param name="spriteName">sprite name w/o any folder</param>
        public static void LoadFromAtlas(this Image image, string spriteName)
        {
            SpriteManager.LoadFromAtlas(image, spriteName);
        }
    }

    /// <summary>
    /// Load and manages all sprites from atlases
    /// </summary>
    public class SpriteManager
    {
        private static SpriteManager _instance;
        /// <summary>
        /// Sprite name by actual Sprite collection
        /// </summary>
        private readonly Dictionary<string, Sprite> _sprites = new Dictionary<string, Sprite>();
        /// <summary>
        /// fast access sprite name to atlas collection
        /// </summary>
        private readonly Dictionary<string, SpriteAtlasInfo> _spritesToAtlases =
            new Dictionary<string, SpriteAtlasInfo>();

        private bool _isInited;

        /// <summary>
        /// Singleton constructor
        /// </summary>
        public SpriteManager()
        {
            _instance = this;
        }

        /// <summary>
        /// Load sprite by name into image.sprite
        /// </summary>
        /// <param name="image">Image</param>
        /// <param name="spriteName">sprite name w/o any folder</param>
        public static void LoadFromAtlas(Image image, string spriteName)
        {
            image.sprite = _instance.Get(spriteName);
        }

        /// <summary>
        /// Load all atlases from resources
        /// </summary>
        public void Initialize()
        {
            if (_isInited)
            {
                Debug.LogError("SpriteManager already inited");
                return;
            }

            var spriteAtlases = Resources.LoadAll<SpriteAtlas>("");
            foreach (var spriteAtlas in spriteAtlases) CollectAtlasInfo(spriteAtlas);

            Resources.UnloadUnusedAssets();

            _isInited = true;

            Debug.Log("SpriteManager initialized with " + spriteAtlases.Length + " atlases.");
        }

        /// <summary>
        /// return sprite by sprite name from atlas
        /// </summary>
        /// <param name="spriteName">sprite name w/o any folder</param>
        /// <returns></returns>
        public Sprite Get(string spriteName)
        {
            SpriteAtlasInfo atlasInfo;

            if (!_spritesToAtlases.TryGetValue(spriteName, out atlasInfo))
            {
                Debug.LogError("No atlas found for sprite '" + spriteName + "'!");
                return default(Sprite);
            }

            Sprite sprite;

            if (!_sprites.TryGetValue(spriteName, out sprite))
            {
                sprite = atlasInfo.GetSprite(spriteName);
                _sprites.Add(spriteName, sprite);
            }

            return sprite;
        }

        /// <summary>
        /// Return array of Vector2[3] where
        /// [0] is position of sprite on atlas as percent
        /// [1] is size of sprite on atlas as percent
        /// [2] is sprite rect on atlas as pixels
        /// </summary>
        /// <param name="spriteName">sprite name w/o any folder</param>
        /// <returns></returns>
        public Vector2[] GetSpriteUV(string spriteName)
        {
            var sprite = Get(spriteName);
            var texture = sprite.texture;
            var spriteRect = sprite.textureRect;
            var textureSize = new Vector2(texture.width, texture.height);
            var result = new[] {spriteRect.position / textureSize, spriteRect.size / textureSize, spriteRect.size};

            return result;
        }

        /// <summary>
        /// Clear atlases and sprites
        /// </summary>
        public void ClearCache()
        {
            var spritesBeforeCount = _sprites.Count;

            foreach (var info in _spritesToAtlases.Values)
                info.ClearAtlas();

            _sprites.Clear();

            Debug.Log("SpriteManager cleared " + spritesBeforeCount + " sprites.");
        }

        /// <summary>
        /// Fill collection by atlas sprites
        /// </summary>
        /// <param name="atlas"></param>
        private void CollectAtlasInfo(SpriteAtlas atlas)
        {
            var spriteClones = new Sprite[atlas.spriteCount];

            atlas.GetSprites(spriteClones);

            foreach (var spriteClone in spriteClones)
            {
                var spriteName = Regex.Replace(spriteClone.name, @"(\(Clone\))$", string.Empty);

                if (_spritesToAtlases.ContainsKey(spriteName))
                    Debug.LogError("SpriteManager already contains sprite named '" + spriteName +
                                   "'! Unique name required! Skipping it...");
                else
                    _spritesToAtlases.Add(spriteName, new SpriteAtlasInfo(atlas.name));
            }
        }

        /// <summary>
        /// atlas name to spriteatlas pair
        /// </summary>
        private class SpriteAtlasInfo
        {
            private readonly string _atlasName;
            private SpriteAtlas _atlas;
            
            /// <summary>
            /// init info w/o atlas loaded
            /// </summary>
            /// <param name="atlasName">atlas name</param>
            public SpriteAtlasInfo(string atlasName)
            {
                _atlasName = atlasName;
            }

            /// <summary>
            /// Return sprite by sprite name
            /// Load atlas on first call
            /// </summary>
            /// <param name="spriteName">sprite name w/o folders</param>
            /// <returns></returns>
            public Sprite GetSprite(string spriteName)
            {
                if (_atlas == null)
                    _atlas = Resources.Load<SpriteAtlas>(_atlasName);

                return _atlas.GetSprite(spriteName);
            }

            /// <summary>
            /// Clear atlas ref
            /// </summary>
            public void ClearAtlas()
            {
                _atlas = null;
            }
        }
    }
}