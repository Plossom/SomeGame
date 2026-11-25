using UnityEngine;

namespace SomeGame.Car
{
    /// <summary>
    /// The HAY BALE DROP gadget: drops a hay bale on the road just behind the car. Yours from the garage holds one
    /// bale, filled again every lap (the gadget button in the HUD). Rosie Rust's (<see cref="Gadget.Ai"/>) drops one when
    /// a car tucks in close behind her, twice a lap at most.
    /// </summary>
    public class HayBaleGadget : Gadget
    {
        public override string Id => "haybale";
        public override string Shout => "HAY!";

        protected override void Fire()
        {
            HayBale.Drop(Car, Sprite, (Vector2)transform.position - (Vector2)transform.up * 2.3f);
            SomeGame.Audio.GameAudio.Play(SomeGame.Audio.AudioLibrary.Instance?.land, 0.7f);
        }

        // Rosie: somebody right behind her, on her line, after the start has sorted itself out: hay!
        protected override void Think()
        {
            if (Race.RaceTime < 6f || Time.time - LastUse < 6f || !Ready) return;
            float me = Progress.Metres;
            foreach (var other in Race.Standings)
            {
                if (other == null || other == Progress || other.IsFinished) continue;
                float behind = me - other.Metres;
                if (behind < 4f || behind > 22f) continue;
                Vector2 toThem = other.transform.position - transform.position;
                if (Vector2.Angle(-transform.up, toThem) > 35f) continue;
                if (other == Race.Player || Random.value < 0.35f) { Use(); return; }
            }
        }
    }
}
