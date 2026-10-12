using SomeGame.Car;
using UnityEngine;

namespace SomeGame.Input
{
    /// <summary>
    /// Development helper: a car input whose stick, throttle and drift button are set from code (automated
    /// driving tests). Not used in the game itself.
    /// </summary>
    public class ScriptedCarInput : MonoBehaviour, ICarInput
    {
        public Vector2 Steer;
        public float Gas = 1f;
        public bool Drift;

        public Vector2 SteerDirection => Steer;
        public float Throttle => Gas;
        public bool DriftHeld => Drift;
    }
}
