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

    [SerializeField] private float selectedElementScale = 1.5f; // 拡大率
    [SerializeField] private float tweenDuration = 0.5f;         // アニメーション時間

    private List<RectTransform> elements = new List<RectTransform>();
    private int selectedIndex = 0;

    private Dictionary<RectTransform, Vector2> originalSizes = new Dictionary<RectTransform, Vector2>();

    // コンテンツのスクロールTweenを管理
    private Tweener contentMoveTween = null;
    void Awake()
    {
        scrollRect = GetComponent<ScrollRect>();
        if (scrollRect == null)
        {
            Debug.LogError("ScrollRect コンポーネントが見つかりません。このスクリプトを ScrollRect がアタッチされたゲームオブジェクトに追加してください。");
            enabled = false;
            return;
        }

        viewportRect = scrollRect.viewport;
        if (viewportRect == null)
        {
            Debug.LogError("Viewport が ScrollRect に設定されていません。ScrollRect の Viewport を正しく設定してください。");
            enabled = false;
            return;
        }

        contentRect = scrollRect.content;
        if (contentRect == null)
        {
            Debug.LogError("Content が ScrollRect に設定されていません。ScrollRect の Content を正しく設定してください。");
            enabled = false;
            return;
        }

        horizontalLayoutGroup = contentRect.GetComponent<HorizontalLayoutGroup>();
        if (horizontalLayoutGroup == null)
        {
            Debug.LogError("Content に HorizontalLayoutGroup コンポーネントが見つかりません。HorizontalLayoutGroup を追加してください。");
            enabled = false;
            return;
        }
    }

    void OnEnable()
    {
        StartCoroutine(InitializeCenter());
    }

    private IEnumerator InitializeCenter()
    {
        // 初回フレーム待機してUIが初期化されるのを待つ
        yield return null;

        // レイアウト再計算
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

        // 子要素収集
        elements.Clear();
        foreach (Transform child in contentRect)
        {
            if (child is RectTransform rt && rt != contentRect)
            {
                elements.Add(rt);
            }
        }

        // オリジナルサイズを記録
        foreach (var elem in elements)
        {
            originalSizes[elem] = elem.sizeDelta;
        }

        // パディングを動的に設定
        SetLayoutPadding();

        // 再度レイアウトを確定
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        yield return null;

        // 初回表示で index=0 を中央表示
        if (elements.Count > 0)
        {
            CenterOnElement(elements[selectedIndex], useTween: false);
        }
    }

    private void SetLayoutPadding()
    {
        if (horizontalLayoutGroup == null)
        {
            Debug.LogError("HorizontalLayoutGroupがアサインされていません。");
            return;
        }

        // ビューポートの幅
        float viewportWidth = viewportRect.rect.width;

        // 要素の幅（最初の要素を基準）
        float elementWidth = elements.Count > 0 ? elements[0].rect.width : 100f; // デフォルト値100

        // パディング計算
        float paddingSide = (viewportWidth - elementWidth) / 2f;
        paddingSide = Mathf.Max(paddingSide, 0); // パディングが負にならないように

        horizontalLayoutGroup.padding.left = Mathf.RoundToInt(paddingSide);
        horizontalLayoutGroup.padding.right = Mathf.RoundToInt(paddingSide);

        Debug.Log($"Set Padding - Left: {horizontalLayoutGroup.padding.left}, Right: {horizontalLayoutGroup.padding.right}");
    }

    public void OnNextButton()
    {
        if (selectedIndex < elements.Count - 1)
        {
            selectedIndex++;
            CenterOnElement(elements[selectedIndex]);
        }
    }

    public void OnPrevButton()
    {
        if (selectedIndex > 0)
        {
            selectedIndex--;
            CenterOnElement(elements[selectedIndex]);
        }
    }

    private void CenterOnElement(RectTransform targetElement, bool useTween = true)
    {
        float viewportWidth = viewportRect.rect.width;

        // 要素中心計算
        Vector3 worldCenter = targetElement.TransformPoint(targetElement.rect.center);
        Vector3 localCenter = contentRect.InverseTransformPoint(worldCenter);
        float elementCenterX = localCenter.x;

        float targetContentPosX = (viewportWidth * 0.5f) - elementCenterX;

        // スクロール移動アニメーション
        if (useTween)
        {
            // 既存のスクロールTweenがあれば停止
            if (contentMoveTween != null && contentMoveTween.IsActive())
            {
                contentMoveTween.Kill();
            }

            // 新しいスクロールTweenを開始
            contentMoveTween = contentRect.DOAnchorPosX(targetContentPosX, tweenDuration)
                .SetEase(Ease.OutCubic);
        }
        else
        {
            // アニメーションなしで即時移動
            contentRect.anchoredPosition = new Vector2(targetContentPosX, contentRect.anchoredPosition.y);
        }

        // 拡大縮小アニメーションを実行
        HandleElementScaling(targetElement, useTween);
    }

    private void HandleElementScaling(RectTransform targetElement, bool useTween)
    {
        foreach (var elem in elements)
        {
            // 既存のサイズTweenを停止
            elem.DOKill();

            if (elem == targetElement)
            {
                // 選択された要素を拡大
                if (useTween)
                {
                    elem.DOSizeDelta(originalSizes[elem] * selectedElementScale, tweenDuration)
                        .SetEase(Ease.OutCubic);
                }
                else
                {
                    elem.sizeDelta = originalSizes[elem] * selectedElementScale;
                }
            }
            else
            {
                // 選択されていない要素を縮小
                if (useTween)
                {
                    elem.DOSizeDelta(originalSizes[elem], tweenDuration)
                        .SetEase(Ease.OutCubic);
                }
                else
                {
                    elem.sizeDelta = originalSizes[elem];
                }
            }
        }

        if (useTween)
        {
            // アニメーション完了後にレイアウトを再計算
            DOVirtual.DelayedCall(tweenDuration, () =>
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
            });
        }
        else
        {
            // レイアウト再計算
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (elements.Count == 0) return;

        // 慣性をリセットして中央に移動後に停止
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

        // 中央に近い要素の拡大縮小をアニメーション付きで実行
        HandleElementScaling(elements[closestIndex], useTween: true);
    }

    public void AddElement(RectTransform newElementPrefab)
    {
        RectTransform newElement = Instantiate(newElementPrefab, contentRect);
        elements.Add(newElement);
        originalSizes[newElement] = newElement.sizeDelta;
        SetLayoutPadding(); // パディング再計算
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
    }
}
