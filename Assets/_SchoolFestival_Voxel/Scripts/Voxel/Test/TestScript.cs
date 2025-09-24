using UnityEngine;

public class TestScript : MonoBehaviour
{   
    public MeshDestroyer meshDestroyer;
    public SeamlessChunkManager seamlessChunkManager;
    private float radius;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SphereCollider sphereCollider = GetComponent<SphereCollider>();
        radius = sphereCollider.radius;
    }

    private void OnCollisionStay(Collision collision)
    {
        meshDestroyer.DestroySphere(transform.position, radius*25f);
        Debug.Log("Test");
    }
}