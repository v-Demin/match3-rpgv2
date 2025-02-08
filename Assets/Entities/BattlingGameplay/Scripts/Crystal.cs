using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class Crystal : MonoBehaviour
{
    [SerializeField] private Sprite[] _crystalSprites;
    [SerializeField] private Image _visual; // Если позже решите использовать 3D-модель, здесь можно заменить на MeshRenderer и т.п.

    public CrystalType Type { get; private set; }

    /// <summary>
    /// Инициализация кристалла заданным типом.
    /// </summary>
    public void Initialize(CrystalType type)
    {
        Type = type;
        UpdateVisual();
    }

    /// <summary>
    /// Обновляет внешний вид кристалла (например, выбирает нужный спрайт).
    /// </summary>
    private void UpdateVisual()
    {
        if (_visual != null && _crystalSprites != null && _crystalSprites.Length > (int)Type)
        {
            _visual.sprite = _crystalSprites[(int)Type];
            // Если потребуется более сложная логика (например, 3D‑модель), измените здесь.
        }
    }

    /// <summary>
    /// Меняет тип кристалла и обновляет его визуальное представление.
    /// </summary>
    public void ChangeType(CrystalType newType)
    {
        Type = newType;
        UpdateVisual();
    }

    /// <summary>
    /// Анимирует перемещение к указанной мировой позиции. 
    /// Важно: контроллер вызывает этот метод, не зная, как именно осуществляется перемещение.
    /// </summary>
    public void AnimateMove(Vector3 targetWorldPosition, float duration, TweenCallback onComplete)
    {
        transform.DOMove(targetWorldPosition, duration).OnComplete(onComplete);
    }

    /// <summary>
    /// Анимирует исчезновение кристалла (например, затухание). 
    /// Контроллер не знает о том, что используется Image – реализация спрятана внутри.
    /// </summary>
    public void AnimateHide(float duration, TweenCallback onComplete)
    {
        if (_visual != null)
            _visual.DOFade(0f, duration).OnComplete(onComplete);
        else
            onComplete?.Invoke();
    }

    /// <summary>
    /// Устанавливает родительский объект и сбрасывает локальную позицию в (0, 0).
    /// Это используется при «приёме» кристалла ячейкой.
    /// </summary>
    public void SetParentAndReset(Transform parent)
    {
        transform.SetParent(parent);
        transform.localPosition = Vector3.zero;
    }
}
