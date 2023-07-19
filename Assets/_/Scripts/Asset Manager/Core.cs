using DataUtilities.ReadableFileFormat;

using System;

namespace AssetManager
{
    struct DownloadProgress
    {
        const int SPEED_HISTORY = 20;

        internal float Progress;
        internal float Speed;
        internal float SpeedAvg;
        internal float RemaingSecs;
        internal TimeSpan RemaingTime;

        float[] LastSpeeds;

        public void OnProgress(Networking.Network.ChunkCollector chunkCollector)
        {
            LastSpeeds ??= new float[SPEED_HISTORY];

            for (int i = 1; i < LastSpeeds.Length; i++)
            { LastSpeeds[i - 1] = LastSpeeds[i]; }
            LastSpeeds[^1] = chunkCollector.Speed;

            Progress = chunkCollector.Progress;
            Speed = chunkCollector.Speed;

            {
                float totalSpeed = 0f;
                for (int i = 0; i < LastSpeeds.Length; i++)
                { totalSpeed += LastSpeeds[i]; }
                SpeedAvg = totalSpeed / LastSpeeds.Length;
            }

            int remaingBytes = chunkCollector.ExpectedSize - chunkCollector.TotalReceivedBytes;
            if (SpeedAvg == 0f)
            {
                RemaingSecs = float.MaxValue;
                RemaingTime = TimeSpan.MaxValue;
            }
            else
            {
                RemaingSecs = remaingBytes / SpeedAvg;

                if (RemaingSecs >= TimeSpan.MaxValue.TotalSeconds)
                { RemaingTime = TimeSpan.MaxValue; }
                else
                { RemaingTime = TimeSpan.FromSeconds(RemaingSecs); }
            }
        }
    }

    public interface ICanLoadAsset : IHaveAssetFields
    {
        public void LoadAsset(Value data);
    }

    public interface IHaveAssetFields
    { }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class AssetFieldAttribute : Attribute
    { }

    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class AssetPropertyAttribute : Attribute
    { }
}
