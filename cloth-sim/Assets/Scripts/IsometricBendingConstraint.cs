using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IsometricBendingConstraint 
{
    float[] m_inv_M;
    float[,] K;
    float[,] m_Q;
    public float m_lagrange_multiplier;
    List<Particle> m_particles;
    public IsometricBendingConstraint(Particle p0,Particle p1,Particle p2,Particle p3)
    {
        m_particles=new List<Particle>();
        m_particles.Add(p0);
        m_particles.Add(p1);
        m_particles.Add(p2);
        m_particles.Add(p3);
        m_inv_M=new float[12]{p0.w,p0.w,p0.w,p1.w,p1.w,p1.w,p2.w,p2.w,p2.w,p3.w,p3.w,p3.w};
        m_lagrange_multiplier=0;
        

        Vector3 x_0 = m_particles[0].x;
        Vector3 x_1 = m_particles[1].x;
        Vector3 x_2 = m_particles[2].x;
        Vector3 x_3 = m_particles[3].x;

        Vector3 e0 = x_1-x_0;
        Vector3 e1 = x_2-x_1;
        Vector3 e2 = x_0-x_2;
        Vector3 e3 = x_3-x_0;
        Vector3 e4 = x_1-x_3;

        float cot_01 = calculateCotTheta(e0, -e1);
        float cot_02 = calculateCotTheta(e0, -e2);
        float cot_03 = calculateCotTheta(e0, e3);
        float cot_04 = calculateCotTheta(e0, e4);

        K = new float[1,4] {{cot_01 + cot_04, cot_02 + cot_03, -cot_01 - cot_02, -cot_03 - cot_04}};
        float A_0 = 0.5f * Vector3.Cross(e0,e1).magnitude;
        float A_1 = 0.5f * Vector3.Cross(e0,e3).magnitude;
        m_Q = new float[4,4];
        for(int i=0;i<4;i++)
        {
            for(int j=0;j<4;j++)
            {
                m_Q[i,j]=(3f/A_0+A_1)*K[0,j]*K[0,i];
            }
        }
    }
    float calculateCotTheta(Vector3 x,Vector3 y)
    {
        float scaled_cos_theta=Vector3.Dot(x,y);
        float scaled_sin_theta=Vector3.Cross(x,y).magnitude;
        return scaled_cos_theta/scaled_sin_theta;
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
        float C = calculateValue();
        float [] grad_C = calculateGrad();
        if(norm(grad_C) < 1e-12) return;
        // PBD:1, XPBD:2
        if(pbd07_mesh_cloth.PBD_OR_XPBD==1||cloth_gameobject.PBD_OR_XPBD==1)
        {
            float s = 0;
            for(int i=0; i<12; i++){
                s += grad_C[i] * m_inv_M[i] * grad_C[i];
            }
            s = -C / s;

            float m_stiffness = 0.1f;//cloth-alembicmain.cpp 第23行
            Vector3[] delta_x=new Vector3[4];
            delta_x[0]=new Vector3(grad_C[0]*m_inv_M[0],grad_C[1]*m_inv_M[1],grad_C[2]*m_inv_M[2]);
            delta_x[0]*=m_stiffness*s;
            delta_x[1]=new Vector3(grad_C[3]*m_inv_M[3],grad_C[4]*m_inv_M[4],grad_C[5]*m_inv_M[5]);
            delta_x[1]*=m_stiffness*s;
            delta_x[2]=new Vector3(grad_C[6]*m_inv_M[6],grad_C[7]*m_inv_M[7],grad_C[8]*m_inv_M[8]);
            delta_x[2]*=m_stiffness*s;
            delta_x[3]=new Vector3(grad_C[9]*m_inv_M[9],grad_C[10]*m_inv_M[10],grad_C[11]*m_inv_M[11]);
            delta_x[3]*=m_stiffness*s;
            //更新p
            for(int i=0;i<4;i++)
            {
                m_particles[i].p += delta_x[i];
            }
        }
        else if(aerodynamics.PBD_OR_XPBD==2||cloth_gameobject.PBD_OR_XPBD==2||cloth_hit_sphere.PBD_OR_XPBD==2||cloth_mesh.PBD_OR_XPBD==2)
        {
            //計算s
            float s2=0;
            for(int i=0;i<12;i++)
            {
                s2+=grad_C[i]*m_inv_M[i]*grad_C[i];
            }
            // Calculate time-scaled compliance
            float m_compliance=50000f;//5乘10的4次方
            float m_delta_time=1/3f;// 公式:delta_frame_time/substep
            float alpha_tilde = m_compliance / (m_delta_time * m_delta_time);

            // Calculate \Delta lagrange multiplier
            float delta_lagrange_multiplier =(-C - alpha_tilde * m_lagrange_multiplier) / (s2+ alpha_tilde);
            // Calculate \Delta x
            Vector3[] xpbd_delta_x=new Vector3[4];
            xpbd_delta_x[0]=new Vector3(grad_C[0]*m_inv_M[0],grad_C[1]*m_inv_M[1],grad_C[2]*m_inv_M[2]);
            xpbd_delta_x[0]*=delta_lagrange_multiplier;
            xpbd_delta_x[1]=new Vector3(grad_C[3]*m_inv_M[3],grad_C[4]*m_inv_M[4],grad_C[5]*m_inv_M[5]);
            xpbd_delta_x[1]*=delta_lagrange_multiplier;
            xpbd_delta_x[2]=new Vector3(grad_C[6]*m_inv_M[6],grad_C[7]*m_inv_M[7],grad_C[8]*m_inv_M[8]);
            xpbd_delta_x[2]*=delta_lagrange_multiplier;
            xpbd_delta_x[3]=new Vector3(grad_C[9]*m_inv_M[9],grad_C[10]*m_inv_M[10],grad_C[11]*m_inv_M[11]);
            xpbd_delta_x[3]*=delta_lagrange_multiplier;
            // Update predicted positions
            for(int i=0;i<4;i++)
            {
                m_particles[i].p += xpbd_delta_x[i];
            }
            // Update the lagrange multiplier
            m_lagrange_multiplier += delta_lagrange_multiplier;
        }
    }
    float calculateValue()
    {
        float sum=0;
        for(int i=0;i<4;++i)
        {
            for(int j=0;j<4;++j)
            {
                sum+=m_Q[i,j]*(m_particles[i].p.x*m_particles[j].p.x+m_particles[i].p.y*m_particles[j].p.y+m_particles[i].p.z*m_particles[j].p.z);
            }
        }
        return 0.5f*sum;
    } 
    float[] calculateGrad()
    {
        float[] grad_C=new float[12];
        for(int i=0;i<4;++i){
            Vector3 sum=new Vector3(0,0,0);
            for(int j=0;j<4;++j)
            {
                sum+=m_Q[i,j]*m_particles[j].p;
            }
            grad_C[3*i+0]=sum.x;
            grad_C[3*i+1]=sum.y;
            grad_C[3*i+2]=sum.z;
        }
        return grad_C;
    }  
}

