using UnityEngine;

public class DetectionDirectionFiller : MonoBehaviour
{
    [SerializeField] GameObject fillerToScale;
    public void UpdateFiller(float scaleAmount)
    {
        Vector3 newScaleToSet = fillerToScale.transform.localScale;
        newScaleToSet.x = scaleAmount;
        fillerToScale.transform.localScale = newScaleToSet;
    }
}
