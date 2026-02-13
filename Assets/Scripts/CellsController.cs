using UnityEngine;

public class CellsController : MonoBehaviour {
    [SerializeField] private GameObject cellPrefab;
    [SerializeField] private int cellCount = 10;
    [SerializeField] private float cellSpacing = 0.1f;
    [SerializeField] private bool growLeft = true;

    private void Start() {
        for (var x = 0; x < cellCount; x++) {
            for (var z = 0; z < cellCount; z++) {
                var xSign = growLeft ? -1 : 1;
                var position = new Vector3(
                    x: transform.position.x + xSign * x * (cellSpacing + cellPrefab.transform.localScale.x),
                    y: 0,
                    z: (z - cellCount / 2) * (cellSpacing + cellPrefab.transform.localScale.z)
                );
                Instantiate(
                    original: cellPrefab,
                    position: position,
                    rotation: Quaternion.identity,
                    parent: transform
                );
            }
        }
    }
}
