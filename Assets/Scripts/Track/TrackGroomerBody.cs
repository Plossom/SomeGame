using SomeGame.Car;
using UnityEngine;

namespace SomeGame.Track
{
    /// <summary>
    /// One snowcat of <see cref="TrackGroomers"/>: a heavy moving block the cars bounce off. Hit its front blade and you
    /// are flung aside; run into its back and you bounce back off the tiller.
    /// </summary>
    public class TrackGroomerBody : MonoBehaviour
    {
        void OnCollisionEnter2D(Collision2D collision)
        {
            var car = collision.rigidbody != null ? collision.rigidbody.GetComponent<CarMovement>() : null;
            if (car == null || collision.contactCount == 0) return;
            Vector2 forward = transform.up, right = transform.right;
            Vector2 rel = collision.GetContact(0).point - (Vector2)transform.position;
            if (Vector2.Dot(rel, forward) > 1.6f)
            {
                // the blade: flung aside, whichever side it hit
                float side = Mathf.Sign(Vector2.Dot(car.Body.position - (Vector2)transform.position, right));
                car.Bump(right * (side == 0f ? 1f : side), 10f);
            }
            else car.Bump(collision.GetContact(0).normal * -1f, 7f);
        }
    }
}
