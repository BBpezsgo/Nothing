using DataUtilities.Serializer;

using Networking.Messages;

using Unity.Netcode;

using UnityEngine;

namespace Networking.Components
{
    [AddComponentMenu("Netcode/Transform View")]
    public class NetcodeTransfowmView : MonoBehaviour, INetworkObservable
    {
        [SerializeField, ReadOnly] Rigidbody rb;
        [SerializeField, ReadOnly] NetcodeView netcodeView;

        [Header("Settings")]
        [SerializeField] internal bool UseLocal = false;

        [Header("Settings - Position")]
        [SerializeField, ReadOnly] internal bool SynchronizePosition;
        [SerializeField] internal bool SynchronizeXPosition = false;
        [SerializeField] internal bool SynchronizeYPosition = false;
        [SerializeField] internal bool SynchronizeZPosition = false;

        [Header("Settings - Rotation")]
        [SerializeField, ReadOnly] internal bool SynchronizeRotation;
        [SerializeField] internal bool SynchronizeXRotation = false;
        [SerializeField] internal bool SynchronizeYRotation = false;
        [SerializeField] internal bool SynchronizeZRotation = false;

        bool FirstTake = false;
        bool FirstEnabled = true;

        [Header("Debug - Position")]
        [SerializeField, ReadOnly] Vector3 NetworkPosition;
        // [Space(4)]
        // [SerializeField, ReadOnly] float Distance;
        // [Tooltip("Calculated from the previous and current position")]
        // [SerializeField, ReadOnly] Vector3 Direction;
        // [SerializeField, ReadOnly] Vector3 OldPosition;

        [Header("Debug - Rotation")]
        [SerializeField, ReadOnly] Vector3 NetworkRotation;
        // [Space(4)]
        // [SerializeField, ReadOnly] float Angle;


        const float ByteRotationMagicNumber = 1.41176470588f;

        void Setup()
        {
            rb = GetComponent<Rigidbody>();
            netcodeView = GetComponent<NetcodeView>();

            if (netcodeView == null)
            { netcodeView = GetComponentInParent<NetcodeView>(); }

            if (netcodeView == null)
            { Debug.LogWarning($"[{nameof(NetcodeTransfowmView)}]: No NetcodeView", this); }

            NetworkPosition = UseLocal ? transform.localPosition : transform.position;
            NetworkRotation = UseLocal ? transform.localRotation.eulerAngles : transform.rotation.eulerAngles;

            SynchronizePosition = SynchronizeXPosition || SynchronizeYPosition || SynchronizeZPosition;
            SynchronizeRotation = SynchronizeXRotation || SynchronizeYRotation || SynchronizeZRotation;
        }

        void Awake()
        {
            FirstEnabled = true;
            Setup();
        }

        void OnEnable()
        {
            FirstTake = true;
            FirstEnabled = true;
            Setup();
        }

        void FixedUpdate()
        {
            if (netcodeView == null) return;
            if (NetworkManager.Singleton == null) return;
            if (FirstEnabled)
            {
                // OldPosition = UseLocal ? transform.localPosition : transform.position;
                // NetworkPosition = Vector3.zero;
                // NetworkRotation = Vector3.zero;
                FirstEnabled = false;
            }

            if (rb != null)
            {
                // rb.freezeRotation = false;
            }
        }

        void Update()
        {
            if (netcodeView == null) return;
            if (NetworkManager.Singleton == null) return;
            if (!NetworkManager.Singleton.IsListening) return;
            if (FirstEnabled) return;

            if (!NetworkManager.Singleton.IsServer)
            {
                Transform tr = transform;

                if (UseLocal)
                {
                    if (SynchronizePosition)
                    {
                        float positionDifference = (tr.localPosition - NetworkPosition).magnitude;
                        tr.localPosition = LerpPosition(tr.localPosition, NetworkPosition, positionDifference * Time.deltaTime);
                    }

                    if (SynchronizeRotation)
                    {
                        tr.localRotation = LerpRotation(tr.localRotation, NetworkRotation, Time.deltaTime);
                    }
                }
                else
                {
                    if (SynchronizePosition)
                    {
                        float positionDifference = (tr.position - NetworkPosition).magnitude;
                        tr.position = LerpPosition(tr.position, NetworkPosition, positionDifference * Time.deltaTime);
                    }

                    if (SynchronizeRotation)
                    {
                        tr.rotation = LerpRotation(tr.rotation, NetworkRotation, Time.deltaTime);
                    }
                }
            }
        }

