using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using Random = UnityEngine.Random;

public class BoardManager : MonoBehaviour
{
    [Header("Параметры поля")]
    public int rows = 7;
    public int cols = 7;
    private Cell[,] _gridCells;

    [Header("Настройки")]
    [Tooltip("Родительский объект, содержащий ячейки (например, с GridLayoutGroup)")]
    public Transform cellsParent;
    [Tooltip("Префаб кристалла")]
    public GameObject crystalPrefab;
    
    private bool _isFillingEmptyCells = false; // Флаг, указывающий, что корутина заполнения уже работает

    void Awake()
    {
        // Инициализируем сетку ячеек, предполагая, что дочерних объектов у cellsParent ровно rows*cols
        _gridCells = new Cell[rows, cols];
        int index = 0;
        foreach (Transform child in cellsParent)
        {
            Cell cell = child.GetComponent<Cell>();
            if (cell != null)
            {
                int row = index / cols;
                int col = index % cols;
                cell.GridPosition = new Vector2Int(col, row);
                _gridCells[row, col] = cell;
                index++;
            }
        }
    }

    private IEnumerator Start()
    {
        FillBoard(false);
        
        yield return new WaitForSeconds(2f);
        
        while (true)
        {
            SwapCrystals(new Vector2Int(Random.Range(0, 7), Random.Range(0, 7)),
                new Vector2Int(Random.Range(0, 7), Random.Range(0, 7)));
            yield return new WaitForSeconds(0.32f);
            CollapseMatches();
            yield return new WaitForSeconds(2f);
        }
    }

    /// <summary>
    /// Возвращает мировую позицию ячейки по её координатам.
    /// </summary>
    private Vector3 GetCellWorldPosition(Vector2Int gridPos)
    {
        return _gridCells[gridPos.y, gridPos.x].transform.position;
    }

    /// <summary>
    /// Возвращает случайный тип кристалла.
    /// </summary>
    private CrystalType GetRandomCrystalType()
    {
        return (CrystalType)Random.Range(0, System.Enum.GetValues(typeof(CrystalType)).Length);
    }

    #region 1) Сокращение совпадающих кристаллов (match‑3)

