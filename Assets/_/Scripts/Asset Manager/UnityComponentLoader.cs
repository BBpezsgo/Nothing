using DataUtilities.ReadableFileFormat;

using System;

using Unity.Netcode;
using Unity.Netcode.Components;

using UnityEngine;

namespace AssetManager
{
    public abstract class UnityThingLoader
    {
        public abstract Type LoaderType { get; }
    }

    public abstract class UnityObjectLoader : UnityThingLoader
    {
        protected object DefaultValue { get; }

        public virtual object Load(Value data)
            => this.Load(data, DefaultValue);

        public abstract object Load(Value data, object @default);
        public abstract Value Save(object value);
    }

    public abstract class UnityComponentLoader : UnityThingLoader
    {
        public abstract void Load(GameObject prefab, Component component, Value data);
        public abstract Value Save(Component component);
    }

    public abstract class UnityObjectLoader<T> : UnityObjectLoader
    {
        public override Value Save(object value)
            => Save((T)value);

        public override object Load(Value data, object @default)
            => Load(data, (T)@default);
        public override object Load(Value data)
             => Load(data, (T)DefaultValue);

        public abstract T Load(Value data, T @default);
        public abstract Value Save(T value);
    }

    public abstract class UnityComponentLoader<T> : UnityComponentLoader where T : Component
    {
        public override void Load(GameObject prefab, Component component, Value data)
            => Load(prefab, (T)component, data);
        public override Value Save(Component component)
            => Save((T)component);

        public abstract void Load(GameObject prefab, T component, Value data);
        public abstract Value Save(T component);
    }

    public class Vector2Loader : UnityObjectLoader<Vector2>
    {
        public override Type LoaderType => typeof(Vector2);

        public override Vector2 Load(Value data, Vector2 @default)
        {
            Vector2 result = @default;

            if (data.Has("X"))
            { result.x = data["X"].Float ?? 0f; }
            else if (data.Has("x"))
            { result.x = data["x"].Float ?? 0f; }

            if (data.Has("Y"))
            { result.y = data["Y"].Float ?? 0f; }
            else if (data.Has("y"))
            { result.y = data["y"].Float ?? 0f; }

            return result;
        }

        public override Value Save(Vector2 value)
        {
            Value result = Value.Object();
            result["x"] = Value.Literal(value.x);
            result["y"] = Value.Literal(value.y);
            return result;
        }
    }

    public class Vector3Loader : UnityObjectLoader<Vector3>
    {
        public override Type LoaderType => typeof(Vector3);

        public override Vector3 Load(Value data, Vector3 @default)
        {
            Vector3 result = @default;

            if (data.Has("X"))
            { result.x = data["X"].Float ?? 0f; }
            else if (data.Has("x"))
            { result.x = data["x"].Float ?? 0f; }

            if (data.Has("Y"))
            { result.y = data["Y"].Float ?? 0f; }
            else if (data.Has("y"))
            { result.y = data["y"].Float ?? 0f; }

            if (data.Has("Z"))
            { result.z = data["Z"].Float ?? 0f; }
            else if (data.Has("z"))
            { result.z = data["z"].Float ?? 0f; }

            return result;
        }

        public override Value Save(Vector3 value)
        {
            Value result = Value.Object();
            result["x"] = Value.Literal(value.x);
            result["y"] = Value.Literal(value.y);
            result["z"] = Value.Literal(value.z);
            return result;
        }
    }

    public class BoundsLoader : UnityObjectLoader<Bounds>
    {
        public override Type LoaderType => typeof(Bounds);

        public override Bounds Load(Value data, Bounds @default)
        {
            Bounds result = @default;

            result.size = data["Size"].LoadObject<Vector3>(@default.size);
            result.center = data["Center"].LoadObject<Vector3>(@default.center);

            return result;
        }

        public override Value Save(Bounds value)
        {
            Value result = Value.Object();

            result["Size"] = value.size.SaveObject();
            result["Center"] = value.center.SaveObject();

            return result;
        }
    }

    public class BoxColliderLoader : UnityComponentLoader<BoxCollider>
    {
        public override Type LoaderType => typeof(BoxCollider);

        public override void Load(GameObject prefab, BoxCollider component, Value data)
        {
            component.center = data["Center"].LoadObject(component.center);
            component.size = data["Size"].LoadObject(component.size);

            component.contactOffset = data["ContactOffset"].Float ?? component.contactOffset;
            component.isTrigger = data["IsTrigger"].Bool ?? component.isTrigger;
        }

        public override Value Save(BoxCollider component)
        {
            Value result = Value.Object();

            result["Center"] = component.center.SaveObject();
            result["Size"] = component.size.SaveObject();

            result["ContactOffset"] = Value.Literal(component.contactOffset);
            result["IsTrigger"] = Value.Literal(component.isTrigger);

            return result;
        }
    }

    public class RigidbodyLoader : UnityComponentLoader<Rigidbody>
    {
        public override Type LoaderType => typeof(Rigidbody);

        public override void Load(GameObject prefab, Rigidbody component, Value data)
        {

        }

        public override Value Save(Rigidbody component)
        {
            Value result = Value.Object();

            return result;
        }
    }

    public class NetworkObjectLoader : UnityComponentLoader<NetworkObject>
    {
        public override Type LoaderType => typeof(NetworkObject);

        public override void Load(GameObject prefab, NetworkObject component, Value data)
        {

        }

        public override Value Save(NetworkObject component)
        {
            Value result = Value.Object();

            return result;
        }
    }

    public class NetworkTransformLoader : UnityComponentLoader<NetworkTransform>
    {
        public override Type LoaderType => typeof(NetworkTransform);

        public override void Load(GameObject prefab, NetworkTransform component, Value data)
        {
            component.InLocalSpace = data["InLocalSpace"].Bool ?? false;
            component.Interpolate = data["Interpolate"].Bool ?? false;
            component.SyncPositionX = data["SyncPositionX"].Bool ?? true;
            component.SyncPositionY = data["SyncPositionY"].Bool ?? false;
            component.SyncPositionZ = data["SyncPositionZ"].Bool ?? true;
            component.SyncRotAngleX = data["SyncRotAngleX"].Bool ?? false;
            component.SyncRotAngleY = data["SyncRotAngleY"].Bool ?? true;
            component.SyncRotAngleZ = data["SyncRotAngleZ"].Bool ?? false;
            component.SyncScaleX = data["SyncScaleX"].Bool ?? false;
            component.SyncScaleY = data["SyncScaleY"].Bool ?? false;
            component.SyncScaleZ = data["SyncScaleZ"].Bool ?? false;
        }

        public override Value Save(NetworkTransform component)
        {
            Value result = Value.Object();

            result["InLocalSpace"] = component.InLocalSpace;
            result["Interpolate"] = component.Interpolate;
            result["SyncPositionX"] = component.SyncPositionX;
            result["SyncPositionY"] = component.SyncPositionY;
            result["SyncPositionZ"] = component.SyncPositionZ;
            result["SyncRotAngleX"] = component.SyncRotAngleX;
            result["SyncRotAngleY"] = component.SyncRotAngleY;
            result["SyncRotAngleZ"] = component.SyncRotAngleZ;
            result["SyncScaleX"] = component.SyncScaleX;
            result["SyncScaleY"] = component.SyncScaleY;
            result["SyncScaleZ"] = component.SyncScaleZ;

            return result;
        }
    }
}
