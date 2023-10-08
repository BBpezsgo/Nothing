namespace Game.Components
{
    public interface IDamagable
    {
        public void Damage(float ammount);
    }

    public interface IDetailedDamagable
    {
        public void Damage(float ammount, Projectile source);
    }
}
