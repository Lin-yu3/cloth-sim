using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class EnvironmentalCollisionConstraint
{
    //https://github.com/yuki-koyama/elasty/blob/master/include/elasty/constraint.hpp
    //https://github.com/yuki-koyama/elasty/blob/master/src/constraint.cpp
    List<Particle> m_particles;
    float m_d;
    public float m_lagrange_multiplier;
    Vector3 m_n;
    float[] m_inv_M;

    public EnvironmentalCollisionConstraint(Particle p0, Vector3 n, float d)
    {
        m_particles = new List<Particle>();
        m_particles.Add(p0);
        m_n=n;
        m_d=d;
        m_inv_M = new float[3] { p0.w, p0.w, p0.w };
        m_lagrange_multiplier=0;
    }
    float norm(float [] array)
    {
        float sum=0;
        for(int i=0; i<array.Length; i++)
        {
            sum += array[i] * array[i];
        }
        return Mathf.Sqrt(sum);
    }
    public void projectParticles()
    {
        float C = calculateValue();
        if(C>=0.0f)return; // Unilateral, 若布料沒穿透碰撞物則跳出 
        float [] grad_C = calculateGrad();
        if( norm(grad_C) < 1e-12) return;
        // PBD:1, XPBD:2
        if(pbd07_mesh_cloth.PBD_OR_XPBD==1)
        {  
            float s = 0;
            for(int i=0; i<3; i++){
                s += grad_C[i] * m_inv_M[i] * grad_C[i];
            }
            s = -C / s;

            float m_stiffness = 1f;
            Vector3 [] delta_x = new Vector3[1];
            delta_x[0] = new Vector3( grad_C[0]* m_inv_M[0], grad_C[1]* m_inv_M[1], grad_C[2]* m_inv_M[2] );//...
            delta_x[0] *= m_stiffness * s ;//小葉老師說這裡怪怪的, 只乘以m_inv_M[0]

            for(int i=0; i<1; i++){
                m_particles[i].p += delta_x[i];
            }
        }
        else if(aerodynamics.PBD_OR_XPBD==2||cloth_gameobject.PBD_OR_XPBD==2||cloth_hit_sphere.PBD_OR_XPBD==2||cloth_mesh.PBD_OR_XPBD==2)
        {
            //計算s
            float s2=0;
            for(int i=0;i<3;i++)
            {
                s2+=grad_C[i]*m_inv_M[i]*grad_C[i];
            }
            // Calculate time-scaled compliance
            float m_compliance=0;// main.cpp 第66,71行
            float m_delta_time=1/60f;// 公式:delta_frame_time/substep
            float alpha_tilde = m_compliance / (m_delta_time * m_delta_time);

            // Calculate \Delta lagrange multiplier
            float delta_lagrange_multiplier =(-C - alpha_tilde * m_lagrange_multiplier) / (s2+ alpha_tilde);
            // Calculate \Delta x
            Vector3[] xpbd_delta_x=new Vector3[1];
            xpbd_delta_x[0]=new Vector3(grad_C[0]*m_inv_M[0],grad_C[1]*m_inv_M[1],grad_C[2]*m_inv_M[2]);
            xpbd_delta_x[0]*=delta_lagrange_multiplier;
            // Update predicted positions
            for(int i=0; i<1; i++){
                m_particles[i].p += xpbd_delta_x[i];
            }
            // Update the lagrange multiplier
            m_lagrange_multiplier += delta_lagrange_multiplier; 
        }
    }
    double[] ToAccord(Vector3 v)
    {
        double[] ans = new double[] { v.x, v.y, v.z };
        for (int i = 0; i < ans.GetLength(0); i++)
        {
            Console.WriteLine("mat" + "[0 ," + i + "] :" + ans[i]);
        }
        return ans;
    }
    float calculateValue()
    {
        Vector3 x = m_particles[0].p;
        float[] k={x.x,x.y,x.z};
        float[] Pm_n={m_n.x,m_n.y,m_n.z};
        float sum=0;
        for(int i=0;i<k.Length;i++)
        {  
            sum+=k[i]*Pm_n[i];
        }
        return sum-m_d;
    }
    float[] calculateGrad()
    {
        float [] grad_C = new float[3];
        
        grad_C[0] = m_n.x;
        grad_C[1] = m_n.y;
        grad_C[2] = m_n.z;

        return grad_C;
    }
}