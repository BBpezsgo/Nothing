internal interface IDamagable
{
    public void Damage(float ammount);
}

internal interface IDetailedDamagable
{
    public void Damage(float ammount, Projectile source);
}