// Assets/Scripts/TestDestruction.cs
using System.Collections;
using UnityEngine;

/// <summary>
/// 自動的にトーラス上に複数の破壊を行ってメッシュが複雑化できるかを試すデモ
/// - ChunkManager と MeshDestroyer が同じ GameObject にアタッチされている想定
/// </summary>
// [RequireComponent(typeof(ChunkManager))]
[RequireComponent(typeof(MeshDestroyer))]
public class TestDestruction : MonoBehaviour
{
    public int hits = 1;
    public float destroyRadius = 1.2f;
    public float waitBetween = 0.2f;
    public Vector3 transformPoint;

    SeamlessChunkManager cm;
    MeshDestroyer md;

    void Start()
    {
        cm = GetComponent<SeamlessChunkManager>();
        md = GetComponent<MeshDestroyer>();

        // StartCoroutine(RunDestruction());
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            StartCoroutine(RunDestruction());
        }
    }

    IEnumerator RunDestruction()
    {
        yield return new WaitForSeconds(waitBetween);
        // place destruction points along a circle in world space (around origin where torus is centered)
        float R = cm.torusOuterR;
        for (int i = 0; i < hits; i++)
        {
            float a = (float)i / hits * Mathf.PI * 2f;
            Vector3 pos = new Vector3(Mathf.Cos(a) * R, 0f, Mathf.Sin(a) * R);
            pos += transformPoint; 
            md.DestroySphere(pos, destroyRadius);
            Debug.Log("テスト破壊");
            yield return new WaitForSeconds(waitBetween);
        }
    }
}
