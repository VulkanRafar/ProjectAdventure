using UnityEngine;

public class AfterImage : MonoBehaviour
{
    public float lifeTime = 0.3f;
    private float timeAlive;
    private SpriteRenderer spriteRenderer;
    private Color initialColor;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        initialColor = spriteRenderer.color;
    }

    private void OnEnable()
    {
        timeAlive = 0f;
        spriteRenderer.color = initialColor;
    }

    private void Update()
    {
        timeAlive += Time.deltaTime;
        float alpha = Mathf.Lerp(initialColor.a, 0, timeAlive / lifeTime);
        spriteRenderer.color = new Color(initialColor.r, initialColor.g, initialColor.b, alpha);

        if (timeAlive >= lifeTime)
        {
            Destroy(gameObject);
        }
    }
}

