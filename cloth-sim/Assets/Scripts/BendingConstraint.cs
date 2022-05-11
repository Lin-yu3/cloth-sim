using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class BendingConstraint
{
    float[] m_inv_M;
    float[,] K;
    float[,] m_Q;
    float m_lagrange_multiplier;
    float m_dihedral_angle;
    List<Particle> m_particles;
    public BendingConstraint(Particle p0,Particle p1,Particle p2,Particle p3, float dihedral_angle)
    {
        m_particles=new List<Particle>();
        m_particles.Add(p0);
        m_particles.Add(p1);
        m_particles.Add(p2);
        m_particles.Add(p3);
        m_dihedral_angle =dihedral_angle;
        m_inv_M = new float[12] {p0.w,p0.w,p0.w,p1.w,p1.w,p1.w,p2.w,p2.w,p2.w,p3.w,p3.w,p3.w};
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
        float C = calculateValue();
        double [] grad_C = calculateGrad();
        // grad_C: double[] to float[]
        float[] grad_C_float = new float[grad_C.Length];
        for(var i = 0;i < grad_C.Length;++i)
        grad_C_float[i] = (float)grad_C[i];
        if(norm(grad_C_float) < 1e-12) return;
        // PBD:1, XPBD:2
        int PBD_OR_XPBD=2;
        switch (PBD_OR_XPBD)
        {
            case 1:
                float s = 0;
                for(int i=0; i<12; i++){
                    s += grad_C_float[i] * m_inv_M[i] * grad_C_float[i];
                }
                s = -C / s;

                float m_stiffness = 0.1f;//cloth-alembicmain.cpp 第23行
                Vector3[] delta_x=new Vector3[4];
                delta_x[0]=new Vector3(grad_C_float[0]*m_inv_M[0],grad_C_float[1]*m_inv_M[1],grad_C_float[2]*m_inv_M[2]);
                delta_x[0]*=m_stiffness*s;
                delta_x[1]=new Vector3(grad_C_float[3]*m_inv_M[3],grad_C_float[4]*m_inv_M[4],grad_C_float[5]*m_inv_M[5]);
                delta_x[1]*=m_stiffness*s;
                delta_x[2]=new Vector3(grad_C_float[6]*m_inv_M[6],grad_C_float[7]*m_inv_M[7],grad_C_float[8]*m_inv_M[8]);
                delta_x[2]*=m_stiffness*s;
                delta_x[3]=new Vector3(grad_C_float[9]*m_inv_M[9],grad_C_float[10]*m_inv_M[10],grad_C_float[11]*m_inv_M[11]);
                delta_x[3]*=m_stiffness*s;
                //更新p
                for(int i=0;i<4;i++)
                {
                    m_particles[i].p += delta_x[i];
                }
                break;
            case 2:
                //計算s
                float s2=0;
                for(int i=0;i<6;i++)
                {
                    s2+=grad_C_float[i]*m_inv_M[i]*grad_C_float[i];
                }
                // Calculate time-scaled compliance
                float m_compliance=50000f;
                float m_delta_time=1/60f;// 公式:delta_frame_time/substep
                float alpha_tilde = m_compliance / (m_delta_time * m_delta_time);

                // Calculate \Delta lagrange multiplier
                float delta_lagrange_multiplier =(-C - alpha_tilde * m_lagrange_multiplier) / (s2+ alpha_tilde);
                // Calculate \Delta x
                Vector3[] xpbd_delta_x=new Vector3[4];
                xpbd_delta_x[0]=new Vector3(grad_C_float[0]*m_inv_M[0],grad_C_float[1]*m_inv_M[1],grad_C_float[2]*m_inv_M[2]);
                xpbd_delta_x[0]*=delta_lagrange_multiplier;
                xpbd_delta_x[1]=new Vector3(grad_C_float[3]*m_inv_M[3],grad_C_float[4]*m_inv_M[4],grad_C_float[5]*m_inv_M[5]);
                xpbd_delta_x[1]*=delta_lagrange_multiplier;
                xpbd_delta_x[2]=new Vector3(grad_C_float[6]*m_inv_M[6],grad_C_float[7]*m_inv_M[7],grad_C_float[8]*m_inv_M[8]);
                xpbd_delta_x[2]*=delta_lagrange_multiplier;
                xpbd_delta_x[3]=new Vector3(grad_C_float[9]*m_inv_M[9],grad_C_float[10]*m_inv_M[10],grad_C_float[11]*m_inv_M[11]);
                xpbd_delta_x[3]*=delta_lagrange_multiplier;
                // Update predicted positions
                for(int i=0;i<4;i++)
                {
                    m_particles[i].p += xpbd_delta_x[i];
                }
                // Update the lagrange multiplier
                m_lagrange_multiplier += delta_lagrange_multiplier;
                break;

        }
    }
    public double[,] convertVecToCrossOp(Vector3 vec)
    {
        double[,] mat = new double[3, 3];
        mat[0, 1] = -vec[2];
        mat[0, 2] = +vec[1];
        mat[1, 0] = +vec[2];
        mat[1, 2] = -vec[0];
        mat[2, 0] = -vec[1];
        mat[2, 1] = +vec[0];
        return mat;
    }
    float calculateValue()
    {
        Vector3 x_0 = m_particles[0].p;
        Vector3 x_1 = m_particles[1].p;
        Vector3 x_2 = m_particles[2].p;
        Vector3 x_3 = m_particles[3].p;

        Vector3 p_10 = x_1 - x_0;
        Vector3 p_20 = x_2 - x_0;
        Vector3 p_30 = x_3 - x_0;

        Vector3 n_0 = Vector3.Cross(p_10,p_20).normalized;
        Vector3 n_1 = Vector3.Cross(p_10,p_30).normalized;

        float current_dihedral_angle = Mathf.Acos(Mathf.Clamp(Vector3.Dot(n_0,n_1), -1.0f, 1.0f));

        if (n_0.magnitude > 0.0) Console.WriteLine("n_0長度大於0");
        if (n_1.magnitude > 0.0) Console.WriteLine("n_1長度大於0");
        if (!double.IsNaN(current_dihedral_angle)) Console.WriteLine("dihedral_angle不是NAN");

        return current_dihedral_angle - m_dihedral_angle;
    } 
    double[] calculateGrad()
    {
        Vector3 x_0 = m_particles[0].p;
        Vector3 x_1 = m_particles[1].p;
        Vector3 x_2 = m_particles[2].p;
        Vector3 x_3 = m_particles[3].p;

        // Assuming that p_0 = [ 0, 0, 0 ]^T without loss of generality
        Vector3 p_1 = x_1 - x_0;
        Vector3 p_2 = x_2 - x_0;
        Vector3 p_3 = x_3 - x_0;

        Vector3 p_1_cross_p_2 = Vector3.Cross(p_1,p_2);
        Vector3 p_1_cross_p_3 = Vector3.Cross(p_1,p_3);

        Vector3 n_0 = p_1_cross_p_2.normalized;
        Vector3 n_1 = p_1_cross_p_3.normalized;

        float d = Vector3.Dot(n_0,n_1);

        // If the current dihedral angle is sufficiently small or large (i.e., zero or pi), return zeros.
        // This is only an ad-hoc solution for stability and it needs to be solved in a more theoretically grounded way.
        float epsilon = Mathf.Pow(10,-12);
        double[] grad_C=new double[12];
        if (1 - d * d < epsilon)
        {
            for (int i = 0; i < 12; i++)
            {
                grad_C[i] = 0;
            }
            return grad_C;
        }

        float common_coeff = -1.0f / Mathf.Sqrt(1 - d * d);

        //作轉換, 準備給Accord
        double[] ToAccord(Vector3 v)
        {
            double[] ans = new double[] { v.x, v.y, v.z };
            for (int i = 0; i < ans.GetLength(0); i++)
            {
                Console.WriteLine("mat" + "[0 ," + i + "] :" + ans[i]);
            }
            return ans;
        }

        //Matrix 3*3
        double[,] Matrix3x3(Vector3 a)
        {
            double[,] vecToMatrix3x3 = { { a.x, a.y, a.z } };
            return vecToMatrix3x3;
        }
        double[,] calc_grad_of_normailzed_cross_prod_wrt_p_a(Vector3 p_a, Vector3 p_b, Vector3 n)
        {
            double left = 1 / Vector3.Cross(p_a, p_b).magnitude;
            double[,] neg_con_p_b = Accord.Math.Elementwise.Multiply(-1, convertVecToCrossOp(p_b));
            double[,] n_mult_n_cross_p_b_T = Accord.Math.Matrix.TransposeAndDot(Matrix3x3(n), Matrix3x3(Vector3.Cross(n, p_b)));
            double[,] right = Accord.Math.Elementwise.Add(neg_con_p_b, n_mult_n_cross_p_b_T);
            return Accord.Math.Elementwise.Multiply(left, right);
        }
        double[,] calc_grad_of_normailzed_cross_prod_wrt_p_b(Vector3 p_a, Vector3 p_b, Vector3 n)
        {
            double left = -(1 / Vector3.Cross(p_a, p_b).magnitude);
            double[,] neg_con_p_a = Accord.Math.Elementwise.Multiply(-1, convertVecToCrossOp(p_a));
            double[,] n_mult_n_cross_p_a_T = Accord.Math.Matrix.TransposeAndDot(Matrix3x3(n), Matrix3x3(Vector3.Cross(n, p_a)));
            double[,] right = Accord.Math.Elementwise.Add(neg_con_p_a, n_mult_n_cross_p_a_T);
            return Accord.Math.Elementwise.Multiply(left, right);
        }
        double[,] partial_n_0_per_partial_p_1 = calc_grad_of_normailzed_cross_prod_wrt_p_a(p_1, p_2, n_0);
        double[,] partial_n_1_per_partial_p_1 = calc_grad_of_normailzed_cross_prod_wrt_p_a(p_1, p_3, n_1);
        double[,] partial_n_0_per_partial_p_2 = calc_grad_of_normailzed_cross_prod_wrt_p_b(p_1, p_2, n_0);
        double[,] partial_n_1_per_partial_p_3 = calc_grad_of_normailzed_cross_prod_wrt_p_b(p_1, p_3, n_1);

        double[] grad_C_wrt_p_1 = Accord.Math.Elementwise.Multiply(common_coeff,
            Accord.Math.Elementwise.Add(Accord.Math.Matrix.TransposeAndDot(partial_n_0_per_partial_p_1, ToAccord(n_1)),
            Accord.Math.Matrix.TransposeAndDot(partial_n_0_per_partial_p_1, ToAccord(n_0))));
        double[] grad_C_wrt_p_2 = Accord.Math.Elementwise.Multiply(common_coeff,
            Accord.Math.Matrix.TransposeAndDot(partial_n_0_per_partial_p_2, ToAccord(n_1)));
        double[] grad_C_wrt_p_3 = Accord.Math.Elementwise.Multiply(common_coeff,
            Accord.Math.Matrix.TransposeAndDot(partial_n_1_per_partial_p_3, ToAccord(n_0)));

        double[] neg_grad_C_wrt_p_1 = Accord.Math.Elementwise.Multiply(-1, grad_C_wrt_p_1);
        double[] neg_grad_C_wrt_p_2 = Accord.Math.Elementwise.Multiply(-1, grad_C_wrt_p_2);
        double[] neg_grad_C_wrt_p_3 = Accord.Math.Elementwise.Multiply(-1, grad_C_wrt_p_3);
        double[] grad_C_wrt_p_1_add_p_2 = Accord.Math.Elementwise.Add(neg_grad_C_wrt_p_1, neg_grad_C_wrt_p_2);
        double[] grad_C_wrt_p_0 = Accord.Math.Elementwise.Add(grad_C_wrt_p_1_add_p_2, neg_grad_C_wrt_p_3);
        //翻譯constraint .cpp 第127-130
        grad_C[3 * 0 + 0] = grad_C_wrt_p_0[0];
        grad_C[3 * 0 + 1] = grad_C_wrt_p_0[1];
        grad_C[3 * 0 + 2] = grad_C_wrt_p_0[2];

        grad_C[3 * 1 + 0] = grad_C_wrt_p_1[0];
        grad_C[3 * 1 + 1] = grad_C_wrt_p_1[1];
        grad_C[3 * 1 + 2] = grad_C_wrt_p_1[2];

        grad_C[3 * 2 + 0] = grad_C_wrt_p_2[0];
        grad_C[3 * 2 + 1] = grad_C_wrt_p_2[1];
        grad_C[3 * 2 + 2] = grad_C_wrt_p_2[2];

        grad_C[3 * 3 + 0] = grad_C_wrt_p_3[0];
        grad_C[3 * 3 + 1] = grad_C_wrt_p_3[1];
        grad_C[3 * 3 + 2] = grad_C_wrt_p_3[2];
        
        return grad_C;
    }
}