        Quaternion LerpRotation(Quaternion current, Vector3 target, float delta)
        {
            Vector3 r = LerpRotation(current.eulerAngles, target, delta);
            return Quaternion.Euler(r);
        }
        Vector3 LerpRotation(Vector3 current, Vector3 target, float delta)
        {
            if (!SynchronizeXRotation) target.x = current.x;
            if (!SynchronizeYRotation) target.y = current.y;
            if (!SynchronizeZRotation) target.z = current.z;

            return Vector3.RotateTowards(current, target, delta, 0f);
        }
        Vector3 LerpPosition(Vector3 current, Vector3 target, float delta)
        {
            if (!SynchronizeXPosition) target.x = current.x;
            if (!SynchronizeYPosition) target.y = current.y;
            if (!SynchronizeZPosition) target.z = current.z;

            return Vector3.MoveTowards(current, target, delta);
        }

        void INetworkObservable.OnRPC(RpcHeader header)
        { }

        static byte AngleToByte(float angle) => (byte)Mathf.Clamp(((angle + 360f) % 360f) / ByteRotationMagicNumber, 0f, 255f);

        void INetworkObservable.OnSerializeView(Deserializer deserializer, Serializer serializer, NetcodeMessageInfo messageInfo)
        {
            if (messageInfo.IsWriting)
            {
                Transform tr = transform;

                if (SynchronizePosition)
                {
                    Vector3 position = UseLocal ? tr.localPosition : tr.position;

                    if (SynchronizeXPosition) serializer.Serialize((short)position.x);
                    if (SynchronizeYPosition) serializer.Serialize((short)position.y);
                    if (SynchronizeZPosition) serializer.Serialize((short)position.z);
                }

                if (SynchronizeRotation)
                {
                    Vector3 rotation = UseLocal ? tr.localRotation.eulerAngles : tr.rotation.eulerAngles;

                    if (SynchronizeXRotation) serializer.Serialize(AngleToByte(rotation.x));
                    if (SynchronizeYRotation) serializer.Serialize(AngleToByte(rotation.y));
                    if (SynchronizeZRotation) serializer.Serialize(AngleToByte(rotation.z));
                }
            }
            else
            {
                Transform tr = transform;

                if (SynchronizePosition)
                {
                    Vector3 serverPosition = UseLocal ? tr.localPosition : tr.position;

                    if (SynchronizeXPosition) serverPosition.x = deserializer.DeserializeInt16();
                    if (SynchronizeYPosition) serverPosition.y = deserializer.DeserializeInt16();
                    if (SynchronizeZPosition) serverPosition.z = deserializer.DeserializeInt16();

                    NetworkPosition = serverPosition;

                    if (FirstTake)
                    {
                        if (UseLocal)
                        { tr.localPosition = NetworkPosition; }
                        else
                        { tr.position = NetworkPosition; }
                    }

                }

                if (SynchronizeRotation)
                {
                    Vector3 serverRotation = UseLocal ? tr.localRotation.eulerAngles : tr.rotation.eulerAngles;

                    if (SynchronizeXRotation) serverRotation.x = deserializer.DeserializeByte() * ByteRotationMagicNumber;
                    if (SynchronizeYRotation) serverRotation.y = deserializer.DeserializeByte() * ByteRotationMagicNumber;
                    if (SynchronizeZRotation) serverRotation.z = deserializer.DeserializeByte() * ByteRotationMagicNumber;

                    NetworkRotation = serverRotation;

                    if (FirstTake)
                    {
                        if (UseLocal)
                        { tr.localRotation = Quaternion.Euler(NetworkRotation); }
                        else
                        { tr.rotation = Quaternion.Euler(NetworkRotation); }
                    }
                }

                if (FirstTake)
                { FirstTake = false; }
            }
        }
    }
}
