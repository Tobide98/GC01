using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class ScrollingUIBackground : MonoBehaviour
{
    public float speed = 1f;
    public Vector2 direction = Vector2.right;

    private Image img;

    private void Start()
    {
        img = GetComponent<Image>();
        ResetOffset();
    }

    private void Update()
    {
        img.material.mainTextureOffset += -direction.normalized * Time.deltaTime * speed;
    }

    public void ResetOffset()
    {
        img.material.mainTextureOffset = Vector2.zero;
    }
}
