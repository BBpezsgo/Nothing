using System.Collections.Generic;
using Maths;
using UnityEngine;

using Utilities;

namespace Game.Components
{
    internal class ProjectileAttacker : AttackerBase
    {
        protected override void Update()
        {
            base.Update();

            if (turret != null)
            {
                turret.LoseTarget();
                turret.RequiredProjectileLifetime = -1f;
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
                bool result = ThinkOnTarget(projectiles[i]);
                if (result) return;
            }

            turret.PrepareShooting = false;
        }

        bool ThinkOnTarget(Projectile projectile)
        {
            if (projectile.TryGetComponent(out RequiredShoots requiredShoots) && requiredShoots.EstimatedHP < -2f)
            {
                if ((projectile.Position - transform.position).sqrMagnitude >= (5f * 5f))
                { return false; }
            }

            (System.Numerics.Vector3 PredictedPosition, float TimeToReach)? predictedAim = Maths.Ballistics.CalculateInterceptCourse(turret.projectileVelocity, projectile.Lifetime, Turret.ShootPosition.To(), projectile.Shot);

            /*
            float? angle_;
            float? t;
            Vector3 projPosition;

            using (ProfilerMarkers.TrajectoryMath.Auto())
            {
                Projectile.Trajectory shot = projectile.Shot;
                float v = turret.projectileVelocity * .95f;

                float lifetime = projectile.Lifetime + Time.deltaTime;

                var projectileTOF = Ballistics.CalculateTime(shot.Velocity, shot.ShootAngle * Maths.Deg2Rad, shot.ShootPosition.y);

                if (projectileTOF.HasValue && (projectileTOF - lifetime) < .5f)
                {
                    // Debug3D.DrawSphere(projectile.Position, 3f, Color.magenta, 5f);
                    return false;
                }

                projPosition = shot.Position(lifetime);

                float d = Maths.Distance(turret.ShootPosition.To2D(), projPosition.To2D());

                angle_ = Ballistics.AngleOfReach2(v, turret.ShootPosition, projPosition);

                t = angle_.HasValue ? Ballistics.TimeToReachDistance(v, angle_.Value, d) : null;

                for (int i = 0; i < 3; i++)
                {
                    if (!angle_.HasValue) break;
                    if (!t.HasValue) break;

                    projPosition = shot.Position(lifetime + t.Value);

                    d = Maths.Distance(turret.ShootPosition.To2D(), projPosition.To2D());

                    angle_ = Ballistics.AngleOfReach2(v, turret.ShootPosition, projPosition);

                    t = angle_.HasValue ? Ballistics.TimeToReachDistance(v, angle_.Value, d) : null;
                }
            }
            */

            // Debug.DrawLine(projectile.Position, projPosition, Color.red, Time.deltaTime, false);

            if (predictedAim.HasValue)
            {
                // Debug3D.DrawSphere(predictedAim.Value.PredictedPosition, 2f, Color.red, predictedAim.Value.TimeToReach);

                // Vector3 predictedTargetPos = turret.ShootPosition + (Quaternion.Euler(-angle_.Value * Maths.Rad2Deg, 0f, 0f) * Vector3.forward) * 5f;

                // Debug.DrawLine(turret.ShootPosition, predictedTargetPos, Color.white, Time.deltaTime, false);

                // turret.Input = new Vector2(0f, angle_.Value * Maths.Rad2Deg); // (turret.ShootPosition + (Quaternion.Euler(-angle_.Value * Maths.Rad2Deg, 0f, 0f) * Vector3.forward) * 5f);
                turret.RequiredProjectileLifetime = predictedAim.Value.TimeToReach - Time.deltaTime;
                turret.SetTarget(predictedAim.Value.PredictedPosition.To());
            }
            else
            {
                turret.Input = default;
                turret.SetTarget(default(Vector3));
            }

            // turret.target = transform.position + relativePosition;

            turret.LoseTarget();

            if (turret.IsAccurateShoot &&
                !turret.OutOfRange &&
                NetcodeUtils.IsOfflineOrServer)
            {
                if (predictedAim.HasValue && requiredShoots != null)
                { turret.Shoot(requiredShoots, predictedAim.Value.TimeToReach); }
                else
                { turret.Shoot(); }
            }

            turret.PrepareShooting = true;
            return true;
        }
    }
}
