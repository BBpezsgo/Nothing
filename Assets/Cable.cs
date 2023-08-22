using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Components
{
    public class Cable : MonoBehaviour
    {
        [SerializeField, ReadOnly] LineRenderer LineRenderer;
        [SerializeField] Transform Joint1;
        [SerializeField] Transform Joint2;

        readonly struct Joint
        {
            public readonly bool IsNull;
            public readonly bool IsRigidbody;
            public readonly Rigidbody Rigidbody;
            public readonly Transform Transform;
            readonly Vector3 rigidbodyOffset;

            public readonly Vector3 Position
            {
                get
                {
                    if (IsRigidbody)
                    {
                        return Rigidbody.position - rigidbodyOffset;
                    }
                    else
                    {
                        return Transform.position;
                    }
                }
                set
                {
                    if (IsRigidbody)
                    {
                        Rigidbody.MovePosition(value + rigidbodyOffset);
                    }
                    else
                    {
                        Transform.position = value;
                    }
                }
            }

            public Joint(Transform transform)
            {
                IsNull = transform == null;
                Transform = transform;
                rigidbodyOffset = Vector3.zero;
                IsRigidbody = false;
                Rigidbody = null;

                if (!IsNull)
                {
                    IsRigidbody = transform.gameObject.TryGetComponentInChildren(out Rigidbody);
                    if (!IsRigidbody && transform.parent != null)
                    {
                        IsRigidbody = transform.parent.gameObject.TryGetComponent(out Rigidbody);
                    }

                    if (IsRigidbody)
                    {
                        rigidbodyOffset = Rigidbody.position - transform.position;
                    }
                }
            }
        }

        class Point
        {
            Vector3 _position;
            Vector3 _oldPosition;
            Vector3 _velocity;

            public Vector3 Position
            {
                get
                {
                    return Joint.IsNull ? _position : Joint.Position;
                }
                set
                {
                    if (Joint.IsNull)
                    {
                        _position = value;
                    }
                    else
                    {
                        Joint.Position = value;
                    }
                }
            }

            public Vector3 Velocity => _velocity;

            public Joint Joint;

            public Point(Vector3 position, Joint joint)
            {
                _position = position;
                _oldPosition = position;
                Joint = joint;
            }

            public void Update(float deltaTime)
            {
                _velocity = _position - _oldPosition;
                _oldPosition = _position;

                if (!Joint.IsNull)
                {
                    _position = Joint.Position;
                    return;
                }

                _position += _velocity * deltaTime;
                _position += Physics.gravity * deltaTime;

                float minY = TheTerrain.Height(_position);
                if (_position.y <= minY)
                {
                    _position.y = minY;
                }
            }
        }

        class Rod
        {
            public Point A;
            public Point B;
            public float Length;

            public Vector3 CurrentOffset => B.Position - A.Position;
            public float CurrentDistance => CurrentOffset.magnitude;

            public Rod(Point a, Point b, float length)
            {
                A = a;
                B = b;
                Length = length;
            }

            public void Update(float deltaTime)
            {
                float distance = CurrentDistance;

                if (distance <= Length)
                { return; }

                float error = distance - Length;

                Vector3 change = (B.Position - A.Position).normalized;
                change *= error;

                float ammountA = .5f;
                float ammountB = .5f;

                if (!A.Joint.IsNull)
                {
                    ammountA = 0;
                    ammountB = 1;
                }
                else if (!B.Joint.IsNull)
                {
                    ammountA = 1;
                    ammountB = 0;
                }

                A.Position += change * ammountA;
                B.Position -= change * ammountB;
            }
        }

        Point[] Points;
        Rod[] Rods;

        void Start()
        {
            LineRenderer = GetComponentInChildren<LineRenderer>(false);

            if (LineRenderer == null)
            { Component.Destroy(this); }

            Generate(20);

            Points[0].Joint = new Joint(Joint1);
            Points[^1].Joint = new Joint(Joint2);
        }

        void Generate(int n)
        {
            Points = new Point[n];
            List<Rod> rods = new();

            LineRenderer.positionCount = n;

            Vector3 start = transform.position;
            Vector3 end = transform.position + Vector3.forward;

            if (Joint1 != null)
            { start = Joint1.position; }
            if (Joint2 != null)
            { end = Joint2.position; }

            for (int i = 0; i < n; i++)
            {
                float p = (float)i / (float)n;

                Points[i] = new Point(start + (end * p), new Joint(null));

                if (i > 0)
                { rods.Add(new Rod(Points[i], Points[i - 1], .1f)); }

                LineRenderer.SetPosition(i, Points[i].Position);
            }

            Rods = rods.ToArray();
        }

        void FixedUpdate()
        {
            for (int i = 0; i < Points.Length; i++)
            {
                Points[i].Update(Time.fixedDeltaTime);
                LineRenderer.SetPosition(i, Points[i].Position);
            }

            for (int i = 0; i < Rods.Length; i++)
            {
                Rods[i].Update(Time.fixedDeltaTime);
            }
        }

        void OnDrawGizmosSelected()
        {
            try
            {
                for (int i = 0; i < Points.Length; i++)
                {
                    Gizmos.color = Color.gray;
                    Gizmos.DrawSphere(Points[i].Position, .2f);

                    Gizmos.color = Color.magenta;
                    Gizmos.DrawRay(Points[i].Position, Points[i].Velocity);
                }

                for (int i = 0; i < Rods.Length; i++)
                {
                    Gizmos.color = Color.white;
                    Gizmos.DrawLine(Rods[i].A.Position, Rods[i].B.Position);
                }
            }
            catch (NullReferenceException) { }
        }
    }
}
