using UnityEngine;

namespace SomeGame.Car
{
    /// <summary>
    /// The BROADSIDE gadget (Captain Kipper's): BOOM! a cannonball flies out of each side at once; a boat alongside
    /// that it hits spins out in a splash. Yours from the garage has one broadside a lap (the gadget button in the
    /// HUD). Kipper (<see cref="Gadget.Ai"/>) fires when somebody draws level with him, twice a lap at most.
    /// </summary>
    public class BroadsideGadget : Gadget
    {
        const float Speed = 24f, Size = 0.9f;

        public override string Id => "broadside";
        public override string Shout => "BOOM!";

        protected override void Fire()
        {
            var library = SomeGame.Audio.AudioLibrary.Instance;
            Vector2 right = transform.right;
            Vector2 velocity = Car.Body.linearVelocity;
            foreach (float side in new[] { -1f, 1f })
                Snowball.Fire(Car, Sprite, (Vector2)transform.position + right * (side * 1.4f), velocity + right * (side * Speed), Size,
                    Color.white, new Color(0.55f, 0.85f, 0.95f), library?.splash);
            SomeGame.Audio.GameAudio.Play(library?.crash, 0.6f);
        }

        // Kipper: somebody level with him, close on either side: BOOM!
        protected override void Think()
        {
            if (Race.RaceTime < 6f || Time.time - LastUse < 6f || !Ready) return;
            Vector2 right = transform.right;
            foreach (var other in Race.Standings)
            {
                if (other == null || other == Progress || other.IsFinished) continue;
                Vector2 toThem = other.transform.position - transform.position;
                float across = Mathf.Abs(Vector2.Dot(toThem, right)), along = Mathf.Abs(Vector2.Dot(toThem, transform.up));
                if (across < 2.5f || across > 14f || along > 2.5f) continue;
                if (other == Race.Player || Random.value < 0.35f) { Use(); return; }
            }
        }
    }
}
