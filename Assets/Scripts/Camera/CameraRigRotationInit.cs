using UnityEngine;

public class CameraRigRotationInit : MonoBehaviour
{
    public Vector3 startEulerRotation;

    private void Awake()
    {
        transform.eulerAngles = startEulerRotation;
    }
}