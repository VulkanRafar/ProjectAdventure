using UnityEngine;

public class ChangeEffectHandler : MonoBehaviour
{
    SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        sr.enabled = false; // Sembunyikan sprite saat start
    }

    public void ShowEffect()
    {
        sr.enabled = true;
    }

    public void HideEffect()
    {
        sr.enabled = false;
    }
}
