namespace Game.Components
{
    public interface IDamageable
    {
        public void Damage(float amount);
    }

    public interface IDetailedDamageable
    {
        public void Damage(float amount, Projectile source);
    }
}
