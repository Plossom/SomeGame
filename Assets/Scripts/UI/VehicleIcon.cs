using UnityEngine;

namespace SomeGame.UI
{
    /// <summary>
    /// The little vehicle on the map and on the loading bar: the buggy, or on the Winter Cup the snowmobile.
    /// Swaps the body (tinted), details and shadow pictures; the buggy's are the images' own.
    /// </summary>
    public class VehicleIcon : MonoBehaviour
    {
        [SerializeField] UnityEngine.UI.Image body;
        [SerializeField] UnityEngine.UI.Image details;
        [SerializeField] UnityEngine.UI.Image shadow;
        [SerializeField] Sprite snowmobileBody;
        [SerializeField] Sprite snowmobileDetails;
        [SerializeField] Sprite snowmobileShadow;

        Sprite _carBody, _carDetails, _carShadow;
        bool _remembered;

        public void Show(bool snowmobile)
        {
            if (!_remembered)
            {
                _carBody = body != null ? body.sprite : null;
                _carDetails = details != null ? details.sprite : null;
                _carShadow = shadow != null ? shadow.sprite : null;
                _remembered = true;
            }
            snowmobile &= snowmobileBody != null;
            if (body != null) body.sprite = snowmobile ? snowmobileBody : _carBody;
            if (details != null) details.sprite = snowmobile ? snowmobileDetails : _carDetails;
            if (shadow != null) shadow.sprite = snowmobile ? snowmobileShadow : _carShadow;
        }
    }
}
