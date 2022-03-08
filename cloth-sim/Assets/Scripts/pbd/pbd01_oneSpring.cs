using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class pbd01_oneSpring : MonoBehaviour
{
    //demo example pbd01_onespring: only one spring 只有2顆球
    //from https://github.com/yuki-koyama/elasty/tree/master/examples/cloth-alembic
    GameObject[] sphere=new GameObject[2];
    Particle[] ball=new Particle[2];
    DistanceConstraint constraint;
    void Start()
    {
        for (int i = 0; i < 2; i++)
        {
            sphere[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere[i].transform.localScale = new Vector3(0.1f,0.1f,0.1f);//設定球大小
            sphere[i].transform.SetParent(transform);
            ball[i]=new Particle(new Vector3(i,0,0));
            sphere[i].transform.localPosition=ball[i].x;
        }
        constraint=new DistanceConstraint(ball[0],ball[1],2f);
    }
    void Update()
    {
        constraint.projectParticles();
        for(int i=0;i<2;i++)
        {
            sphere[i].transform.localPosition=ball[i].p;
        }
    }
}
