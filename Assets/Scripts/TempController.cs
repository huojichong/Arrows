using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace DefaultNamespace
{
    public class TempController : MonoBehaviour
    {

        [SerializeField] private float length;

        [SerializeField] private Transform boneRoot;
        
        [SerializeField] private SkinnedMeshRenderer skinnedMeshRenderer;
        private void Start()
        {
            // var bones = new List<Transform>();
            //
            // foreach (var bone in boneRoot)
            // {
            //     bones.Add(bone as Transform);
            // }
            
            // 4. 初始化绳子控制器
            var controller = GetComponent<SplineRopeSnake>();
            
            controller.bones = skinnedMeshRenderer.bones.ToList(); // 传递骨骼引用
            controller.baseLength = length;                // 设置基础长度
        }
    }
}