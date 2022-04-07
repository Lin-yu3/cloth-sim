# cloth-sim
use PBD or XPBD to simulate cloth

## constraints
使用到的constraint有
* DistanceConstraint(限制長度)
* FixedPointConstraint(固定頂點)
* EnvironmentalCollisionConstraint(防止穿透)
* IsometricBendingConstraint
## pbd folder
含有 6 個範例
* pbd01_oneSpring
* pbd02_twoSpring
* pbd03_fixedpointGravity
* pbd04_clothGravity
* pbd05_30x30cloth
* pbd06_collisionconstraint

## xpbd folder
含有 4 個範例
* cloth_sim
以 cube 繪製頂點,檢查頂點是否受重力向下,且constraint順利運作 (delta_frame_time, num_iters, num_substep, AlgorithmType)=(1/60, 10, 5, XPBD)
* cloth_TriangleMesh
以 mesh來繪製布料, 模擬速度提升許多
* cloth_hit_sphere
使用 collision constraint 分別針對固定即會移動的碰撞物
* aerodynamics
內含 5 種 condition (Vector3 wind_velocity, float drag_coeff, float lift_coeff) 

## 尚未解決
- [ ] CollisionConstraint 布停留在物體表面
- [x] IsometricBendingConstraint(p_0,p_1,p_2,p_3) 分別要放什麼?
- [x] 測試 IsometricBendingConstraint
- [x] 如何調整iters次數, m_delta_physics_time, 以及DistanceConstraint, FixedPointConstraint的 m_delta_time才能讓布料擺盪快速且不會過度拉長

## 每週進度
1. 執行 yuki的elasty專案, 並與我們實作的成果做比較
在Unity package Manager install Alembic, 匯入.abc檔, TimeLine Assets add Alembic Track, 將要執行檔案的時間軸拉進去
2. 在projectParticles 前將 m_lagrange_multiplier 歸零
3. 調整decay rate 的參數, exp(log(0.95) x 1/60) = 0.99962879567
4. TODO(2/14): 加上TriangleMesh, 並依yuki/cloth-alembic 判斷是哪個edge, triangle 建立資料結構, 以利使用 IsometricBendingConstraint
5. TODO(2/17): 看懂理解generateClothMeshObjData()，它寫在 utils.cpp 裡，以紙筆畫得出來想知道它的網格怎麼建的
6. 在（v_index,h_index）=(奇數行,50), add new Vector3(1,0,(v_index/50)x2)
7. TODO(2/21): 在Unity根據yuki建三角網格的方式畫出布料, 當x軸減一, 最右邊會少兩個三角形
8. 使用Unity Mesh畫網格
9. TODO(2/24): 一星期內完成mesh建置, 並判斷是屬於哪條edge及triangle, 再套用constraint
10. Vector3[] myVertices=vertices.ToArray(); //將List<Vector3> vertices 的值存進 Vector3[] myVertices
11. int[] myTriangles=triangles.ToArray(); //將List<int> triangles 的值存進 int[] myTriangles
12. 透過 m_triangle_list 和 m_uv_list 來管理vertices, 以便判斷是哪條edge 及 連接哪些triangle
13. TODO(3/3): 先做出能擺盪的布料，三角mesh的三邊須加上distancd constraint, m_uv_list陣列包陣列的問題可晚點解決(自己件資料結構即可) 
14. vertice位置值更新為NAN, 因此無法畫出mesh, 解決方法先用cube繪製頂點比較容易Debug
15. 發現x,z的值與float u,v有關，找到bug，問題出在整數除以整數，應改為整數除以浮點數
16. 目前狀況: myball[i] 沒有初始化,因為宣告了2個,就錯了。
17. (3/14) Debug 3個錯誤: (1) myball[i].w = 1.0f /2626, (2) fixconstraints.Count 竟然是0, (3) my_delta_physics_time 乘上 myball[i].f 的那行,太小了, 沒有效果。
18. 增加兩種collision: (1) SPHERE_COLLISION (2)MOVING_SPHERE_COLLISION 
19. TODO(3/24): 正面、反面，可以在 mesh 推2次(順時針、逆時針) Download double sided shader（Unity package )就能輕鬆繪製雙面布料, 但效能使用多寡還待查證, 老師說可使用
20. TODO(3/31): 小紅筆電安裝Linux跑yuki程式與我們的交叉比對, 看看collision constraint問題出在哪
21. 論文(PBD)內提到繪製mesh後須先判斷三角形每邊邊長最多與兩個三角形共用
22. TODO(4/7): C std::map 在 C# 可用 Dictionary 來取代 
