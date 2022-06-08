using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DistanceConstraint
{
    //from https://github.com/yuki-koyama/elasty/blob/master/include/elasty/constraint.hpp
    //from https://github.com/yuki-koyama/elasty/blob/master/src/constraint.cpp
    float m_d;
    float[] m_inv_M;
    public float m_lagrange_multiplier;
    List<Particle> m_particles;
    public DistanceConstraint(Particle p0,Particle p1,float d)
    {
        m_particles=new List<Particle>();
        m_particles.Add(p0);
        m_particles.Add(p1);
        m_d=d;
        m_inv_M=new float[6]{p0.w,p0.w,p0.w,p1.w,p1.w,p1.w};
        m_lagrange_multiplier=0;
    }
    float norm(float[] array)
    {
        float sum=0;
        for(int i=0;i<array.Length;i++)
        {
            sum+=array[i]*array[i];
        }
        return Mathf.Sqrt(sum);
    }
    public void projectParticles()
    {
        
        float C=calculateValue();
        float[] grad_C=calculateGrad();
        if(norm(grad_C)<1e-12)return;
        // PBD:1, XPBD:2
        if(pbd07_mesh_cloth.PBD_OR_XPBD==1||cloth_gameobject.PBD_OR_XPBD==1)
        {  
            //計算s
            float s=0;
            for(int i=0;i<6;i++)
            {
                //s+=grad_C[i]*1/6f*grad_C[i];
                s+=grad_C[i]*m_inv_M[i]*grad_C[i];
            }
            s=-C/s;
            //計算 delta_x
            float m_stiffness=1f;
            Vector3[] delta_x=new Vector3[2];
            delta_x[0]=new Vector3(grad_C[0]*m_inv_M[0],grad_C[1]*m_inv_M[1],grad_C[2]*m_inv_M[2]);
            // delta_x[0]*=m_stiffness*s*1/6f;
            delta_x[0]*=m_stiffness*s;
            delta_x[1]=new Vector3(grad_C[3]*m_inv_M[3],grad_C[4]*m_inv_M[4],grad_C[5]*m_inv_M[5]);
            //delta_x[1]*=m_stiffness*s*1/6f;
            delta_x[1]*=m_stiffness*s;
            //更新p
            for(int i=0;i<2;i++)
            {
                m_particles[i].p += delta_x[i];
            }
        }
        else if(aerodynamics.PBD_OR_XPBD==2||cloth_gameobject.PBD_OR_XPBD==2||cloth_hit_sphere.PBD_OR_XPBD==2||cloth_mesh.PBD_OR_XPBD==2)
        {   
            //計算s
            float s2=0;
            for(int i=0;i<6;i++)
            {
                s2+=grad_C[i]*m_inv_M[i]*grad_C[i];
            }
            // Calculate time-scaled compliance
            float m_compliance=0.05f;//5*10負2次方
            float m_delta_time=1/3f;// 公式:delta_frame_time/substep
            float alpha_tilde = m_compliance / (m_delta_time * m_delta_time);

            // Calculate \Delta lagrange multiplier
            float delta_lagrange_multiplier =(-C - alpha_tilde * m_lagrange_multiplier) / (s2+ alpha_tilde);
            // Calculate \Delta x
            Vector3[] xpbd_delta_x=new Vector3[2];
            xpbd_delta_x[0]=new Vector3(grad_C[0]*m_inv_M[0],grad_C[1]*m_inv_M[1],grad_C[2]*m_inv_M[2]);
            xpbd_delta_x[0]*=delta_lagrange_multiplier;
            xpbd_delta_x[1]=new Vector3(grad_C[3]*m_inv_M[3],grad_C[4]*m_inv_M[4],grad_C[5]*m_inv_M[5]);
            xpbd_delta_x[1]*=delta_lagrange_multiplier;
            // Update predicted positions
            for(int i=0;i<2;i++)
            {
                m_particles[i].p += xpbd_delta_x[i];
            }
            // Update the lagrange multiplier
            m_lagrange_multiplier += delta_lagrange_multiplier;
        }
    }
    float calculateValue()
    {
        Vector3 x_0=m_particles[0].p;
        Vector3 x_1=m_particles[1].p;
        return Vector3.Distance(x_0,x_1)-m_d;
    } 
    float[] calculateGrad()
    {
        float[] grad_C=new float[6];
        Vector3 x_0=m_particles[0].p;
        Vector3 x_1=m_particles[1].p;
        Vector3 r=x_0-x_1;
        float dist=r.magnitude;//不用開根號
        Vector3 random=new Vector3(Random.Range(-1f,1f), Random.Range(-1f,1f),Random.Range(-1f,1f));
        //防止1除以極小的數值, 導致向量壞掉
        Vector3 n=(dist<1e-24)?random.normalized:r*(1.0f/dist);
        grad_C[0] = +n.x;
        grad_C[1] = +n.y;
        grad_C[2] = +n.z;
        grad_C[3] = -n.x;
        grad_C[4] = -n.y;
        grad_C[5] = -n.z;
        return grad_C;
    }   
}