    /// <summary>
    /// Поиск горизонтальных и вертикальных групп из 3 и более одинаковых кристаллов и их удаление.
    /// </summary>
    public void CollapseMatches()
    {
        bool[,] toRemove = new bool[rows, cols];

        // Горизонтальная проверка
        for (int r = 0; r < rows; r++)
        {
            int matchCount = 1;
            for (int c = 1; c < cols; c++)
            {
                if (_gridCells[r, c].CurrentCrystal != null && _gridCells[r, c - 1].CurrentCrystal != null &&
                    _gridCells[r, c].CurrentCrystal.Type == _gridCells[r, c - 1].CurrentCrystal.Type)
                {
                    matchCount++;
                }
                else
                {
                    if (matchCount >= 3)
                    {
                        for (int k = 0; k < matchCount; k++)
                        {
                            toRemove[r, c - 1 - k] = true;
                        }
                    }
                    matchCount = 1;
                }
            }
            if (matchCount >= 3)
            {
                for (int k = 0; k < matchCount; k++)
                {
                    toRemove[r, cols - 1 - k] = true;
                }
            }
        }

        // Вертикальная проверка
        for (int c = 0; c < cols; c++)
        {
            int matchCount = 1;
            for (int r = 1; r < rows; r++)
            {
                if (_gridCells[r, c].CurrentCrystal != null && _gridCells[r - 1, c].CurrentCrystal != null &&
                    _gridCells[r, c].CurrentCrystal.Type == _gridCells[r - 1, c].CurrentCrystal.Type)
                {
                    matchCount++;
                }
                else
                {
                    if (matchCount >= 3)
                    {
                        for (int k = 0; k < matchCount; k++)
                        {
                            toRemove[r - 1 - k, c] = true;
                        }
                    }
                    matchCount = 1;
                }
            }
            if (matchCount >= 3)
            {
                for (int k = 0; k < matchCount; k++)
                {
                    toRemove[rows - 1 - k, c] = true;
                }
            }
        }

        // Удаляем отмеченные кристаллы
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (toRemove[r, c])
                {
                    RemoveCrystalAt(new Vector2Int(c, r));
                }
            }
        }
    }

    /// <summary>
    /// Удаляет кристалл в ячейке с заданными координатами посредством анимации исчезания.
    /// После завершения анимации, если корутина заполнения не запущена, она стартует.
    /// </summary>
    private void RemoveCrystalAt(Vector2Int gridPos)
    {
        Cell cell = _gridCells[gridPos.y, gridPos.x];
        if (cell.CurrentCrystal != null)
        {
            cell.CurrentCrystal.AnimateHide(0.3f, () =>
            {
                Destroy(cell.CurrentCrystal.gameObject);
                cell.ReleaseCrystal();
                // Запускаем заполнение, только если корутина ещё не запущена
                if (!_isFillingEmptyCells)
                {
                    StartCoroutine(FillEmptyCells());
                }
            });
        }
    }


    #endregion

    #region 4) Автоматическое падение кристаллов

    /// <summary>
    /// Проходит по колонкам и, если обнаружены пустые ячейки, перемещает кристаллы вниз с анимацией.
    /// После завершения анимации вызывается повторное заполнение.
    /// </summary>
    private IEnumerator FillEmptyCells()
    {
        // Если уже запущено, выходим
        if (_isFillingEmptyCells)
            yield break;

        _isFillingEmptyCells = true;

        bool moved;
        do
        {
            moved = false;
            // Проходим по каждой колонке
            for (int c = 0; c < cols; c++)
            {
                // Идем сверху вниз (начиная с нижней строки)
                for (int r = rows - 1; r >= 0; r--)
                {
                    if (_gridCells[r, c].CurrentCrystal == null)
                    {
                        // Ищем ближайший кристалл выше
                        for (int r2 = r - 1; r2 >= 0; r2--)
                        {
                            if (_gridCells[r2, c].CurrentCrystal != null)
                            {
                                Cell fromCell = _gridCells[r2, c];
                                Cell toCell = _gridCells[r, c];
                                Crystal movingCrystal = fromCell.CurrentCrystal;
                                fromCell.ReleaseCrystal();

                                // Анимируем перемещение кристалла в целевую ячейку
                                movingCrystal.AnimateMove(toCell.transform.position, 0.3f, () =>
                                {
                                    toCell.AcceptCrystal(movingCrystal);
                                });
                                moved = true;
                                break;
                            }
                        }
                    }
                }
            }

            if (moved)
                yield return new WaitForSeconds(0.35f); // Даем время завершиться анимациям
        }
        while (moved);

        // После того как все кристаллы сдвинулись вниз, заполняем верхние пустые ячейки новыми кристаллами.
        FillBoard(true);

        _isFillingEmptyCells = false;
    }

    #endregion

    #region 3) Заполнение поля кристаллами

    /// <summary>
    /// Заполняет пустые ячейки кристаллами. Если fallingMode==true, кристаллы появляются выше поля и падают вниз.
    /// </summary>
    public void FillBoard(bool fallingMode)
    {
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                if (_gridCells[r, c].CurrentCrystal == null)
                {
                    Cell cell = _gridCells[r, c];
                    GameObject crystalGO = Instantiate(crystalPrefab, transform);
                    Crystal crystal = crystalGO.GetComponent<Crystal>();
                    crystal.Initialize(GetRandomCrystalType());

                    if (fallingMode)
                    {
                        Vector3 startPos = cell.transform.position + Vector3.up * 100f;
                        crystal.transform.position = startPos;
                        crystal.AnimateMove(cell.transform.position, 0.5f, () =>
                        {
                            cell.AcceptCrystal(crystal);
                        });
                    }
                    else
                    {
                        crystal.transform.position = cell.transform.position;
                        cell.AcceptCrystal(crystal);
                    }
                }
            }
        }
    }

    #endregion

    #region 5) Обмен местами кристаллов

    /// <summary>
    /// Меняет местами кристаллы в двух ячейках с анимацией перемещения.
    /// После завершения анимации кристаллы привязываются к новым ячейкам.
    /// </summary>
    public void SwapCrystals(Vector2Int pos1, Vector2Int pos2)
    {
        Cell cell1 = _gridCells[pos1.y, pos1.x];
        Cell cell2 = _gridCells[pos2.y, pos2.x];

        if (cell1.CurrentCrystal != null && cell2.CurrentCrystal != null)
        {
            Crystal crystal1 = cell1.CurrentCrystal;
            Crystal crystal2 = cell2.CurrentCrystal;
            cell1.ReleaseCrystal();
            cell2.ReleaseCrystal();

            Vector3 pos1World = cell1.transform.position;
            Vector3 pos2World = cell2.transform.position;

            crystal1.AnimateMove(pos2World, 0.3f, () =>
            {
                cell2.AcceptCrystal(crystal1);
            });
            crystal2.AnimateMove(pos1World, 0.3f, () =>
            {
                cell1.AcceptCrystal(crystal2);
            });
        }
    }

    #endregion

    #region 6) Прокрутка ряда с wrap‑around

    /// <summary>
    /// Прокручивает ряд (горизонтальный или вертикальный) на заданное смещение.
    /// Кристаллы, выходящие за границу, появляются с другой стороны.
    /// </summary>
    public void ScrollRow(RowType rowType, int index, int delta)
    {
        if (rowType == RowType.Horizontal)
        {
            Crystal[] rowCrystals = new Crystal[cols];
            for (int c = 0; c < cols; c++)
                rowCrystals[c] = _gridCells[index, c].CurrentCrystal;

            Crystal[] newRow = new Crystal[cols];
            for (int c = 0; c < cols; c++)
            {
                int newPos = (c + delta) % cols;
                if (newPos < 0) newPos += cols;
                newRow[newPos] = rowCrystals[c];
            }

            for (int c = 0; c < cols; c++)
            {
                Cell cell = _gridCells[index, c];
                cell.ReleaseCrystal();
                if (newRow[c] != null)
                {
                    newRow[c].AnimateMove(cell.transform.position, 0.3f, () =>
                    {
                        cell.AcceptCrystal(newRow[c]);
                    });
                }
            }
        }
        else if (rowType == RowType.Vertical)
        {
            Crystal[] colCrystals = new Crystal[rows];
            for (int r = 0; r < rows; r++)
                colCrystals[r] = _gridCells[r, index].CurrentCrystal;

            Crystal[] newCol = new Crystal[rows];
            for (int r = 0; r < rows; r++)
            {
                int newPos = (r + delta) % rows;
                if (newPos < 0) newPos += rows;
                newCol[newPos] = colCrystals[r];
            }

            for (int r = 0; r < rows; r++)
            {
                Cell cell = _gridCells[r, index];
                cell.ReleaseCrystal();
                if (newCol[r] != null)
                {
                    newCol[r].AnimateMove(cell.transform.position, 0.3f, () =>
                    {
                        cell.AcceptCrystal(newCol[r]);
                    });
                }
            }
        }
    }

    #endregion

    #region 7) Изменение цвета (типа) кристаллов

    /// <summary>
    /// Меняет тип (цвет) кристалла в указанных ячейках.
    /// </summary>
    public void ChangeCrystalsColor(List<(Vector2Int gridPos, CrystalType newType)> changes)
    {
        foreach (var change in changes)
        {
            Vector2Int pos = change.gridPos;
            if (pos.y >= 0 && pos.y < rows && pos.x >= 0 && pos.x < cols)
            {
                Cell cell = _gridCells[pos.y, pos.x];
                if (cell.CurrentCrystal != null)
                {
                    cell.CurrentCrystal.ChangeType(change.newType);
                }
            }
        }
    }

    #endregion
}

