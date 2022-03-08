using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class pbd04_clothGravity : MonoBehaviour
{
    //demo example pbd04_clothGravity: 31x15 balls, 30*15 + 31*14 constraints, add 7 FixedPointConstrint
    //from https://github.com/yuki-koyama/elasty/tree/master/examples/cloth-alembic
    GameObject [,] sphere=new GameObject[31,15];
    Particle[,] ball = new Particle[31,15];
    List<DistanceConstraint> constraints = new List<DistanceConstraint>();
    List<FixedPointConstraint> fixconstraints = new List<FixedPointConstraint>();
    void Start()
    {
        //為了讓橫的球先畫, 所以先int j
        for(int j=0;j<15;j++)
        {
            for(int i=0;i<31;i++)
            {
                sphere[i,j] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere[i,j].transform.localScale = new Vector3(0.1f,0.1f,0.1f);//設定球大小
                sphere[i,j].transform.SetParent(transform);
                ball[i,j]=new Particle(new Vector3((i-14.5f)/15f*5,-(j-14.5f)/15f*5,0));
                sphere[i,j].transform.localPosition=ball[i,j].x;

                //最上排間隔5顆球釘在牆上
                if(i%5==0 && j==0) // 0,5,10,15,20,25,30  
                {
                    Color Red=new Color(255,0,0);
                    sphere[i,j].GetComponent<Renderer>().material.color= Red;
                    ball[i,j].m=99999;//質量很大,就不會被DistanceConstraint影響
                    ball[i,j].w=1/ball[i,j].m;
                    fixconstraints.Add(new FixedPointConstraint(ball[i,0],ball[i,j].p));
                }
                else
                {
                    ball[i,j].f=new Vector3(0,-9.8f/1000f,0);//注意!!!重力值亂設!!!
                    ball[i,j].m=1;
                    ball[i,j].w=1/ball[i,j].m;
                }
                
            }
        }
        // 建置 DistanceConstraint
        for(int j=0;j<15;j++)
        {
            for(int i=0;i<31-1;i++)
            {
                // d 需要依照GameObject間距比例設定
                constraints.Add(new DistanceConstraint(ball[i+0,j],ball[i+1,j],1/15f*5));
            }
        }
        for(int j=0;j<15-1;j++)
        {
            for(int i=0;i<31;i++)
            {
                // d 需要依照GameObject間距比例設定
                constraints.Add(new DistanceConstraint(ball[i,j+0],ball[i,j+1],1/15f*5));
            }
        }
    }
    void Update()
    {
        // from:https://github.com/yuki-koyama/elasty/blob/692a41953c16243a0d75374d2218176b9b238c86/src/engine.cpp
        // 依照每秒幾個frame設定
        float m_delta_physics_time=1/60f; // 公式:delta_frame_time/substep
        //重力模擬 
        for(int j=0;j<15;j++)
        {   for(int i=0;i<31;i++)
            {//f=ma, a=f*1/m = f*w
                ball[i,j].v=ball[i,j].v + m_delta_physics_time*ball[i,j].w*ball[i,j].f;
                ball[i,j].p=ball[i,j].x + m_delta_physics_time*ball[i,j].v;            
            }
        }    
        //每條constraint都需要被project
        foreach(DistanceConstraint constraint in constraints)
        {
            constraint.projectParticles();
        }
        foreach(FixedPointConstraint constraint in fixconstraints)
        {
            constraint.projectParticles();
        }

        //更新 GameObject localPosition & Particles
        for(int j=0;j<15;j++)
        {
            for(int i=0;i<31;i++)
            {
                //更新 GameObject's localPosition
                sphere[i,j].transform.localPosition=ball[i,j].p;
                //更新 Particles
                ball[i,j].v=(ball[i,j].p-ball[i,j].x)*(1.0f/m_delta_physics_time);
                ball[i,j].x=ball[i,j].p;            
            }
        }        
    }
}
