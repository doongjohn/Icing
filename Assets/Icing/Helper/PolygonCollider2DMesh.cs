using UnityEngine;

[RequireComponent(typeof(PolygonCollider2D))]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[ExecuteInEditMode]
public class PolygonCollider2DMesh : MonoBehaviour
{
    private PolygonCollider2D pc2;
    private MeshFilter mf;
    private Mesh mesh;

#if UNITY_EDITOR
    private void Awake()
    {
        pc2 = gameObject.GetComponent<PolygonCollider2D>();
        mf = GetComponent<MeshFilter>();
        mesh = new Mesh();
    }
    void Update()
    {
        if (Application.isPlaying || pc2 == null)
            return;

        int pointCount = pc2.GetTotalPointCount();
        Vector2[] points = pc2.points;
        Vector3[] vertices = new Vector3[pointCount];
        Vector2[] uv = new Vector2[pointCount];
        for (int j = 0; j < pointCount; j++)
        {
            Vector2 actual = points[j];
            vertices[j] = new Vector3(actual.x, actual.y, 0);
            uv[j] = actual;
        }

        Triangulator tr = new Triangulator(points);
        int[] triangles = tr.Triangulate();

        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mf.mesh = mesh;
    }
#endif
}
