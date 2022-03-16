using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class cloth_TriangleMesh : MonoBehaviour
{
    int horizontal_resolution=30;//水平
    int vertical_resolution=30;//垂直
    List<Vector3> vertices= new List<Vector3>();
    Vector3[] myVertices=new Vector3[976];
    Particle[] ball=new Particle[976];
    List<int> triangles=new List<int>();
    int[] myTriangles=new int[5490];
    int[,] m_triangle_list=new int[1830,3];
    List<Vector2> uvs= new List<Vector2>();
    Vector2[] myUV;
    float[,] m_uv_list=new float[1830,6]; // 6=TriangleMesh三頂點的uv
    List<DistanceConstraint> distconstraints = new List<DistanceConstraint>();
    List<FixedPointConstraint> fixconstraints = new List<FixedPointConstraint>();
    public Material material;
    Mesh mesh;
    void Start()
    {
        genVertices();
        genTriangles();
        DrawMeshSetConstraint();
    }
    void Update()
    {
        for(int substep=0;substep<5;substep++)
        {
            float m_delta_physics_time = 1/60f; // 公式:delta_frame_time/substep
            //重力模擬      
            for(int i=0;i<ball.Length;i++)
            {//f=ma, a=f*1/m = f*w
                ball[i].v = ball[i].v + m_delta_physics_time * ball[i].w * ball[i].f;
                ball[i].p = ball[i].x + m_delta_physics_time * ball[i].v;
            }
            // Reset Lagrange multipliers (only necessary for XPBD)
            foreach(DistanceConstraint constraint in distconstraints){
                constraint.m_lagrange_multiplier=0;
            }
            foreach(FixedPointConstraint constraint in fixconstraints){
                constraint.m_lagrange_multiplier=0;
            }
            // Project Particles
            int solverIterators=10;
            for (int i = 0; i < solverIterators; i++){
                foreach(DistanceConstraint constraint in distconstraints){
                    constraint.projectParticles();
                }
                foreach(FixedPointConstraint constraint in fixconstraints)
                {
                    constraint.projectParticles();
                }  
            }
            //更新 GameObject localPosition & Particles
            for(int i=0;i<ball.Length;i++)
            {
                //更新 GameObject's localPosition
                myVertices[i] = ball[i].p;
                mesh.vertices=myVertices;
                //更新 particle
                ball[i].v = (ball[i].p- ball[i].x) * (1.0f/m_delta_physics_time);
                ball[i].x = ball[i].p;
                //Update velocities
                ball[i].v*=0.9999f;
            }
        }
    }
    void DrawMeshSetConstraint()
    {
        gameObject.AddComponent<MeshFilter>();
        gameObject.AddComponent<MeshRenderer>();
        gameObject.GetComponent<MeshRenderer>().material = material;
        mesh = GetComponent<MeshFilter>().mesh;
        mesh.Clear();
        //設置頂點
        for(int i=0;i<vertices.Count;i++){ vertices[i]+=new Vector3(0,2,1);}
        myVertices=vertices.ToArray();
        mesh.vertices=myVertices;
    
        //設置三角形頂點順序，順時針設置
        myTriangles=triangles.ToArray();
        mesh.triangles=myTriangles;
        //設置uv
        Vector2[] myUV=uvs.ToArray();
        mesh.uv=myUV;

        for(int i=0;i<myVertices.Length;i++)
        {
            ball[i] = new Particle(myVertices[i]);
            myVertices[i] = ball[i].x;
            ball[i].v=new Vector3(Random.Range(-0.001f,+0.001f),Random.Range(-0.001f,+0.001f),Random.Range(-0.001f,+0.001f));
        }
        //釘住右上角,左上角
        float range_radius = 0.1f;
        for (int i=0;i<myVertices.Length;i++)
        {
            ball[i].m = 1;
            ball[i].w = 1.0f/ball[i].m;
            ball[i].f = new Vector3(0, -9.8f, 0);
            if ((ball[i].x - new Vector3(1,2,0)).magnitude < range_radius)
            {
                fixconstraints.Add( new FixedPointConstraint(ball[i],ball[i].x));   
            }
            else if ((ball[i].x - new Vector3(-1,2,0)).magnitude < range_radius)
            {
                fixconstraints.Add( new FixedPointConstraint(ball[i],ball[i].x));
            }
        }
        print("fixconstraints.Count: "+ fixconstraints.Count);
        for ( int i = 0; i < myTriangles.Length / 3; ++i)
        {
            Particle p_0 = ball[myTriangles[i * 3 + 0]];
            Particle p_1 = ball[myTriangles[i * 3 + 1]];
            Particle p_2 = ball[myTriangles[i * 3 + 2]];

            distconstraints.Add( new DistanceConstraint(p_0, p_1, (p_0.x - p_1.x).magnitude));
            distconstraints.Add( new DistanceConstraint(p_0, p_2, (p_0.x - p_2.x).magnitude));
            distconstraints.Add( new DistanceConstraint(p_1, p_2, (p_1.x - p_2.x).magnitude));
        }
        print("distconstraints.Count: "+distconstraints.Count);

    }
    void genVertices()
    {
        // Vertices
        for (int v_index = 0; v_index <= vertical_resolution; ++v_index)
        {
            for (int h_index = 0; h_index <= horizontal_resolution; ++h_index)
            {
                float u = (h_index - 0.5f) / (float)horizontal_resolution; 
                if (v_index % 2 == 0 || h_index == 0) u = h_index / (float) horizontal_resolution;
                float v = v_index / (float)vertical_resolution;
                //print("v: "+v);
                float x = (u - 0.5f) * 2;
                float y = (v - 0.5f) * 2;
                //print("y: "+y);
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

