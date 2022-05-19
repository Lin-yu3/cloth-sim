using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
public class cloth_mesh : MonoBehaviour
{  
    //from https://github.com/yuki-koyama/elasty/blob/master/examples/cloth-alembic/main.cpp
    //from https://github.com/yuki-koyama/elasty/blob/master/src/cloth-sim-object.cpp
    //from https://github.com/yuki-koyama/elasty/blob/master/src/utils.cpp
    public Material material;
    public int solverIterators=2;
    public static int PBD_OR_XPBD=2;
    List<Vector3> vertices=new List<Vector3>();
    Vector3[] myVertices=new Vector3[976];
    Particle[] ball=new Particle[976];
    List<int> triangles=new List<int>();
    int[] myTriangles=new int[5490];
    List<Vector2> uvs= new List<Vector2>();
    Vector2[] myUV=new Vector2[976];
    Vector3[] normals=new Vector3[976];
    List<DistanceConstraint> distconstraints = new List<DistanceConstraint>();
    List<FixedPointConstraint> fixconstraints = new List<FixedPointConstraint>();
    List<EnvironmentalCollisionConstraint> collconstraints = new List<EnvironmentalCollisionConstraint>();
    List<IsometricBendingConstraint> isoconstraints = new List<IsometricBendingConstraint>();
    List<BendingConstraint> bendconstraints=new List<BendingConstraint>();
    GameObject sphere;
    Mesh mesh;
    
