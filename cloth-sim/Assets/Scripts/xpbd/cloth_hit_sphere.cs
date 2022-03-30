using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class cloth_hit_sphere : MonoBehaviour
{  
    //from https://github.com/yuki-koyama/elasty/blob/master/examples/cloth-alembic/main.cpp
    //from https://github.com/yuki-koyama/elasty/blob/master/src/cloth-sim-object.cpp
    //from https://github.com/yuki-koyama/elasty/blob/master/src/utils.cpp
    
    public Material material;
    public GameObject myPrefab;
    public int horizontal_resolution=30;//水平
    public int vertical_resolution=30;//垂直
    public bool MOVING_SPHERE_COLLISION = false;
    public enum Condition { Without_Aerodynamics, With_Aerodynamics, Wind, Wind_High_Drag, Wind_High_Lift}
    public Condition condition;
    List<Vector3> vertices=new List<Vector3>();
    Vector3[] myVertices=new Vector3[976];
    Vector3[] myVertices2=new Vector3[976];
    Particle[] ball=new Particle[976];
    List<int> triangles=new List<int>();
    int[] myTriangles=new int[5490];
    int[] myTriangles2=new int[5490];
    List<Vector2> uvs= new List<Vector2>();
    Vector2[] myUV;
    Vector2[] myUV2;
    List<DistanceConstraint> distconstraints = new List<DistanceConstraint>();
    List<FixedPointConstraint> fixconstraints = new List<FixedPointConstraint>();
    List<EnvironmentalCollisionConstraint> collconstraints = new List<EnvironmentalCollisionConstraint>();
    GameObject sphere;
    Mesh mesh;
    
    void Start()
    {
        genVertices();
        genTriangles();
        DrawMeshSetConstraint();
        if(MOVING_SPHERE_COLLISION==false){
            sphere=Instantiate(myPrefab, new Vector3(0,0.5f,0), Quaternion.identity);
        }
        else{
            sphere=Instantiate(myPrefab, new Vector3(0,0.5f,3), Quaternion.identity);
        }
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
            generateCollisionConstraints();
            // Project Particles
            int solverIterators=10;
            for (int i = 0; i < solverIterators; i++){
                foreach(EnvironmentalCollisionConstraint constraint in collconstraints){
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
            //Clear EnvironmentalCollisionConstraints
            collconstraints.Clear();
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
        //判斷三角形每條邊與幾個三角形共用
    }
    void generateCollisionConstraints()
    {
        if(MOVING_SPHERE_COLLISION==false)
        {
            Vector3 center= sphere.transform.localPosition;
            float tolerance=0.05f;
            float radius=0.5f+0.02f;//大圓半徑+小圓半徑?
            for(int i=0; i<ball.Length; i++)
            {
                Vector3 direction = ball[i].x - center;
                if (direction.magnitude< radius + tolerance)
                {
                    Vector3 normal = direction.normalized;
                    float distance = (center.x*normal.x+center.y*normal.y+center.z*normal.z) + radius;
                    collconstraints.Add( new EnvironmentalCollisionConstraint(ball[i], normal, distance));
                }
            }
        }
        else if(MOVING_SPHERE_COLLISION==true)
        {
            // Collision with a moving sphere
            sphere.transform.localPosition+=new Vector3(0,0,-0.03f);
            float posZ= sphere.transform.position.z;
            Vector3 center=new Vector3(0, 0.5f, posZ);
            float tolerance= 0.05f;
            float radius=0.5f+0.02f;//大圓半徑+小圓半徑?
            for(int i=0; i<ball.Length; i++)
            {
                Vector3 direction = ball[i].x - center;
                if (direction.magnitude< radius + tolerance)
                {
                    Vector3 normal = direction.normalized;
                    float distance = (center.x*normal.x+center.y*normal.y+center.z*normal.z) + radius;
                    collconstraints.Add( new EnvironmentalCollisionConstraint(ball[i], normal, distance));
                }
            }
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

            // Note: This wind force model was proposed by [Wilson+14]
            Vector3 f = -coeff * ((drag_coeff - lift_coeff) * Vector3.Dot(v_rel,n) * v_rel + lift_coeff * v_rel_squared * n);
            ball[myTriangles[i * 3 + 0]].f += (m_0 / m_sum) * f;
            ball[myTriangles[i * 3 + 1]].f += (m_1 / m_sum) * f;
            ball[myTriangles[i * 3 + 2]].f += (m_2 / m_sum) * f;
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

