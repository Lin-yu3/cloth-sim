using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Particle
{
    public Vector3 x;//原始位置
    public Vector3 p;//新位置
    public Vector3 v;//速度
    public Vector3 f;//外力
    public float m=1;//質量
    public float w=1;//權重=1/m
    public Particle(Vector3 position)
    {
        p=position;
        x=position;
        v=new Vector3(0,0,0);
    }
    
}
