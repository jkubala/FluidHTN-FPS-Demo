using UnityEngine;

namespace FPSDemo.Player
{
    public class DetectionDirectionFiller : MonoBehaviour
    {
        // ========================================================= INSPECTOR FIELDS

        [SerializeField] GameObject fillerToScale;


        // ========================================================= PUBLIC METHODS

        public void UpdateFiller(float scaleAmount)
        {
            Vector3 newScaleToSet = fillerToScale.transform.localScale;
            newScaleToSet.x = scaleAmount;
            fillerToScale.transform.localScale = newScaleToSet;
        }
    }
}