    void Start()
    {
        generateClothMeshObjData(2,2,30,30);
        DrawMeshSetConstraint();
    }
    void Update()
    {
        for(int substep=0;substep<5;substep++)
        {
            float m_delta_physics_time = 1/60f; // 公式:delta_frame_time/substep
            //Apply external forces
            for(int i=0;i<ball.Length;i++)
            {
                Vector3 g=new Vector3(0,-9.8f,0);
                ball[i].f=ball[i].m*g;
            }
            //applyAerodynamicForces
            applyAerodynamicForces(new Vector3(0,0,0) , 0.1f, 0.06f);
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
            foreach(EnvironmentalCollisionConstraint constraint in collconstraints){
                constraint.m_lagrange_multiplier=0;
            }
            // Project Particles
            
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
        myUV=uvs.ToArray();
        mesh.uv=myUV;
        //加些小小的擾動
        for(int i=0;i<myVertices.Length;i++)
        {
            ball[i] = new Particle(myVertices[i]);
            myVertices[i] = ball[i].x;
            ball[i].v=new Vector3(UnityEngine.Random.Range(-0.001f,+0.001f),UnityEngine.Random.Range(-0.001f,+0.001f),UnityEngine.Random.Range(-0.001f,+0.001f) );
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
        //判斷三角形每條邊與幾個三角形共用
        Dictionary < Tuple<int, int>, List<int>> edges_and_triangles = new Dictionary < Tuple<int, int>, List<int> > ();
        for (int i = 0; i < myTriangles.Length / 3; ++i)
        {
            int index_0 = myTriangles[i * 3 + 0];
            int index_1 = myTriangles[i * 3 + 1];
            int index_2 = myTriangles[i * 3 + 2];

            Tuple<int, int> e_01 = new Tuple<int, int>(index_0, index_1);
            Tuple<int, int> e_02 = new Tuple<int, int>(index_0, index_2);
            Tuple<int, int> e_12 = new Tuple<int, int>(index_1, index_2);

            void register_edge(Tuple<int, int> edge)
            {
                //找不到就加新的
                if(!edges_and_triangles.ContainsKey(edge))
                {
                    
                    edges_and_triangles[edge]=new List<int>();
                    edges_and_triangles[edge].Add(i);
                }
                else 
                {
                    edges_and_triangles[edge].Add(i);
                }  
            }
            register_edge(e_01);
            register_edge(e_02);
            register_edge(e_12);
        }
        foreach (KeyValuePair<Tuple<int, int>, List<int>> key_value in edges_and_triangles)
        {
            Tuple<int,int> edge = key_value.Key;
            List<int> triangles = key_value.Value;

            // Boundary
            if (triangles.Count == 1)
            {
                continue;
            }

            int obtain_another_vertex(int triangle, Tuple<int,int> edge)
            {
                int vertex_0 = myTriangles[3 * triangle + 0];
                int vertex_1 = myTriangles[3 * triangle + 1];
                int vertex_2 = myTriangles[3 * triangle + 2];

                if (vertex_0 != edge.Item1 && vertex_0 != edge.Item2)
                {
                    return vertex_0;
                }
                else if (vertex_1 != edge.Item1 && vertex_1 != edge.Item2)
                {
                    return vertex_1;
                }
                else 
                {
                    return vertex_2;
                }
            }
            int another_vertex_0 = obtain_another_vertex(triangles[0], edge);
            int another_vertex_1 = obtain_another_vertex(triangles[1], edge);
            
            string out_of_plane_strategy = "IsometricBending";
            switch (out_of_plane_strategy)
            {
                case "IsometricBending" :
                    Particle iso_p_0 = ball[edge.Item1];
                    Particle iso_p_1 = ball[edge.Item2];
                    Particle iso_p_2 = ball[another_vertex_0];
                    Particle iso_p_3 = ball[another_vertex_1];

                    isoconstraints.Add(new IsometricBendingConstraint(iso_p_0, iso_p_1, iso_p_2, iso_p_3));
                    break;

                case "Bending":

                    Particle bend_p_0 = ball[edge.Item1];
                    Particle bend_p_1 = ball[edge.Item2];
                    Particle bend_p_2 = ball[another_vertex_0];
                    Particle bend_p_3 = ball[another_vertex_1];

                    Vector3 x_0 = bend_p_0.x;
                    Vector3 x_1 = bend_p_1.x;
                    Vector3 x_2 = bend_p_2.x;
                    Vector3 x_3 = bend_p_3.x;

                    Vector3 p_10 = x_1 - x_0;
                    Vector3 p_20 = x_2 - x_0;
                    Vector3 p_30 = x_3 - x_0;

                    Vector3 n_0 = Vector3.Cross(p_10,p_20).normalized;
                    Vector3 n_1 = Vector3.Cross(p_10,p_30).normalized;

                    // Typical value is 0.0 or pi
                    float dihedral_angle = Mathf.Acos(Mathf.Clamp(Vector3.Dot(n_0,n_1), -1.0f, 1.0f));

                    if (Single.IsNaN(dihedral_angle)) print("dihedral_angle is NaN !!!!!");
                    
                    bendconstraints.Add(new BendingConstraint(bend_p_0, bend_p_1, bend_p_2, bend_p_3, dihedral_angle));

                    break;

                case "Cross":
                    Particle dist_p_2 = ball[another_vertex_0];
                    Particle dist_p_3 = ball[another_vertex_1];

                    Vector3 dist_x_2 = dist_p_2.x;
                    Vector3 dist_x_3 = dist_p_3.x;

                    distconstraints.Add(new DistanceConstraint(dist_p_2, dist_p_3, (dist_x_2 - dist_x_3).magnitude));
                    break;
            }
        }
        calculateAreas();
    }
    void calculateAreas()
    {
        int[,] m_triangle_list=new int[myTriangles.Length / 3,3];
        for (int i = 0; i < myTriangles.Length / 3; ++i)
        {
            m_triangle_list[i, 0] = myTriangles[i * 3 + 0];
            m_triangle_list[i, 1] = myTriangles[i * 3 + 1];
            m_triangle_list[i, 2] = myTriangles[i * 3 + 2];
        }
        float[] m_area_list=new float[myTriangles.Length/3];

        for (int i = 0; i < m_triangle_list.GetLength(0); ++i)
        {
            Vector3 x_0 = ball[m_triangle_list[i, 0]].x;
            Vector3 x_1 = ball[m_triangle_list[i, 1]].x;
            Vector3 x_2 = ball[m_triangle_list[i, 2]].x;

            float area = 0.5f *Vector3.Cross((x_1 - x_0),(x_2 - x_0)).magnitude;

            m_area_list[i]=area;
        } 
    }
    void applyAerodynamicForces(Vector3 global_velocity, float drag_coeff, float lift_coeff)
    {
        float rho = 1.225f; // Taken from Wikipedia: https://en.wikipedia.org/wiki/Density_of_air

        //(drag_coeff >= lift_coeff);
        for ( int i = 0; i < myTriangles.Length / 3; ++i)
        {
            Vector3 x_0 = ball[myTriangles[i * 3 + 0]].x;
            Vector3 x_1 = ball[myTriangles[i * 3 + 1]].x;
            Vector3 x_2 = ball[myTriangles[i * 3 + 2]].x;

            Vector3 v_0 = ball[myTriangles[i * 3 + 0]].v;
            Vector3 v_1 = ball[myTriangles[i * 3 + 1]].v;
            Vector3 v_2 = ball[myTriangles[i * 3 + 2]].v;

            float m_0 = ball[myTriangles[i * 3 + 0]].m;
            float m_1 = ball[myTriangles[i * 3 + 1]].m;
            float m_2 = ball[myTriangles[i * 3 + 2]].m;

            float m_sum = m_0 + m_1 + m_2;

            // Calculate the weighted average of the particle velocities
            Vector3 v_triangle = (m_0 * v_0 + m_1 * v_1 + m_2 * v_2) / m_sum;

            // Calculate the relative velocity of the triangle
            Vector3 v_rel = v_triangle - global_velocity;
            float v_rel_squared = v_rel.sqrMagnitude;

            Vector3 cross= Vector3.Cross(x_1 - x_0, x_2 - x_0) ;
            float area = 0.5f * cross.magnitude;
            Vector3 n_either_side = cross.normalized;
            Vector3 n = (Vector3.Dot(n_either_side,v_rel) > 0.0) ? n_either_side : -n_either_side;

            float coeff = 0.5f * rho * area;

            // Note: This wind force model was proposed by [12]
            Vector3 f = -coeff * ((drag_coeff - lift_coeff) * Vector3.Dot(v_rel,n) * v_rel + lift_coeff * v_rel_squared * n);
            ball[myTriangles[i * 3 + 0]].f += (m_0 / m_sum) * f;
            ball[myTriangles[i * 3 + 1]].f += (m_1 / m_sum) * f;
            ball[myTriangles[i * 3 + 2]].f += (m_2 / m_sum) * f;
        }
    } 
    void generateClothMeshObjData(int width, int height, int horizontal_resolution, int vertical_resolution)
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
                float x = (u - 0.5f) * width;
                float y = (v - 0.5f) * height;
                //print("y: "+y);
                vertices.Add(new Vector3(x, 0, y));
                uvs.Add(new Vector2(u, v));
                // Additional vetex at the even-indexed row
                if (v_index % 2 == 1 && h_index == horizontal_resolution)
                {
                    vertices.Add(new Vector3(0.5f * width, 0, y));
                    uvs.Add(new Vector2(1, v));
                }
            }
        }
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

