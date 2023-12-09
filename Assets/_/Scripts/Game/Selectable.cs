using Game.Managers;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Game.Components
{
    public class Selectable : MonoBehaviour
    {
        public enum State : int
        {
            None = 0,
            Almost = 1,
            Selected = 2,
        }

        [SerializeField] internal GameObject UiSelected;

        [SerializeField, ReadOnly] State selectableState = State.None;
        public State SelectableState
        {
            get => selectableState;
            set
            {
                if (this == null) return;

                selectableState = value;

                if (selectableState == State.None)
                { UiSelected.SetActive(false); }
                else
                {
                    UiSelected.SetActive(true);
                    if (selectableState == State.Almost)
                    { UiSelected.GetComponent<DecalProjector>().material.SetEmissionColor(SelectionManager.Instance.AlmostSelectedColor, 1f); }
                    else if (selectableState == State.Selected)
                    { UiSelected.GetComponent<DecalProjector>().material.SetEmissionColor(SelectionManager.Instance.SelectedColor, 1f); }
                }
            }
        }

        void Start()
        {
            if (UiSelected == null)
            {
                Debug.LogError($"[{nameof(Selectable)}]: {nameof(UiSelected)} is null", this);
                enabled = false;
            }
        }
    }
}
