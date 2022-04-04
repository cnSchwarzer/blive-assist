using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI; 

namespace Utility {
    public class WebSpriteUtility {
        private static Dictionary<string, Sprite> spriteCache = new();

        private static IEnumerator LoadJpgPng(string url, Image image) {
            using var req = UnityWebRequestTexture.GetTexture(url, true);
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
                Debug.Log(req.error);
            var ret = DownloadHandlerTexture.GetContent(req);
            if (ret == null) {
                image.sprite = null;
            } else {
                image.sprite = Sprite.Create(ret, new Rect(0, 0, ret.width, ret.height), new Vector2(0.5f, 0.5f));
            }

            spriteCache[url] = image.sprite;
        }

        private static IEnumerator LoadConverted(string url, Image image) { 
            yield return LoadJpgPng($"{url}@240w_240h_1c_1s.png", image);
        }

        public static IEnumerator Load(string url, Image image) {
            if (spriteCache.ContainsKey(url)) {
                //Debug.Log($"Cache {url}");
                image.sprite = spriteCache[url];
            } else if (url.Contains(".webp") || url.Contains(".gif")) {
                //Debug.Log($"Load {url}");
                yield return LoadConverted(url, image);
            } else if (url.Contains(".jpg") || url.Contains(".png")) {
                //Debug.Log($"Load {url}");
                yield return LoadJpgPng(url, image);
            } else {
                Debug.Log($"Unsupported URL {url}");
            }
        }
    }
}