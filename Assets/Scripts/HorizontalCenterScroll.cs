using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

public class HorizontalCenterScroll : MonoBehaviour, IEndDragHandler, IDragHandler
{
    private ScrollRect scrollRect;
    private RectTransform viewportRect;
    private RectTransform contentRect;
    private HorizontalLayoutGroup horizontalLayoutGroup;

    [SerializeField] private float selectedElementScale = 1.5f; // Scale for the selected element
    [SerializeField] private float tweenDuration = 0.5f;       // Animation duration

    private List<RectTransform> elements = new List<RectTransform>();
    private int selectedIndex = 0;

    private Dictionary<RectTransform, Vector2> originalSizes = new Dictionary<RectTransform, Vector2>();

    // Manages the scrolling tween for the content
    private Tweener contentMoveTween = null;

    void Awake()
    {
        // Assign required components and validate
        scrollRect = GetComponent<ScrollRect>();
        if (scrollRect == null)
        {
            Debug.LogError("ScrollRect component is missing. Attach this script to a GameObject with a ScrollRect.");
            enabled = false;
            return;
        }

        viewportRect = scrollRect.viewport;
        if (viewportRect == null)
        {
            Debug.LogError("Viewport is not set in ScrollRect. Please configure the Viewport.");
            enabled = false;
            return;
        }

        contentRect = scrollRect.content;
        if (contentRect == null)
        {
            Debug.LogError("Content is not set in ScrollRect. Please configure the Content.");
            enabled = false;
            return;
        }

        horizontalLayoutGroup = contentRect.GetComponent<HorizontalLayoutGroup>();
        if (horizontalLayoutGroup == null)
        {
            Debug.LogError("HorizontalLayoutGroup component is missing on Content. Add a HorizontalLayoutGroup.");
            enabled = false;
            return;
        }
    }

    void OnEnable()
    {
        // Initialize the center alignment on enable
        StartCoroutine(InitializeCenter());
    }

    void Start()
    {
        DOTween.SetTweensCapacity(500, 250); // Increase Sequence and Tween capacities
    }


    private IEnumerator InitializeCenter()
    {
        // Wait for one frame to allow UI initialization
        yield return null;

        // Force layout update
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

        // Collect all child elements
        elements.Clear();
        foreach (Transform child in contentRect)
        {
            if (child is RectTransform rt && rt != contentRect)
            {
                elements.Add(rt);
            }
        }

        // Store the original sizes of elements
        foreach (var elem in elements)
        {
            originalSizes[elem] = elem.sizeDelta;
        }

        // Dynamically set padding for layout
        SetLayoutPadding();

        // Force another layout update
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        yield return null;

        // Initially center on the first element (index = 0)
        if (elements.Count > 0)
        {
            CenterOnElement(elements[selectedIndex], useTween: false);
        }
    }

    private void SetLayoutPadding()
    {
        if (horizontalLayoutGroup == null)
        {
            Debug.LogError("HorizontalLayoutGroup is not assigned.");
            return;
        }

        // Calculate padding to center elements in the viewport
        float viewportWidth = viewportRect.rect.width;
        float elementWidth = elements.Count > 0 ? elements[0].rect.width : 100f; // Default element width

        float paddingSide = (viewportWidth - elementWidth) / 2f;
        paddingSide = Mathf.Max(paddingSide, 0); // Ensure padding is not negative

        horizontalLayoutGroup.padding.left = Mathf.RoundToInt(paddingSide);
        horizontalLayoutGroup.padding.right = Mathf.RoundToInt(paddingSide);

        Debug.Log($"Set Padding - Left: {horizontalLayoutGroup.padding.left}, Right: {horizontalLayoutGroup.padding.right}");
    }

    public void OnNextButton()
    {
        // Navigate to the next element if possible
        if (selectedIndex < elements.Count - 1)
        {
            selectedIndex++;
            CenterOnElement(elements[selectedIndex]);
        }
    }

    public void OnPrevButton()
    {
        // Navigate to the previous element if possible
        if (selectedIndex > 0)
        {
            selectedIndex--;
            CenterOnElement(elements[selectedIndex]);
        }
    }

