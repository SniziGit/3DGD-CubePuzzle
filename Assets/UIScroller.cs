using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class UIScroller : MonoBehaviour
{
    [Header("Scroll Settings")]
    [SerializeField] private float scrollSpeed = 100f;
    [SerializeField] private float minY = 0f;
    [SerializeField] private float maxY = 500f;
    [SerializeField] private bool smoothScroll = true;
    [SerializeField] private float smoothTime = 0.1f;
    
    [Header("Input Settings")]
    [SerializeField] private bool invertScroll = false;
    [SerializeField] private bool requireHover = true;
    
    private RectTransform rectTransform;
    private float currentY;
    private float velocityY;
    private bool isHovering;
    
    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        currentY = rectTransform.anchoredPosition.y;
        
        // Clamp initial position
        currentY = Mathf.Clamp(currentY, minY, maxY);
        rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, currentY);
    }
    
    private void Update()
    {
        HandleInput();
        
        if (smoothScroll)
        {
            currentY = Mathf.SmoothDamp(currentY, rectTransform.anchoredPosition.y, ref velocityY, smoothTime);
        }
        
        UpdateScrollPosition();
    }
    
    private void HandleInput()
    {
        if (requireHover && !isHovering)
            return;
            
        float scrollInput = Input.GetAxis("Mouse ScrollWheel");
        
        if (Mathf.Abs(scrollInput) > 0.01f)
        {
            float scrollDirection = invertScroll ? -1f : 1f;
            float newY = rectTransform.anchoredPosition.y + (scrollInput * scrollSpeed * scrollDirection);
            
            newY = Mathf.Clamp(newY, minY, maxY);
            
            if (smoothScroll)
            {
                currentY = newY;
            }
            else
            {
                rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, newY);
            }
        }
    }
    
    private void UpdateScrollPosition()
    {
        if (smoothScroll)
        {
            Vector2 targetPosition = new Vector2(rectTransform.anchoredPosition.x, currentY);
            rectTransform.anchoredPosition = Vector2.Lerp(rectTransform.anchoredPosition, targetPosition, Time.deltaTime * 10f);
        }
    }
    
    public void SetScrollPosition(float y)
    {
        y = Mathf.Clamp(y, minY, maxY);
        currentY = y;
        rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, y);
    }
    
    public void ScrollToTop()
    {
        SetScrollPosition(minY);
    }
    
    public void ScrollToBottom()
    {
        SetScrollPosition(maxY);
    }
    
    public void SetScrollRange(float newMinY, float newMaxY)
    {
        minY = newMinY;
        maxY = newMaxY;
        
        // Ensure current position is within new range
        float currentY = rectTransform.anchoredPosition.y;
        SetScrollPosition(currentY);
    }
    
    private void OnMouseEnter()
    {
        isHovering = true;
    }
    
    private void OnMouseExit()
    {
        isHovering = false;
    }
    
    // For UI elements that don't receive mouse events directly
    public void SetHovering(bool hovering)
    {
        isHovering = hovering;
    }
    
    // Visual feedback in editor
    private void OnDrawGizmosSelected()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();
            
        Gizmos.color = Color.yellow;
        Vector3 minPos = transform.position + Vector3.up * minY;
        Vector3 maxPos = transform.position + Vector3.up * maxY;
        
        Gizmos.DrawLine(minPos, maxPos);
        Gizmos.DrawSphere(minPos, 5f);
        Gizmos.DrawSphere(maxPos, 5f);
    }
}
