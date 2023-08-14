using Unity.Netcode;

namespace Game.Blueprints.Component
{
    public class BlueprintInstance : NetworkBehaviour
    {
        internal Blueprint Blueprint;

        protected override void OnSynchronize<T>(ref BufferSerializer<T> serializer)
        {
            base.OnSynchronize(ref serializer);

            if (serializer.IsReader)
            {
                FastBufferReader reader = serializer.GetFastBufferReader();
                reader.ReadValue(out Blueprint);

                BlueprintManager.InstantiateBlueprint(Blueprint, gameObject);
            }
            else if (serializer.IsWriter)
            {
                FastBufferWriter writer = serializer.GetFastBufferWriter();

                writer.WriteValue(Blueprint);
            }
        }
    }
}
