using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class test_edge_triangles : MonoBehaviour
{
    int vertical_resolution=10;
    int horizontal_resolution=10;
    public Material material;
    Mesh mesh;
    List<Vector3> vertices;
    Vector3[] myVertices=new Vector3[126];
    List<int> triangles;
    int[] myTriangles=new int[630];
    List<Vector2> uvs;
    Vector2[] myUV=new Vector2[126];
    void Start()
    {
        vertices = new List<Vector3>();
        triangles=new List<int>();
        uvs = new List<Vector2>();
        genVertices();
        genTriangles();
        DrawMesh();
        TriangleNeighbors.GetNeighbors(mesh);    
    }
    void DrawMesh()
    {
        gameObject.AddComponent<MeshFilter>();
        gameObject.AddComponent<MeshRenderer>();
        gameObject.GetComponent<MeshRenderer>().material = material;
        mesh = GetComponent<MeshFilter>().mesh;
        mesh.Clear();
        //設置頂點
        for(int i=0;i<vertices.Count;i++){ vertices[i]+=new Vector3(0,2,1);}
        Vector3[] myVertices=vertices.ToArray();
        mesh.vertices=myVertices;
        print("myVertices.Length: "+ myVertices.Length);
        //設置三角形頂點順序，順時針設置
        int[] myTriangles=triangles.ToArray();
        mesh.triangles=myTriangles;
        //for(int i=0;i<triangles.Count;i++){print("triangle["+i+"]: "+triangles[i]);}
        print("myTriangles.Length: "+ myTriangles.Length);
        //設置uv
        Vector2[] myUV=uvs.ToArray();
        mesh.uv=myUV;
        print("myUV.Length: "+ myUV.Length);
    }
    // Update is called once per frame
    void Update()
    {
        
    }
    void genVertices()
    {
        // Vertices
        for (int v_index = 0; v_index <= vertical_resolution; ++v_index)
        {
            for (int h_index = 0; h_index <= horizontal_resolution; ++h_index)
            {
                float u = (h_index - 0.5f) / horizontal_resolution; 
                if (v_index % 2 == 0 || h_index == 0) u = h_index / horizontal_resolution;
                float v = (v_index) / (vertical_resolution);
                float x = (u- 0.5f) * 2;
                float y = (v - 0.5f) * 2;

                vertices.Add(new Vector3(x, 0, y));
                uvs.Add(new Vector2(u, v));
                // Additional vetex at the even-indexed row
                if (v_index % 2 == 1 && h_index == horizontal_resolution)
                {
                    vertices.Add(new Vector3(0.5f * 2, 0, y));
                    uvs.Add(new Vector2(1, v));
                }
            }
        }
    }
    void genTriangles()
    {
        // Triangles
        for (int v_index = 0; v_index < vertical_resolution; ++v_index)
        {
            if (v_index % 2 == 0)
            {
                int top_row_begin = (2 * (horizontal_resolution + 1) + 1) * (v_index / 2);
                int bottom_row_begin = top_row_begin + horizontal_resolution + 1;

                for (int h_index = 0; h_index <= horizontal_resolution; ++h_index)
                {
                    if (h_index == 0)
                    {
                        triangles.Add(top_row_begin + h_index);
                        triangles.Add(bottom_row_begin + 0);
                        triangles.Add(bottom_row_begin + 1);
                    }
                    else
                    {
                        triangles.Add(top_row_begin + h_index);
                        triangles.Add(top_row_begin + h_index - 1);
                        triangles.Add(bottom_row_begin + h_index);
                        
                        triangles.Add(top_row_begin + h_index);
                        triangles.Add(bottom_row_begin + h_index); 
                        triangles.Add(bottom_row_begin + h_index + 1);
                    }
                }
            }
            else
            {
                int top_row_begin = (2 * (horizontal_resolution + 1) + 1) * ((v_index - 1) / 2) + horizontal_resolution + 1;
                int bottom_row_begin = top_row_begin + horizontal_resolution + 2;

                for (int h_index = 0; h_index <= horizontal_resolution; ++h_index)
                {
                    if (h_index == 0)
                    {
                        triangles.Add(top_row_begin + h_index);
                        triangles.Add(bottom_row_begin + h_index);
                        triangles.Add(top_row_begin + h_index + 1);
                    }
                    else
                    {
                        triangles.Add(top_row_begin + h_index);
                        triangles.Add(bottom_row_begin + h_index - 1);
                        triangles.Add(bottom_row_begin + h_index);
                        
                        triangles.Add(top_row_begin + h_index);
                        triangles.Add(bottom_row_begin + h_index);
                        triangles.Add(top_row_begin + h_index + 1);
                    }
                }
            }
        }
    }
}
