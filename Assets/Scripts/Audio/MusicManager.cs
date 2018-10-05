using UnityEngine;

public class MusicManager : MonoBehaviour
{
    private void Start()
    {
        var source = GetComponent<AudioSource>();
        source.Play();
    }
}