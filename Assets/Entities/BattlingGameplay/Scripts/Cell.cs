using UnityEngine;
using UnityEngine.UI;

public class Cell : MonoBehaviour
{
    [SerializeField] private Image _cellImage; // Компонент для визуального отображения ячейки (например, для выделения)
    [SerializeField] private Color _inactiveColor = Color.white;
    [SerializeField] private Color _selectedColor = Color.green;
    [SerializeField] private Color _availableColor = Color.yellow;

    /// <summary>
    /// Позиция ячейки в сетке.
    /// </summary>
    public Vector2Int GridPosition { get; set; }

    /// <summary>
    /// Текущее состояние ячейки.
    /// </summary>
    public CellState CurrentState { get; private set; } = CellState.Inactive;

    // Внутренне хранение установленного кристалла.
    private Crystal _currentCrystal;

    /// <summary>
    /// Публичное чтение текущего кристалла (если он установлен).
    /// </summary>
    public Crystal CurrentCrystal => _currentCrystal;

    /// <summary>
    /// Устанавливает состояние ячейки, меняя её визуальное представление.
    /// </summary>
    public void SetState(CellState newState)
    {
        CurrentState = newState;
        switch (newState)
        {
            case CellState.Inactive:
                _cellImage.color = _inactiveColor;
                break;
            case CellState.Selected:
                _cellImage.color = _selectedColor;
                break;
            case CellState.AvailableForMove:
                _cellImage.color = _availableColor;
                break;
        }
    }

    /// <summary>
    /// "Принимает" кристалл: устанавливает его дочерним объектом этой ячейки и сбрасывает локальную позицию.
    /// Все детали (например, вызов SetParent и обнуление позиции) инкапсулированы внутри метода кристалла.
    /// </summary>
    public void AcceptCrystal(Crystal crystal)
    {
        _currentCrystal = crystal;
        crystal.SetParentAndReset(transform);
    }

    /// <summary>
    /// "Отвязывает" кристалл от ячейки, очищая внутреннюю ссылку.
    /// Это нужно, чтобы при анимации перемещения кристалла контроллер не вмешивался в детали реализации.
    /// </summary>
    public void ReleaseCrystal()
    {
        _currentCrystal = null;
    }
}
