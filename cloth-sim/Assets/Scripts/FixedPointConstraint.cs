using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FixedPointConstraint
{
    //https://github.com/yuki-koyama/elasty/blob/692a41953c16243a0d75374d2218176b9b238c86/include/elasty/constraint.hpp
    //https://github.com/yuki-koyama/elasty/blob/90765bbc9e9a143744da2b0c862a3078d01ac401/src/constraint.cpp
    List<Particle> m_particles;
    float[] m_inv_M;
    public float m_lagrange_multiplier;
    Vector3 m_point;
    
    public FixedPointConstraint(Particle p0, Vector3 point)
    {
        m_particles = new List<Particle>();
        m_particles.Add(p0);
        m_point = point;
        m_inv_M = new float[3] { p0.w, p0.w, p0.w };
        m_lagrange_multiplier=0;
    }
    float norm(float [] array){
        float sum=0;
        for(int i=0; i<array.Length; i++){
            sum += array[i] * array[i];
        }
        return Mathf.Sqrt(sum);
    }
    public void projectParticles(){
        float C = calculateValue();
        float [] grad_C = calculateGrad();
        if( norm(grad_C) < 1e-12) return;
        // PBD:1, XPBD:2
        int PBD_OR_XPBD=2;
        switch (PBD_OR_XPBD)
        {
            case 1:
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
                break;
            case 2:    
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
                break;
        }  
    }
    float calculateValue()
    {
        Vector3 x = m_particles[0].p;

        return (x-m_point).magnitude;
    }
    float[] calculateGrad()
    {
        float [] grad_C = new float[3];
        Vector3 x = m_particles[0].p;
        Vector3 r = x - m_point;
        float dist = r.magnitude;

        Vector3 random=new Vector3(Random.Range(-1f,1f), Random.Range(-1f,1f),Random.Range(-1f,1f));
        //防止1除以極小的數值, 導致向量壞掉
        Vector3 n=(dist<1e-24)?random.normalized:r*(1.0f/dist);

        grad_C[0] = n.x;
        grad_C[1] = n.y;
        grad_C[2] = n.z;

        return grad_C;
    }
}