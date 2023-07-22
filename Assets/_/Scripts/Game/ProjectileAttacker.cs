using AssetManager;

using System.Collections.Generic;

using UnityEngine;

using Utilities;

namespace Game.Components
{
    internal class ProjectileAttacker : AttackerBase, IHaveAssetFields
    {
        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            if (turret != null)
            {
                turret.LoseTarget();
                turret.RequiedProjectileLifetime = -1f;
            }

            if (BaseObject is ICanTakeControl canTakeControl && canTakeControl.AnybodyControllingThis())
            { return; }

            List<Projectile> projectilesList = new(RegisteredObjects.Projectiles);
            projectilesList.RemoveAll(projectile =>
            {
                if (projectile == null) return true;
                if (projectile.Rigidbody == null) return true;
                if (projectile.OwnerTeamHash == TeamHash) return true;
                if (projectile.Owner == BaseObject) return true;
                return false;
            });
            Projectile[] projectiles = projectilesList.ToArray();

            System.Array.Sort(projectiles, (a, b) =>
            {
                // var rbA = a.Rigidbody;
                // var rbB = b.Rigidbody;

                Vector3 diffA = a.Position - transform.position;
                Vector3 diffB = b.Position - transform.position;

                // float dot = Vector2.Dot(diffA.normalized.To2D(), rbA.velocity.To2D());
                // if (dot > -0.9f) continue;

                return (int)(diffA.sqrMagnitude - diffB.sqrMagnitude);
            });

            for (int i = 0; i < projectiles.Length; i++)
            {
                var result = ThinkOnTarget(projectiles[i]);
                if (result) return;
            }

            turret.PrepareShooting = false;
        }

        bool ThinkOnTarget(Projectile projectile)
        {
            RequiredShoots requiredShoots = null;
            if (projectile.TryGetComponent(out requiredShoots) && requiredShoots.EstimatedHP < -2f)
            {
                if ((projectile.Position - transform.position).sqrMagnitude >= (5f * 5f))
                { return false; }
            }

            float? angle_;
            float? t;
            Vector3 projPosition;

            using (ProfilerMarkers.TrajectoryMath.Auto())
            {
                Projectile.Trajectory shot = projectile.Shot;
                float v = turret.projectileVelocity * .95f;

                float lifetime = projectile.Lifetime + Time.fixedDeltaTime;

                var projectileTOF = Ballistics.CalculateTime(shot.Velocity, shot.ShootAngle * Mathf.Deg2Rad, shot.ShootPosition.y);

                if (projectileTOF.HasValue && (projectileTOF - lifetime) < .5f)
                {
                    // Debug3D.DrawSphere(projectile.Position, 3f, Color.magenta, 5f);
                    return false;
                }

                projPosition = shot.Position(lifetime);

                float d = Vector2.Distance(turret.ShootPosition.To2D(), projPosition.To2D());

                angle_ = Ballistics.AngleOfReach2(v, turret.ShootPosition, projPosition);

                t = angle_.HasValue ? Ballistics.TimeToReachDistance(v, angle_.Value, d) : null;

                for (int i = 0; i < 3; i++)
                {
                    if (!angle_.HasValue) break;
                    if (!t.HasValue) break;

                    projPosition = shot.Position(lifetime + t.Value);

                    d = Vector2.Distance(turret.ShootPosition.To2D(), projPosition.To2D());

                    angle_ = Ballistics.AngleOfReach2(v, turret.ShootPosition, projPosition);

                    t = angle_.HasValue ? Ballistics.TimeToReachDistance(v, angle_.Value, d) : null;
                }
            }

            // Debug.DrawLine(projectile.Position, projPosition, Color.red, Time.fixedDeltaTime, false);

            if (angle_.HasValue && t.HasValue)
            {
                Debug3D.DrawSphere(projPosition, 2f, Color.red, t ?? Time.fixedDeltaTime);

                // Vector3 predictedTargetPos = turret.ShootPosition + (Quaternion.Euler(-angle_.Value * Mathf.Rad2Deg, 0f, 0f) * Vector3.forward) * 5f;

                // Debug.DrawLine(turret.ShootPosition, predictedTargetPos, Color.white, Time.fixedDeltaTime, false);

                // turret.Input = new Vector2(0f, angle_.Value * Mathf.Rad2Deg); // (turret.ShootPosition + (Quaternion.Euler(-angle_.Value * Mathf.Rad2Deg, 0f, 0f) * Vector3.forward) * 5f);
                turret.RequiedProjectileLifetime = t.Value - Time.fixedDeltaTime;
                turret.SetTarget(projPosition);
            }
            else
            {
                turret.Input = Vector3.zero;
                turret.SetTarget(Vector3.zero);
            }


            // turret.target = transform.position + relativePosition;

            turret.LoseTarget();

            if (turret.IsAccurateShoot && NetcodeUtils.IsOfflineOrServer)
            {
                if (t.HasValue && requiredShoots != null)
                { turret.Shoot(requiredShoots, t.Value); }
                else
                { turret.Shoot(); }
            }

            turret.PrepareShooting = true;
            return true;
        }
    }
}
