using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class cloth_sim : MonoBehaviour
{
    int horizontal_resolution=50;//水平
    int vertical_resolution=50;//垂直
    GameObject[] sphere=new GameObject[2626];
    List<Particle> ball=new List<Particle>();
    Particle[] myball=new Particle[2626];
    List<int> triangles=new List<int>();
    int[] myTriangles=new int[15150];
    int[,] m_triangle_list=new int[5050,3];
    List<Vector2> uvs;
    Vector2[] myUV;
    List<DistanceConstraint> distconstraints = new List<DistanceConstraint>();
    List<FixedPointConstraint> fixconstraints = new List<FixedPointConstraint>();
    List<EnvironmentalCollisionConstraint> collconstraints = new List<EnvironmentalCollisionConstraint>();
    //public GameObject myCube;
    void Start()
    {
        genVertices();
        genTriangles();
        int[] myTriangles=triangles.ToArray();
        Particle[] myball=ball.ToArray();
        for(int i=0;i<myball.Length;i++)
        {
            sphere[i] = GameObject.CreatePrimitive(PrimitiveType.Cube);
            sphere[i].transform.localScale = new Vector3(0.008f, 0.008f, 0.008f);
            sphere[i].transform.SetParent(transform);
            sphere[i].transform.localPosition = myball[i].x; 
            myball[i].v=new Vector3(0,0,0);
            myball[i].m=1/myball.Length;
            myball[i].w=1/myball[i].m;
            myball[i].f = new Vector3(0, -9.8f, 0);
            print("myball["+i+"]: "+myball[i].x);
        }
        for ( int i = 0; i < myTriangles.Length / 3; ++i)
        {
            Particle p_0 = myball[myTriangles[i * 3 + 0]];
            Particle p_1 = myball[myTriangles[i * 3 + 1]];
            Particle p_2 = myball[myTriangles[i * 3 + 2]];

            distconstraints.Add( new DistanceConstraint(p_0, p_1, (p_0.x - p_1.x).magnitude));
            distconstraints.Add( new DistanceConstraint(p_0, p_2, (p_0.x - p_2.x).magnitude));
            distconstraints.Add( new DistanceConstraint(p_1, p_2, (p_1.x - p_2.x).magnitude));
        }
        float range_radius = 0.1f;
        for (int i=0;i<myball.Length;i++)
        {
            // Add small perturb
            myball[i].v=new Vector3(Random.Range(-0.001f,+0.001f),Random.Range(-0.001f,+0.001f),Random.Range(-0.001f,+0.001f));

            if ((myball[i].x - new Vector3(1,2,0)).magnitude < range_radius)
            {
                fixconstraints.Add( new FixedPointConstraint(myball[i],myball[i].x));
            }
            if ((myball[i].x - new Vector3(-1,2,0)).magnitude < range_radius)
            {
                fixconstraints.Add( new FixedPointConstraint(myball[i],myball[i].x));
            }
        } 
    }
    void Update()
    {
        // from:https://github.com/yuki-koyama/elasty/blob/692a41953c16243a0d75374d2218176b9b238c86/src/engine.cpp
        // 依照每秒幾個frame設定
        for(int substep=0;substep<5;substep++)
        {
            float m_delta_physics_time = 1/300f; // 公式:delta_frame_time/substep
            //重力模擬      
            for(int i=0;i<myball.Length;i++)
            {//f=ma, a=f*1/m = f*w
                myball[i].v = myball[i].v + m_delta_physics_time * myball[i].w * myball[i].f;
                myball[i].p = myball[i].x + m_delta_physics_time * myball[i].v;
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
                foreach(EnvironmentalCollisionConstraint constraint in collconstraints)
                {
                    constraint.projectParticles();
                }
                foreach(DistanceConstraint constraint in distconstraints){
                    constraint.projectParticles();
                }
                foreach(FixedPointConstraint constraint in fixconstraints)
                {
                    constraint.projectParticles();
                }
                
            }
            //更新 GameObject localPosition & Particles
            for(int i=0;i<myball.Length;i++)
            {
                //更新 GameObject's localPosition
                sphere[i].transform.localPosition = myball[i].p;
                //更新 particle
                myball[i].v = (myball[i].p- myball[i].x) * (1.0f/m_delta_physics_time);
                myball[i].x = myball[i].p;
                //Update velocities
                myball[i].v*=0.9999f;
            }
        }
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
                float v = v_index / (float)(vertical_resolution);
                float x = (u - 0.5f) * 2;
                float y = (v - 0.5f) * 2;
                ball.Add(new Particle( new Vector3(x, 0, y)));
                //uvs.Add(new Vector2(u, v));
                // Additional vetex at the even-indexed row
                if (v_index % 2 == 1 && h_index == horizontal_resolution)
                {
                    ball.Add(new Particle(new Vector3(0.5f * 2, 0, y)));
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