    private void CenterOnElement(RectTransform targetElement, bool useTween = true)
    {
        // Center the target element in the viewport
        float viewportWidth = viewportRect.rect.width;

        // Calculate the center position of the target element
        Vector3 worldCenter = targetElement.TransformPoint(targetElement.rect.center);
        Vector3 localCenter = contentRect.InverseTransformPoint(worldCenter);
        float elementCenterX = localCenter.x;

        float targetContentPosX = (viewportWidth * 0.5f) - elementCenterX;

        // Animate the content movement
        if (useTween)
        {
            if (contentMoveTween != null && contentMoveTween.IsActive())
            {
                contentMoveTween.Kill();
            }

            contentMoveTween = contentRect.DOAnchorPosX(targetContentPosX, tweenDuration)
                .SetEase(Ease.OutCubic);
        }
        else
        {
            contentRect.anchoredPosition = new Vector2(targetContentPosX, contentRect.anchoredPosition.y);
        }

        // Perform scaling animation
        HandleElementScaling(targetElement, useTween);
    }

    private void HandleElementScaling(RectTransform targetElement, bool useTween)
    {
        foreach (var elem in elements)
        {
            // Stop existing tweens
            elem.DOKill();

            if (elem == targetElement)
            {
                // Scale up the selected element
                if (useTween)
                {
                    // Use or reuse Tween to reduce overhead
                    elem.DOSizeDelta(originalSizes[elem] * selectedElementScale, tweenDuration)
                        .SetEase(Ease.OutCubic)
                        .SetUpdate(true);
                }
                else
                {
                    elem.sizeDelta = originalSizes[elem] * selectedElementScale;
                }
            }
            else
            {
                // Reset size for unselected elements
                if (useTween)
                {
                    elem.DOSizeDelta(originalSizes[elem], tweenDuration)
                        .SetEase(Ease.OutCubic)
                        .SetUpdate(true);
                }
                else
                {
                    elem.sizeDelta = originalSizes[elem];
                }
            }
        }

        if (useTween)
        {
            // Use or reuse Tween for delayed layout recalculation
            DOVirtual.DelayedCall(tweenDuration, () =>
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
            }).SetUpdate(true); // Ensure it runs during Update
        }
        else
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        }
    }


    public void OnEndDrag(PointerEventData eventData)
    {
        // Stop inertia and align to the closest element after dragging
        if (elements.Count == 0) return;

        scrollRect.velocity = Vector2.zero;

        float currentContentX = contentRect.anchoredPosition.x;
        float viewportCenterX = viewportRect.rect.width * 0.5f;

        int closestIndex = 0;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < elements.Count; i++)
        {
            Vector3 worldCenter = elements[i].TransformPoint(elements[i].rect.center);
            Vector3 localCenter = contentRect.InverseTransformPoint(worldCenter);
            float elementCenterX = localCenter.x;

            float diff = Mathf.Abs((-(currentContentX) + viewportCenterX) - elementCenterX);
            if (diff < closestDistance)
            {
                closestDistance = diff;
                closestIndex = i;
            }
        }

        selectedIndex = closestIndex;
        CenterOnElement(elements[selectedIndex]);
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Dynamically scale the closest element during dragging
        if (elements.Count == 0) return;

        float currentContentX = contentRect.anchoredPosition.x;
        float viewportCenterX = viewportRect.rect.width * 0.5f;

        int closestIndex = 0;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < elements.Count; i++)
        {
            Vector3 worldCenter = elements[i].TransformPoint(elements[i].rect.center);
            Vector3 localCenter = contentRect.InverseTransformPoint(worldCenter);
            float elementCenterX = localCenter.x;

            float diff = Mathf.Abs((-(currentContentX) + viewportCenterX) - elementCenterX);
            if (diff < closestDistance)
            {
                closestDistance = diff;
                closestIndex = i;
            }
        }

        HandleElementScaling(elements[closestIndex], useTween: true);
    }

    public void AddElement(RectTransform newElementPrefab)
    {
        // Add a new element to the content and recalculate layout
        RectTransform newElement = Instantiate(newElementPrefab, contentRect);
        elements.Add(newElement);
        originalSizes[newElement] = newElement.sizeDelta;
        SetLayoutPadding();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
    }
}
