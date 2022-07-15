using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StableFluid
{
    const int N = 32, size = (N + 2) * (N + 2) * (N + 2);
    float force = 5.0f;
    float source = 100.0f;
    float dt = 0.4f;
    float visc = 0.0f;
    float diff = 0.0f;
    float[] u = new float[size];
    float[] v = new float[size];
    float[] w = new float[size];
    float[] u_prev = new float[size];
    float[] v_prev = new float[size];
    float[] w_prev = new float[size];
    public float[] dens = new float[size];
    float[] dens_prev = new float[size];
    
    public void Update()
    {
       for (int i = 0; i < 66 * 66; i++)
        {
            dens_prev[i] = 0;
            u_prev[i] = 0;
            v_prev[i] = 0;
            w_prev[i] = 0;
        }
        vel_step(N, u, v, u_prev, v_prev, visc, dt);
        dens_step(N, dens, dens_prev, u, v, diff, dt); 
    }
    int IX(int i, int j, int k) => ((i) + (N + 2) * (j) + (N + 2) * (N + 2) * (k));
    void SWAP(float[] x0, float[] x)
    {
        float[] temp = x0;
        x0 = x;
        x = temp;
    }
    // step1:添加密度(想在哪裡增加流體)
    void add_source(int N, float[] x, float[] s, float dt)
    {
        int size = (N + 2) * (N + 2);
        for (int i = 0; i < size; i++) x[i] += dt * s[i];
    }
    // step2:擴散密度(流體會向外擴散)
    void lin_solve(int N, int b, float[] x, float[] x0, float a, float c)
    {
        for (int n = 0; n < 20; n++)
        {
            for (int k = 1; k <= N; k++)
            {
                for (int j = 1; j <= N; j++)
                {
                    for (int i = 1; i <= N; i++)
                    {
                        x[IX(i, j, k)] = (  x0[IX(i, j, k)]  
                                            + a * (x[IX(i - 1, j, k)] + x[IX(i + 1, j, k)] +
                                                   x[IX(i, j - 1, k)] + x[IX(i, j + 1, k)] +
                                                   x[IX(i, j, k - 1)] + x[IX(i, j, k + 1)])   ) / c;
                    }
                }
            }
        }
        set_bnd(N, b, x);
    }
    // 根據內部單元來填充邊界的幽靈單元
    void set_bnd(int N, int b, float[] x)
    {
        for (int i = 1; i <= N; i++)
        {
            x[IX(0, i, 0)] = (b == 1) ? -x[IX(1, i, 0)] : x[IX(1, i, 0)];
            x[IX(N + 1, i, 0)] = (b == 1) ? -x[IX(N, i, 0)] : x[IX(N, i, 0)];
            x[IX(i, 0, 0)] = (b == 2) ? -x[IX(i, 1, 0)] : x[IX(i, 1, 0)];
            x[IX(i, N + 1, 0)] = (b == 2) ? -x[IX(i, N, 0)] : x[IX(i, N, 0)];

            x[IX(0, i, N + 1)] = (b == 1) ? -x[IX(1, i, N + 1)] : x[IX(1, i, N + 1)];
            x[IX(N + 1, i, N + 1)] = (b == 1) ? -x[IX(N, i, N + 1)] : x[IX(N, i, N + 1)];
            x[IX(i, 0, N + 1)] = (b == 2) ? -x[IX(i, 1, N + 1)] : x[IX(i, 1, N + 1)];
            x[IX(i, N + 1, N + 1)] = (b == 2) ? -x[IX(i, N, N + 1)] : x[IX(i, N, N + 1)];
        }
        x[IX(0, 0, 0)] = 0.3333f * (x[IX(1, 0, 0)] + x[IX(0, 1, 0)] + x[IX(0, 0, 1)]);
        x[IX(0, N + 1, 0)] = 0.3333f * (x[IX(1, N + 1, 0)] + x[IX(0, N, 0)] + x[IX(0, N + 1, 1)]);
        x[IX(N + 1, 0, 0)] = 0.3333f * (x[IX(N, 0, 0)] + x[IX(N + 1, 1, 0)] + x[IX(N + 1, 0, 1)]);
        x[IX(N + 1, N + 1, 0)] = 0.3333f * (x[IX(N, N + 1, 0)] + x[IX(N + 1, N, 0)] + x[IX(N + 1, N + 1, 1)]);

        x[IX(0, 0, N + 1)] = 0.3333f * (x[IX(1, 0, N + 1)] + x[IX(0, 1, N + 1)] + x[IX(0, 0, N)]);
        x[IX(0, N + 1, N + 1)] = 0.5f * (x[IX(1, N + 1, N + 1)] + x[IX(0, N, N + 1)] + x[IX(0, N + 1, N)]);
        x[IX(N + 1, 0, N + 1)] = 0.5f * (x[IX(N, 0, N + 1)] + x[IX(N + 1, 1, N + 1)] + x[IX(N + 1, 0, N)]);
        x[IX(N + 1, N + 1, N + 1)] = 0.5f * (x[IX(N, N + 1, N + 1)] + x[IX(N + 1, N, N + 1)] + x[IX(N + 1, N + 1, N)]);
    }
    void diffuse(int N, int b, float[] x, float[] x0, float diff, float dt)
    {
        float a = dt * diff * N * N;
        lin_solve(N, b, x, x0, a, 1 + 4 * a);
    }
    // step3:移動密度
    void advect(int N, int b, float[] d, float[] d0, float[] u, float[] v, float dt)
    {
        float dt0 = dt * N;
        for (int k = 1; k <= N; k++)
        {
            for (int j = 1; j <= N; j++)
            {
                for (int i = 1; i <= N; i++)
                {
                    float x = i - dt0 * u[IX(i, j, k)];
                    float y = j - dt0 * v[IX(i, j, k)];
                    float z = k - dt0 * w[IX(i, j, k)];

                    if (x < 0.5f) x = 0.5f;
                    if (x > N + 0.5f) x = N + 0.5f;
                    int i0 = (int)x, i1 = i0 + 1;

                    if (y < 0.5f) y = 0.5f;
                    if (y > N + 0.5f) y = N + 0.5f;
                    int j0 = (int)y, j1 = j0 + 1;

                    if (z < 0.5f) z = 0.5f;
                    if (z > N + 0.5f) z = N + 0.5f;
                    int k0 = (int)z, k1 = k0 + 1;

                    float s1 = x - i0, s0 = 1 - s1;//對i
                    float t1 = y - j0, t0 = 1 - t1;//對j
                    float r1 = z - k0, r0 = 1 - r1;//對k
                    //三線性內插
                    d[IX(i, j, k)] =
                               r0 * (s0 * (t0 * d0[IX(i0, j0, k0)] + t1 * d0[IX(i0, j1, k0)]) +
                                     s1 * (t0 * d0[IX(i1, j0, k0)] + t1 * d0[IX(i1, j1, k0)]))
                             + r1 * (s0 * (t0 * d0[IX(i0, j0, k1)] + t1 * d0[IX(i0, j1, k1)]) +
                                     s1 * (t0 * d0[IX(i1, j0, k1)] + t1 * d0[IX(i1, j1, k1)]));

                }
            }
        }
        set_bnd(N, b, d);
    }
    void project(int N, float[] u, float[] v, float[] p, float[] div)
    {
        for (int k = 1; k <= N; k++)
        {
            for (int j = 1; j <= N; j++)
            {
                for (int i = 1; i <= N; i++)
                {
                    div[IX(i, j, k)] = -0.5f * (u[IX(i + 1, j, k)] - u[IX(i - 1, j, k)] +
                                                v[IX(i, j + 1, k)] - v[IX(i, j - 1, k)] +
                                                w[IX(i, j, k + 1)] - w[IX(i, j, k - 1)]
                                               ) / N;
                    p[IX(i, j, k)] = 0;
                }
            }
        }
        set_bnd(N, 0, div);
        set_bnd(N, 0, p);
        lin_solve(N, 0, p, div, 1, 4);
        for (int k = 1; k <= N; k++)
        {
            for (int j = 1; j <= N; j++)
            {
                for (int i = 1; i <= N; i++)
                {
                    u[IX(i, j, k)] -= 0.5f * N * (p[IX(i + 1, j, k)] - p[IX(i - 1, j, k)]);
                    v[IX(i, j, k)] -= 0.5f * N * (p[IX(i, j + 1, k)] - p[IX(i, j - 1, k)]);
                    w[IX(i, j, k)] -= 0.5f * N * (p[IX(i, j, k + 1)] - p[IX(i, j, k - 1)]);
                }
            }
        }
        set_bnd(N, 1, u);
        set_bnd(N, 2, v);
    }
    // step4:整合在一起
    void dens_step(int N, float[] x, float[] x0, float[] u, float[] v, float diff, float dt)
    {
        add_source(N, x, x0, dt);
        SWAP(x0, x); diffuse(N, 0, x, x0, diff, dt);
        SWAP(x0, x); advect(N, 0, x, x0, u, v, dt);
    }
    // 邊界網格
    void vel_step(int N, float[] u, float[] v, float[] u0, float[] v0, float visc, float dt)
    {
        add_source(N, u, u0, dt);
        add_source(N, v, v0, dt);
        SWAP(u0, u); diffuse(N, 1, u, u0, visc, dt);
        SWAP(v0, v); diffuse(N, 2, v, v0, visc, dt);
        project(N, u, v, u0, v0);
        SWAP(u0, u);
        SWAP(v0, v);
        advect(N, 1, u, u0, u0, v0, dt);
        advect(N, 2, v, v0, u0, v0, dt);
        project(N, u, v, u0, v0);
    }
    
}
