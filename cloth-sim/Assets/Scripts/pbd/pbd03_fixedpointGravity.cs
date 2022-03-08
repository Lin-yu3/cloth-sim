using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class pbd03_fixedpointGravity : MonoBehaviour
{
    //demo example pbd03_fixedpointGravity:   
    //增加重力g, 會用到Particle的 v,f,m,w
    //https://github.com/yuki-koyama/elasty/blob/692a41953c16243a0d75374d2218176b9b238c86/src/engine.cpp
    //from https://github.com/yuki-koyama/elasty/tree/master/examples/cloth-alembic
    GameObject[] sphere=new GameObject[8];
    Particle[] ball=new Particle[8];
    List<DistanceConstraint> constraints;
    FixedPointConstraint fixconstraint;
    void Start()
    {
        for (int i = 0; i < 8; i++)
        {
            ball[i]=new Particle(new Vector3(i,1,0));
            if(i==0)
            {
                ball[i].m=99999;//質量很大,就不會被DistanceConstraint影響
            }
            else
            {
                //f=重力
                ball[i].f=new Vector3(0,-9.8f,0);
                ball[i].m=1;
            }
            ball[i].w=1/ball[i].m;
            sphere[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere[i].transform.localScale = new Vector3(0.5f,0.5f,0.5f);//設定球大小
            sphere[i].transform.localPosition=ball[i].x;
            sphere[i].transform.SetParent(transform,false);
        }
        constraints=new List<DistanceConstraint>();
        for(int i=0;i<7;i++)
        {
            constraints.Add(new DistanceConstraint(ball[i+0],ball[i+1],1f));
        }
        Color Red=new Color(255,0,0);
        sphere[0].GetComponent<Renderer>().material.color= Red;
        fixconstraint=new FixedPointConstraint(ball[0],new Vector3(0,1,0)); 
    }
    void Update()
    {
        float m_delta_physics_time=1/60f;//隨便設, 每台電腦的效能不同, 調整此參數能讓畫面看起來更順暢
        //重力模擬 f=ma, a=f*1/m = f*w
        for(int i=0;i<8;i++)
        {
            ball[i].v=ball[i].v + m_delta_physics_time*ball[i].w*ball[i].f;
            ball[i].p=ball[i].x + m_delta_physics_time*ball[i].v;            
        }
        //每條constraint都需要被project
        foreach(DistanceConstraint constraint in constraints)
        {
            constraint.projectParticles();
        }
        fixconstraint.projectParticles();//FixedPointConstraint
        for(int i=0;i<8;i++)
        {
            sphere[i].transform.localPosition=ball[i].p;
        }
        //更新位置及速度
        for(int i=0;i<8;i++)
        {
            ball[i].v=(ball[i].p-ball[i].x)*(1.0f/m_delta_physics_time);
            ball[i].x=ball[i].p;
        }
    }
}
