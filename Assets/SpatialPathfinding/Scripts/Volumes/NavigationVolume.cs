using Unity.Mathematics;
using UnityEngine;

[DefaultExecutionOrder(150)]
[RequireComponent(typeof(BoxCollider))]
public class NavigationVolume : MonoBehaviour
{
    [SerializeField, Range(1, 15)] private int cellSize = 1;
    [SerializeField] private int3 cellAmount;
    [SerializeField] private int amountOfCellsPerMainCell;
    [Space]
    [SerializeField] private bool ShowGrid = false;
    [Space]
    [SerializeField] private Color volumeColor = new Color(0f, 1f, 0.85f, 0.72f);

    private void OnTriggerEnter(Collider other)
    {
        FlyingAgent targetAgent = other.gameObject.GetComponent<FlyingAgent>();

        if (targetAgent != null)
        {
            targetAgent.UpdateActiveVolume(this);
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = volumeColor;
        Gizmos.DrawCube(transform.position, new Vector3(cellAmount.x, cellAmount.y, cellAmount.z) * amountOfCellsPerMainCell * cellSize);

        if (ShowGrid)
        {
            for (int x = 0; x < cellAmount.x; x++)
            {
                for (int y = 0; y < cellAmount.y; y++)
                {
                    for (int z = 0; z < cellAmount.z; z++)
                    {
                        Vector3 mainCellCenter = new Vector3(
                        transform.position.x + ((x - (cellAmount.x - 1f) / 2f) * cellSize) * amountOfCellsPerMainCell,
                        transform.position.y + ((y - (cellAmount.y - 1f) / 2f) * cellSize) * amountOfCellsPerMainCell,
                        transform.position.z + ((z - (cellAmount.z - 1f) / 2f) * cellSize) * amountOfCellsPerMainCell);

                        Gizmos.color = Color.blue;
                        Gizmos.DrawWireCube(mainCellCenter, Vector3.one);

                        for (int a = 0; a < amountOfCellsPerMainCell; a++)
                        {
                            for (int b = 0; b < amountOfCellsPerMainCell; b++)
                            {
                                for (int c = 0; c < amountOfCellsPerMainCell; c++)
                                {
                                    Vector3 subcellCenter = new Vector3(
                                        mainCellCenter.x + (a - (amountOfCellsPerMainCell - 1f) / 2f) * cellSize,
                                        mainCellCenter.y + (b - (amountOfCellsPerMainCell - 1f) / 2f) * cellSize,
                                        mainCellCenter.z + (c - (amountOfCellsPerMainCell - 1f) / 2f) * cellSize
                                    );

                                    Gizmos.color = Color.red;
                                    Gizmos.DrawWireCube(subcellCenter, Vector3.one * 0.1f);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}