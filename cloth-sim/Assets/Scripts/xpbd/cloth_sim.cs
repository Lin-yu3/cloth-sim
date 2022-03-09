using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class cloth_sim : MonoBehaviour
{
    int horizontal_resolution=50;//水平
    int vertical_resolution=50;//垂直
    GameObject[] sphere=new GameObject[2626];
    Particle[] ball=new Particle[2626];
    List<Vector3> vertices;
    Vector3[] myVertices=new Vector3[2626];
    List<int> triangles;
    int[] myTriangles=new int[15150];
    int[,] m_triangle_list=new int[5050,3];
    List<Vector2> uvs;
    Vector2[] myUV;
    List<DistanceConstraint> distconstraints = new List<DistanceConstraint>();
    List<FixedPointConstraint> fixconstraints = new List<FixedPointConstraint>();
    List<EnvironmentalCollisionConstraint> collconstraints = new List<EnvironmentalCollisionConstraint>();
    
    void Start()
    {
        vertices = new List<Vector3>();
        triangles=new List<int>();
        for(int i=0;i<ball.Length;i++)
        {
            sphere[i] = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sphere[i].transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);
            sphere[i].transform.SetParent(transform);
        }
        genVertices();
        genTriangles();
        DrawSphereSetConstraints();
    }
    /*void Update()
    {
        float m_delta_physics_time = 1/300f; // 公式:delta_frame_time/substep, substep=5
        //重力模擬       
        for (int i = 0; i < ball.Length; i++)
        {
            ball[i].v = ball[i].v + m_delta_physics_time * ball[i].w * ball[i].f;
            ball[i].p = ball[i].x + m_delta_physics_time * ball[i].v;
        }
        //更新Mesh
        //mesh.vertices=myVertices;
        // Reset Lagrange multipliers (only necessary for XPBD)
        foreach(DistanceConstraint constraint in distconstraints){
            constraint.m_lagrange_multiplier=0;
        }
        foreach(FixedPointConstraint constraint in fixconstraints){
            constraint.m_lagrange_multiplier=0;
        }
        //generateCollisionConstraints();
        // Project Particles
        int solverIterators=10;
        for (int i = 0; i < solverIterators; i++){    
            foreach(DistanceConstraint constraint in distconstraints){
                constraint.projectParticles();
            }
            foreach(FixedPointConstraint constraint in fixconstraints){
                constraint.projectParticles();
            }
        }
        //更新 GameObject localPosition & Particles
        for(int i=0;i<ball.Length;i++)
        {
            //更新 GameObject's localPosition
            sphere[i].transform.localPosition= ball[i].p;
            //更新 particle
            ball[i].v = (ball[i].p-ball[i].x) * (1.0f/m_delta_physics_time);
            ball[i].x = ball[i].p;
            //Update velocities
            ball[i].v*=0.9999f;
        }
        //Clear EnvironmentalCollisionConstraints
        //collconstraints.Clear();
    }*/
    void DrawSphereSetConstraints()
    {
        //設置頂點
        //for(int i=0;i<vertices.Count;i++){ vertices[i]+=new Vector3(0,2,1);}
        Vector3[] myVertices=vertices.ToArray();
        for(int i=0;i<ball.Length;i++)
        {
            ball[i] = new Particle(myVertices[i]);
            print("myVertices["+i+"]: "+ myVertices[i]);
            sphere[i].transform.localPosition = ball[i].x;     
        }
        
        //設置三角形頂點順序，順時針設置
        int[] myTriangles=triangles.ToArray();
        print("myTriangles.Length: "+ myTriangles.Length);

        //釘住右上角,左上角
        float range_radius = 0.1f;
        for (int i=0;i<ball.Length;i++)
        {
            if ((ball[i].x - new Vector3(1,2,0)).magnitude < range_radius)
            {
                fixconstraints.Add( new FixedPointConstraint(ball[i],ball[i].x));
                ball[i].m = 99999;//質量很大,就不會被DistanceConstraint影響
                ball[i].w = 1/ball[i].m;
            }
            if ((ball[i].x - new Vector3(-1,2,0)).magnitude < range_radius)
            {
                fixconstraints.Add( new FixedPointConstraint(ball[i],ball[i].x));
                ball[i].m = 99999;//質量很大,就不會被DistanceConstraint影響
                ball[i].w = 1/ball[i].m;
            }
        }
        for ( int i = 0; i < myTriangles.Length / 3; ++i)
        {
            Particle p_0 = ball[myTriangles[i * 3 + 0]];
            Particle p_1 = ball[myTriangles[i * 3 + 1]];
            Particle p_2 = ball[myTriangles[i * 3 + 2]];

            distconstraints.Add( new DistanceConstraint(p_0, p_1, (p_0.x - p_1.x).magnitude));
            distconstraints.Add( new DistanceConstraint(p_0, p_2, (p_0.x - p_2.x).magnitude));
            distconstraints.Add( new DistanceConstraint(p_1, p_2, (p_1.x - p_2.x).magnitude));
        }
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
                float x = (u - 0.5f) * 2;
                float y = (v - 0.5f) * 2;
                vertices.Add(new Vector3(x, 0, y));
                //uvs.Add(new Vector2(u, v));
                // Additional vetex at the even-indexed row
                if (v_index % 2 == 1 && h_index == horizontal_resolution)
                {
                    vertices.Add(new Vector3(0.5f * 2, 0, y));
                    //uvs.Add(new Vector2(1, v));
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
