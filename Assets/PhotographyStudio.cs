using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

using UnityEngine;

namespace Game.Managers
{
    public class PhotographyStudio : MonoBehaviour
    {
        [SerializeField] PhotographyStudioObjects Objects;

        [SerializeField] bool SaveToResources;
        [SerializeField] string ResourcesPath;

        [SerializeField, ReadOnly] GameObject placedObject;
        [SerializeField] Vector3 _photographyPosition;
        [SerializeField, Min(0f)] float _photographySize = 1f;
        [SerializeField, Range(0, 1)] float TransparencyThreshold;

        [SerializeField] RenderTexture _photographyStudioRenderer;
        [SerializeField] Transform _photographyStudio;
        Camera Photographer => _photographyStudio.gameObject.GetComponentInChildren<Camera>();

        [SerializeField, Button(nameof(RenderPhotographyStudio), true, true, "Render")] string _btnRender;
        [SerializeField, Thumbnail(nameof(_photographyStudioRenderer))] Texture2D lastRender = null;

        void PlaceObject(GameObject renderThis)
        {
            bool lastEnabled = _photographyStudio.gameObject.activeSelf;
            if (!lastEnabled)
            { _photographyStudio.gameObject.SetActive(true); }

            if (placedObject != null)
            {
                if (Application.isPlaying)
                { Texture2D.Destroy(placedObject); }
                else
                { Texture2D.DestroyImmediate(placedObject); }
            }

            if (renderThis != null)
            {
                placedObject = GameObject.Instantiate(renderThis, Vector3.zero, Quaternion.Euler(0f, -220f, 0f), _photographyStudio);

                float baseTop = _photographyPosition.y - (_photographySize / 2);

                Bounds bounds = placedObject.GetRendererBounds();

                float scale = _photographySize / Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);

                placedObject.transform.localScale = scale * Vector3.one;

                bounds = placedObject.GetRendererBounds();

                placedObject.transform.position = new Vector3(_photographyPosition.x, baseTop + bounds.extents.y, _photographyPosition.z) - bounds.center;

                placedObject.SetLayerRecursive(_photographyStudio.gameObject.layer);
            }

            if (!lastEnabled)
            { Photographer.Render(); }

            if (!lastEnabled)
            { _photographyStudio.gameObject.SetActive(false); }
        }

        void RenderPhotographyStudio()
        {
            for (int i = 0; i < Objects.Objects.Length; i++)
            {
                var renderThis = Objects.Objects[i];
                PlaceObject(renderThis.Object);
                Save(renderThis.Name);
            }

#if UNITY_EDITOR
            if (SaveToResources)
            { UnityEditor.AssetDatabase.Refresh(); }
#endif
        }

        void Save(string name)
        {
            if (lastRender != null)
            {
                if (Application.isPlaying)
                { Texture2D.Destroy(lastRender); }
                else
                { Texture2D.DestroyImmediate(lastRender); }
            }

            RenderTexture oldRenderTexture = RenderTexture.active;
            RenderTexture.active = _photographyStudioRenderer;

            lastRender = new Texture2D(_photographyStudioRenderer.width, _photographyStudioRenderer.height, TextureFormat.RGBA32, false);

            lastRender.ReadPixels(new Rect(0, 0, _photographyStudioRenderer.width, _photographyStudioRenderer.height), 0, 0);

            MakeTransparent(lastRender, Photographer.backgroundColor, TransparencyThreshold);

            lastRender.Apply();

            RenderTexture.active = oldRenderTexture;

            string path;

            if (SaveToResources)
            {
                if (string.IsNullOrWhiteSpace(ResourcesPath))
                { path = Path.Combine(EditorUtils.ResourcesPath, $"{name}.png"); }
                else
                { path = Path.Combine(EditorUtils.ResourcesPath, ResourcesPath, $"{name}.png"); }
            }
            else
            { path = $@"C:\Users\bazsi\Desktop\Nothing Assets 3D\Photography Studio\{name}.png"; }

            File.WriteAllBytes(path, lastRender.EncodeToPNG());
        }

        static void MakeTransparent(Texture2D texture, Color transparentColor, float tolerance)
        {
            for (int y = 0; y < texture.height; y++)
            {
                for (int x = 0; x < texture.width; x++)
                {
                    Color pixel = texture.GetPixel(x, y);

                    float d;
                    {
                        Vector3 a = new(pixel.r, pixel.g, pixel.b);
                        Vector3 b = new(transparentColor.r, transparentColor.g, transparentColor.b);
                        d = Vector3.Distance(a, b);
                    }

                    if (d < tolerance)
                    {
                        float invertedD = 1f - Mathf.Clamp01(d);

                        Color newPixel = pixel; //Color.Lerp(pixel, new Color(1f, 0f, 1f, 0f), invertedD);
                        newPixel.a = Mathf.Lerp(pixel.a, 0f, invertedD);
                        texture.SetPixel(x, y, newPixel);
                    }
                }
            }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(_photographyPosition, Vector3.one * _photographySize);
        }
    }
}
