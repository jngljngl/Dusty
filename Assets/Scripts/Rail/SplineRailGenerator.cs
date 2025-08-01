// SplineRailGenerator.cs – Geração de rail cilíndrico com pré-visualização e otimização
using UnityEngine;
using UnityEngine.Splines;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[RequireComponent(typeof(SplineContainer))]
public class SplineRailGenerator : MonoBehaviour
{
    [Header("Mesh Settings")]
    public float raio = 0.1f;
    public int segmentosRadiais = 8;
    public int subdivisoesSpline = 50;
    public Material material;
    public bool atualizarNoEditor = true;

    private Mesh railMeshPreview;

    void OnEnable()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying && atualizarNoEditor)
        {
            EditorApplication.update += AtualizarPreview;
        }
#endif
    }

    void OnDisable()
    {
#if UNITY_EDITOR
        EditorApplication.update -= AtualizarPreview;
#endif
    }

    void AtualizarPreview()
    {
        if (!atualizarNoEditor) return;
        if (GetComponent<SplineContainer>() == null) return;

        List<Vector3> pontos = new List<Vector3>();
        var container = GetComponent<SplineContainer>();
        for (int i = 0; i <= subdivisoesSpline; i++)
        {
            float t = i / (float)subdivisoesSpline;
            pontos.Add(container.Spline.EvaluatePosition(t));
        }

        if (railMeshPreview != null) DestroyImmediate(railMeshPreview);
        railMeshPreview = GerarCilindroMesh(pontos);

        MeshFilter mf = GetComponent<MeshFilter>();
        if (!mf) mf = gameObject.AddComponent<MeshFilter>();
        mf.sharedMesh = railMeshPreview;

        MeshRenderer mr = GetComponent<MeshRenderer>();
        if (!mr) mr = gameObject.AddComponent<MeshRenderer>();
        if (material) mr.sharedMaterial = material;
    }

    [ContextMenu("Gerar Rail Mesh Consolidada")]
    public void GerarMeshCilindrica()
    {
        SplineContainer container = GetComponent<SplineContainer>();
        if (container == null || container.Spline == null) return;

        List<Vector3> pontos = new List<Vector3>();
        for (int i = 0; i <= subdivisoesSpline; i++)
        {
            float t = i / (float)subdivisoesSpline;
            pontos.Add(container.Spline.EvaluatePosition(t));
        }

        Mesh railMesh = GerarCilindroMesh(pontos);

        GameObject railObj = new GameObject("GeneratedRail");
        railObj.tag = "Rail";
        railObj.transform.position = Vector3.zero;
        railObj.transform.rotation = Quaternion.identity;

        MeshFilter mf = railObj.AddComponent<MeshFilter>();
        mf.sharedMesh = railMesh;

        MeshRenderer mr = railObj.AddComponent<MeshRenderer>();
        if (material) mr.sharedMaterial = material;

        MeshCollider mc = railObj.AddComponent<MeshCollider>();
        mc.sharedMesh = railMesh;
        mc.convex = false;

       // railObj.AddComponent<Rail>();
    }

    private Mesh GerarCilindroMesh(List<Vector3> path)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        int rings = path.Count;
        float anguloStep = 360f / segmentosRadiais;

        for (int i = 0; i < rings; i++)
        {
            Vector3 forward = (i < rings - 1) ? (path[i + 1] - path[i]).normalized : (path[i] - path[i - 1]).normalized;
            Vector3 up = Vector3.up;
            Vector3 right = Vector3.Cross(up, forward).normalized;
            up = Vector3.Cross(forward, right).normalized;

            for (int j = 0; j < segmentosRadiais; j++)
            {
                float angRad = Mathf.Deg2Rad * anguloStep * j;
                Vector3 offset = Mathf.Cos(angRad) * right * raio + Mathf.Sin(angRad) * up * raio;
                vertices.Add(path[i] + offset);
                uvs.Add(new Vector2(j / (float)segmentosRadiais, i / (float)rings));
            }
        }

        for (int i = 0; i < rings - 1; i++)
        {
            for (int j = 0; j < segmentosRadiais; j++)
            {
                int atual = i * segmentosRadiais + j;
                int proximo = i * segmentosRadiais + (j + 1) % segmentosRadiais;
                int acima = atual + segmentosRadiais;
                int acimaProx = proximo + segmentosRadiais;

                triangles.Add(atual);
                triangles.Add(acima);
                triangles.Add(acimaProx);

                triangles.Add(atual);
                triangles.Add(acimaProx);
                triangles.Add(proximo);
            }
        }

        Mesh mesh = new Mesh();
        mesh.name = "SplineRailMesh";
        mesh.indexFormat = vertices.Count > 65000 ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();

        return mesh;
    }
}
