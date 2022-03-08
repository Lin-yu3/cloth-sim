using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class pbd06_collisionconstraint : MonoBehaviour
{
    //demo example pbd06_collisionconstraint: 31x31 balls, 30*31 + 31*30 DistanceConstraints, add 2 FixedPointConstrint
    //which particle hit collison add CollisionConstraints
    //from https://github.com/yuki-koyama/elasty/tree/master/examples/cloth-alembic
    GameObject collision_sphere;
    GameObject [,] sphere=new GameObject[31,31];
    Particle[,] ball = new Particle[31,31];
    List<DistanceConstraint> constraints = new List<DistanceConstraint>();
    List<FixedPointConstraint> fixconstraints = new List<FixedPointConstraint>();
    List<EnvironmentalCollisionConstraint> collconstraints = new List<EnvironmentalCollisionConstraint>();
    void Start()
    {
        collision_sphere = GameObject.Find("collision_sphere");
        for(int j=0; j<=30; j++){
            for(int i=0; i<=30; i++){
                sphere[i,j] = GameObject.CreatePrimitive(PrimitiveType.Cube);
                sphere[i,j].transform.localScale = new Vector3(0.02f, 0.02f, 0.02f);
                sphere[i,j].transform.SetParent(transform);
                //ball[i,j] = new Particle( new Vector3( (i/30f)*2-1,  -(j/30f)*2-1, 0 ) );
                ball[i,j] = new Particle( new Vector3( (i/30f)*2-1,  0, -(j/30f)*2-1 ) );
                sphere[i,j].transform.localPosition = ball[i,j].x; 

                //釘住左上角,右上角
                if (i%30== 0 && j==0)
                {
                    Color Red=new Color(255,0,0);
                    sphere[i,j].GetComponent<Renderer>().material.color= Red;
                    ball[i,j].m = 99999;//質量很大,就不會被DistanceConstraint影響
                    ball[i,j].w = 1/ball[i,j].m;
                    fixconstraints.Add( new FixedPointConstraint(ball[i,0], ball[i,j].p ) );
                }
                else
                {
                    ball[i,j].f = new Vector3(0, -9.8f, 0);//注意!!!重力值亂設!!!
                    ball[i,j].m = 0.5f;
                    ball[i,j].w = 1/ball[i,j].m;
                    ball[i,j].v = new Vector3(Random.Range(-0.1f,+0.1f),Random.Range(-0.1f,+0.1f),Random.Range(-0.1f,+0.1f));
                }
            }
        }
        for(int j=0; j<=30; j++){
            for(int i=0; i<=30-1; i++){
                constraints.Add( new DistanceConstraint(ball[i+0,j], ball[i+1,j], 2/30f) );
            }
        }
        for(int j=0; j<=30-1; j++){
            for(int i=0; i<=30; i++){
                constraints.Add( new DistanceConstraint(ball[i,j+0], ball[i,j+1], 2/30f) );
            }
        }
        // 斜邊\
        for(int j=0; j<30; j++){
            for(int i=0; i<30; i++){
                constraints.Add( new DistanceConstraint(ball[i,j], ball[i+1,j+1], 2/30f*1.414f) );
            }
        }
        // 斜邊/
        for(int j=0; j<30; j++){
            for(int i=0; i<30; i++){
                constraints.Add( new DistanceConstraint(ball[i,j+1], ball[i+1,j], 2/30f*1.414f) );
            }
        }
    }
    void generateCollisionConstraints()
    {
        Vector3 center=new Vector3(0,-1,0);
        float tolerance=0.05f;
        float radius=0.5F+0.01f;//大圓半徑+小圓半徑?
        for(int j=0; j<=30; j++)
        {
            for(int i=0; i<=30; i++)
            {
                Vector3 direction = ball[i,j].x - center;
                if (direction.magnitude< radius + tolerance)
                {
                    Vector3 normal = direction.normalized;
                    float distance = (center.x*normal.x+center.y*normal.y+center.z*normal.z) + radius;
                    collconstraints.Add( new EnvironmentalCollisionConstraint(ball[i,j], normal, distance));
                }
            }
        }
    }
    // Update is called once per frame
    void Update()
    {
        // from:https://github.com/yuki-koyama/elasty/blob/692a41953c16243a0d75374d2218176b9b238c86/src/engine.cpp
        // 依照每秒幾個frame設定
        float m_delta_physics_time = 1/60f; // 公式:delta_frame_time/substep
        //重力模擬       
        for(int j=0; j<=30; j++)
        {//f=ma, a=f*1/m = f*w
            for(int i=0; i<=30; i++)
            {
                ball[i,j].v = ball[i,j].v + m_delta_physics_time * ball[i,j].w * ball[i,j].f;
                ball[i,j].p = ball[i,j].x + m_delta_physics_time * ball[i,j].v;
                //ball[i,j].v*=0.9f;
            }
        }
        generateCollisionConstraints();
        // Project Particles
        int solverIterators=10;
        for (int i = 0; i < solverIterators; i++){
            foreach(EnvironmentalCollisionConstraint constraint in collconstraints)
            {
                constraint.projectParticles();
            }
            foreach(DistanceConstraint constraint in constraints){
                constraint.projectParticles();
            }
            foreach(FixedPointConstraint constraint in fixconstraints)
            {
                constraint.projectParticles();
            }
            
        }
        //更新 GameObject localPosition & Particles
        for(int j=0; j<=30; j++)
        {
            for(int i=0; i<=30; i++)
            {
                //更新 GameObject's localPosition
                sphere[i,j].transform.localPosition = ball[i,j].p;
                //更新 particle
                ball[i,j].v = (ball[i,j].p- ball[i,j].x) * (1.0f/m_delta_physics_time);
                ball[i,j].x = ball[i,j].p;
                //Update velocities
                ball[i,j].v*=0.9999f;
            }
        }
        
    }
}

