using AssetManager;

using Unity.Netcode;

using UnityEngine;

internal class BuildingAttacker : Building, IDetailedDamagable, ISelectable, ICanTakeControlAndHasTurret
{
    public Turret Turret => Attacker.turret;
    [SerializeField, ReadOnly] ulong controllingByUser;
    public ulong ControllingByUser
    {
        get => controllingByUser;
        set => controllingByUser = value;
    }

    [SerializeField, AssetField] internal GameObject UiSelected;

    [SerializeField, ReadOnly] AttackerBase Attacker;


    ISelectable.State selectableState = ISelectable.State.None;
    public ISelectable.State SelectableState
    {
        get => selectableState;
        set
        {
            selectableState = value;
            if (selectableState == ISelectable.State.None)
            { UiSelected.SetActive(false); }
            else
            {
                UiSelected.SetActive(true);
                if (selectableState == ISelectable.State.Almost)
                { UiSelected.GetComponent<Renderer>().material.SetEmissionColor(SelectionManager.Instance.AlmostSelectedColor, 1f); }
                else if (selectableState == ISelectable.State.Selected)
                { UiSelected.GetComponent<Renderer>().material.SetEmissionColor(SelectionManager.Instance.SelectedColor, 1f); }
            }
        }
    }

    public virtual void DoInput() => Attacker.DoInput();
    public virtual void DoFrequentInput() => Attacker.DoFrequentInput();

    void Awake()
    {
        this.ControllingByUser = ulong.MaxValue;

        if (!TryGetComponent(out Attacker))
        { Debug.LogError($"[{nameof(BuildingAttacker)}]: No Attacker!"); }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (this.IAmControllingThis() && gameObject.scene.isLoaded)
        { TakeControlManager.Instance.ControllableDestroyed(); }
    }

    public void Damage(float ammount, Projectile source)
    {
        if (ammount > .0001f &&
            source != null &&
            source.Owner != null &&
            TeamManager.Instance.GetFuckYou(this, source.Owner) >= 0f &&
            Attacker is Attacker attacker)
        {
            attacker.SomeoneDamagedMe(source.Owner);
        }

        Damage(ammount);
    }
}
