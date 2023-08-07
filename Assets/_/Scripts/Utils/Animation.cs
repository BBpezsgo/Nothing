using UnityEngine;

namespace Utilities
{
    internal readonly struct AnimationFunctions
    {
        internal static float Square(float v) => v * v;
        internal static float Squareroot(float v) => Mathf.Sqrt(v);
    }

    internal delegate float AnimationFunction(float v);

    internal abstract class Animation
    {
        [SerializeField, ReadOnly] protected readonly float Speed;
        protected readonly AnimationFunction Function;

        protected static float Now => Time.unscaledTime;

        internal abstract float Elapsed { get; }

        protected virtual float RawPercent => Mathf.Clamp01(Elapsed * Speed);

        internal float Percent
        {
            get
            {
                return RawPercent;
                /*
                if (Function == null)
                { return RawPercent; }
                return Function.Invoke(RawPercent);
                */
            }
        }

        internal float PercentInverted => 1f - Percent;

        protected Animation(float speed, AnimationFunction function)
        {
            Speed = speed;
            Function = function;
        }
    }

    [System.Serializable]
    internal class SimpleAnimation : Animation
    {
        [SerializeField, ReadOnly, TimeSpan] protected float startedAt;

        internal bool IsStarted => startedAt > 0f;
        internal bool IsFinished => RawPercent >= 1f;

        internal override float Elapsed => Mathf.Max(Now - startedAt, 0f);

        internal SimpleAnimation(float speed) : this(speed, null) { }
        internal SimpleAnimation(float speed, AnimationFunction function) : base(speed, function)
        {
            startedAt = Now;
        }

        internal virtual void Restart()
        {
            startedAt = Now;
        }

        /// <summary>
        /// Stops the animation and discards the progress
        /// </summary>
        internal virtual void Reset()
        {
            startedAt = 0f;
        }

        /// <summary>
        /// If the animation is not started yet, then it will start
        /// </summary>
        internal virtual void Start()
        {
            if (IsStarted)
            { return; }

            startedAt = Now;
        }
    }

    [System.Serializable]
    internal class SimpleReversableAnimation : SimpleAnimation
    {
        [SerializeField, ReadOnly] bool IsReversed;
        readonly PingPongAnimationController Controller;

        protected override float RawPercent => (IsReversed) ? (1f - base.RawPercent) : (base.RawPercent);

        internal SimpleReversableAnimation(float speed) : this(speed, null, null) { }
        internal SimpleReversableAnimation(float speed, PingPongAnimationController controller) : this(speed, controller, null) { }
        internal SimpleReversableAnimation(float speed, AnimationFunction function) : this(speed, null, function) { }

        internal SimpleReversableAnimation(float speed, PingPongAnimationController controller, AnimationFunction function) : base(speed, function)
        {
            IsReversed = false;
            Controller = controller;
        }

        internal override void Restart()
        {
            base.Restart();
            IsReversed = false;
        }

        /// <summary>
        /// Restarts the animation in reverse mode
        /// </summary>
        internal void RestartBack()
        {
            base.Restart();
            IsReversed = true;
        }

        /// <summary>
        /// Invert the animation direction
        /// </summary>
        internal void Reverse()
            => IsReversed = !IsReversed;

        /// <summary>
        /// Sets the animation direction
        /// </summary>
        internal void Reverse(bool isReversed)
        {
            IsReversed = isReversed;
            startedAt -= .00001f;
        }

        /// <summary>
        /// Stops the animation and discards the progress
        /// </summary>
        internal override void Reset()
        {
            base.Reset();
            IsReversed = false;
        }

        /// <summary>
        /// If the animation is not started yet, then it will start in reverse mode
        /// </summary>
        internal void StartBack()
        {
            if (IsStarted)
            { return; }

            base.Start();

            IsReversed = true;
        }

        /// <summary>
        /// If any controller is attached, then refreshes
        /// </summary>
        internal void Refresh()
        {
            if (Controller == null)
            { return; }

            Controller.Refresh(this);
        }
    }

    internal delegate float AnimationDependency();

    [System.Serializable]
    internal class DependentAnimation : Animation
    {
        readonly AnimationDependency Dependency;

        internal override float Elapsed => Mathf.Max(Now - Dependency.Invoke(), 0f);

        internal DependentAnimation(float speed, AnimationDependency dependency) : this(speed, dependency, null) { }

        internal DependentAnimation(float speed, AnimationDependency dependency, AnimationFunction function) : base(speed, function)
        {
            Dependency = dependency;
        }
    }

    internal delegate bool PingPongCondition();

    [System.Serializable]
    internal class PingPongAnimationController
    {
        readonly PingPongCondition conditionCallback;

        [SerializeField, ReadOnly] bool condition;

        [SerializeField, ReadOnly] bool lastCondition;

        [SerializeField, ReadOnly] bool IsChanged;

        internal PingPongAnimationController() : this(null) { }

        internal PingPongAnimationController(PingPongCondition condition)
        {
            this.conditionCallback = condition;
        }

        internal void Refresh(bool condition)
        {
            this.condition = condition;

            this.IsChanged = this.IsChanged || (this.condition != this.lastCondition);
            this.lastCondition = this.condition;
        }

        internal void Refresh()
            => Refresh(conditionCallback.Invoke());

        internal void Refresh(SimpleReversableAnimation animation)
        {
            if (this.IsChanged)
            { animation.Restart(); }
            this.IsChanged = false;

            animation.Reverse(this.condition);
        }
    }
}