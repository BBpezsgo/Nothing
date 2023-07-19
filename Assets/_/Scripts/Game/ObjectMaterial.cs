namespace Game.Components
{
    public interface IObjectMaterial
    {
        public string Material { get; }
        /// <summary>
        /// Defines the projectile bounciness.
        /// <br/>
        /// The lower the value, the smaller the angle of impact angle required for the projectiles to bounce off.
        /// <br/>
        /// 0 means projectiles can not bounce off.
        /// </summary>
        public float Hardness { get; }
        /// <summary>
        /// Defines how much exploison damage it absorbs (percent %)
        /// </summary>
        public float BlastAbsorptionCapacity { get; }
    }
}
