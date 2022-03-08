using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class pbd02_twoSpring : MonoBehaviour
{
    //demo example pbd02_twoSpring: two spring, three balls  
    //from https://github.com/yuki-koyama/elasty/tree/master/examples/cloth-alembic
    GameObject[] sphere=new GameObject[3];
    Particle[] ball=new Particle[3];
    DistanceConstraint[] constraint=new DistanceConstraint[2];
    void Start()
    {
        for (int i = 0; i < 3; i++)
        {
            sphere[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere[i].transform.localScale = new Vector3(0.1f,0.1f,0.1f);//設定球大小
            sphere[i].transform.SetParent(transform);
            ball[i]=new Particle(new Vector3(i,0,0));
            sphere[i].transform.localPosition=ball[i].x;
        }
        for(int i=0;i<2;i++)
        {
            constraint[i]=new DistanceConstraint(ball[i+0],ball[i+1],3f);
        }
    }
    void Update()
    {
        //每條constraint都需要被project
        for(int i=0;i<2;i++)
        {
            constraint[i].projectParticles();
        }
        for(int i=0;i<3;i++)
        {
            sphere[i].transform.localPosition=ball[i].p;
        }
    }
}
