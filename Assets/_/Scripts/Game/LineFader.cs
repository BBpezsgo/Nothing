using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Components
{
    public class LineFader : MonoBehaviour
    {
        [SerializeField, ReadOnly] LineRenderer LineRenderer;
        [SerializeField] float Time = 1f;
        [SerializeField, ReadOnly] float currentTime;

        [SerializeField, ReadOnly, ColorUsage(true, true)] Color originalStartColor;
        [SerializeField, ReadOnly, ColorUsage(true, true)] Color originalEndColor;

        [SerializeField, ReadOnly, ColorUsage(true, true)] Color transparentStartColor;
        [SerializeField, ReadOnly, ColorUsage(true, true)] Color transparentEndColor;

        void Start()
        {
            LineRenderer = GetComponent<LineRenderer>();
        }

        void OnEnable()
        {
            originalStartColor = LineRenderer.startColor;
            originalEndColor = LineRenderer.endColor;

            transparentStartColor = new Color(LineRenderer.startColor.r, LineRenderer.startColor.g, LineRenderer.startColor.b, 0f);
            transparentEndColor = new Color(LineRenderer.endColor.r, LineRenderer.endColor.g, LineRenderer.endColor.b, 0f);

            currentTime = 0f;
            LineRenderer.widthMultiplier = 1f;
        }

        void FixedUpdate()
        {
            currentTime += UnityEngine.Time.fixedDeltaTime;

            float t = System.Math.Clamp(currentTime, 0f, Time) / Time;
            LineRenderer.startColor = Color.Lerp(originalStartColor, transparentStartColor, t);
            LineRenderer.endColor = Color.Lerp(originalEndColor, transparentEndColor, t);
            LineRenderer.widthMultiplier = 1f - t;

            if (currentTime >= Time)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
