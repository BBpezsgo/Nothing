using System.Collections.Generic;
using System.Linq;

using UnityEngine;

namespace Game.Components
{
    internal class UnitItemTransporter : Unit
    {
        [SerializeField, ReadOnly, NonReorderable] List<Item> Targets = new();
        [SerializeField] float ReachDistance = 1f;
        [SerializeField] INeedItems CurrentItemNeeder = null;
        [SerializeField] Transform CargoPosition;
        [SerializeField] Item TransportingItem;

        INeedItems[] ItemNeeders = new INeedItems[0];

        [SerializeField, ReadOnly] float TimeToNextTargetSearch = 1f;
        [SerializeField, ReadOnly] float TimeToNextItemNeederSearch = 1f;

        void FindItems(string itemID)
        {
            Item[] items = FindObjectsOfType<Item>(false);
            Targets.Clear();
            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] == null)
                { continue; }
                if (itemID != null && items[i].ItemID != itemID)
                { continue; }
                if (items[i].IsPickedUp)
                { continue; }
                Targets.Add(items[i]);
            }
        }

        protected override void FixedUpdate()
        {
            base.FixedUpdate();
            if (this.AnybodyControllingThis())
            { return; }


            if (TryGetComponent(out UnitBehaviour_AvoidObstacles avoidObstacles))
            { avoidObstacles.IgnoreCollision = null; }

            if ((Targets.Count > 0 || TransportingItem != null) && (CurrentItemNeeder != null && (Object)CurrentItemNeeder != null))
            {
                DoTransport();
            }
            else
            { DoIdle(); }

            {
                if (TimeToNextItemNeederSearch > 0)
                {
                    TimeToNextItemNeederSearch -= Time.fixedDeltaTime;
                }
                else
                {
                    TimeToNextItemNeederSearch = 5f;

                    List<INeedItems> itemNeeders = new(GameObject.FindObjectsOfType<MonoBehaviour>(false).OfType<INeedItems>());
                    for (int i = itemNeeders.Count - 1; i >= 0; i--)
                    {
                        if (itemNeeders[i].Team != this.Team)
                        {
                            itemNeeders.RemoveAt(i);
                            continue;
                        }
                    }
                    this.ItemNeeders = itemNeeders.ToArray();
                }
            }
        }

        void DoTransport()
        {
            if (Targets.Count <= 0 && TransportingItem == null)
            { return; }

            if (TransportingItem != null)
            {
                if ((Object)CurrentItemNeeder == null)
                {
                    GameObject.Destroy(TransportingItem.gameObject);
                    return;
                }

                TransportingItem.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

                Transform itemNeeder = ((Component)CurrentItemNeeder).transform;

                if ((itemNeeder.position - transform.position).To2D().sqrMagnitude <= (ReachDistance * ReachDistance))
                {
                    CurrentItemNeeder.GiveItem(1f);

                    GameObject.Destroy(TransportingItem.gameObject);

                    if (TryGetComponent(out UnitBehaviour_Seek seek))
                    { seek.Target = Vector3.zero; }
                }
                else
                {
                    if (TryGetComponent(out UnitBehaviour_Seek seek))
                    { seek.Target = itemNeeder.position; }
                }

                if (TryGetComponent(out UnitBehaviour_AvoidObstacles avoidObstacles))
                { avoidObstacles.IgnoreCollision = itemNeeder; }
            }
            else
            {
                if (Targets[0].IsPickedUp)
                {
                    Targets.RemoveAt(0);
                    if (TryGetComponent(out UnitBehaviour_Seek seek))
                    { seek.Target = Vector3.zero; }
                }
                else if ((Targets[0].transform.position - transform.position).To2D().sqrMagnitude <= (ReachDistance * ReachDistance))
                {
                    TransportingItem = Targets[0];
                    Targets.RemoveAt(0);

                    TransportingItem.PickUp(CargoPosition);

                    if (TryGetComponent(out UnitBehaviour_Seek seek))
                    { seek.Target = Vector3.zero; }
                }
                else
                {
                    if (TryGetComponent(out UnitBehaviour_Seek seek))
                    { seek.Target = Targets[0].transform.position; }

                    if (TryGetComponent(out UnitBehaviour_AvoidObstacles avoidObstacles))
                    { avoidObstacles.IgnoreCollision = Targets[0].transform; }
                }
            }
        }

        void DoIdle()
        {
            if (TimeToNextTargetSearch > 0)
            {
                TimeToNextTargetSearch -= Time.fixedDeltaTime;
            }
            else
            {
                TimeToNextTargetSearch = 2f;

                for (int i = 0; i < ItemNeeders.Length; i++)
                {
                    if (!ItemNeeders[i].NeedItems)
                    { continue; }

                    string needItemID = ItemNeeders[i].ItemID;

                    FindItems(needItemID);

                    CurrentItemNeeder = ItemNeeders[i];

                    break;
                }
            }

            if (TryGetComponent(out UnitBehaviour_Seek seek))
            { seek.Target = Vector3.zero; }

            if (TryGetComponent(out UnitBehaviour_AvoidObstacles avoidObstacles))
            { avoidObstacles.IgnoreCollision = null; }
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(transform.position, ReachDistance);
        }
    }
}
